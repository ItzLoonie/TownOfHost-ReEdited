﻿using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Wraith
{
    private static readonly int Id = 1004444;
    private static List<byte> playerIdList = new();

    private static OptionItem WraithCooldown;
    private static OptionItem WraithDuration;

    private static Dictionary<byte, long> InvisTime = new();
    private static Dictionary<byte, long> lastTime = new();
    private static Dictionary<byte, int> ventedId = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Wraith, 1, zeroOne: false);        
        WraithCooldown = FloatOptionItem.Create(Id + 2, "WraithCooldown", new(1f, 999f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
            .SetValueFormat(OptionFormat.Seconds);
        WraithDuration = FloatOptionItem.Create(Id + 4, "WraithDuration", new(1f, 999f, 1f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wraith])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        InvisTime = new();
        lastTime = new();
        ventedId = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

                    if (!AmongUsClient.Instance.AmHost) return;
                if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(PlayerControl pc)
    {
        if (pc.AmOwner) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetWraithTimer, SendOption.Reliable, pc.GetClientId());
        writer.Write((InvisTime.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
        writer.Write((lastTime.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        InvisTime = new();
        lastTime = new();
        long invis = long.Parse(reader.ReadString());
        long last = long.Parse(reader.ReadString());
        if (invis > 0) InvisTime.Add(PlayerControl.LocalPlayer.PlayerId, invis);
        if (last > 0) lastTime.Add(PlayerControl.LocalPlayer.PlayerId, last);
    }
    public static bool CanGoInvis(byte id)
        => GameStates.IsInTask && !InvisTime.ContainsKey(id) && !lastTime.ContainsKey(id);
    public static bool IsInvis(byte id) => InvisTime.ContainsKey(id);

    private static long lastFixedTime = 0;
    public static void AfterMeetingTasks()
    {
        lastTime = new();
        InvisTime = new();
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)))
        {
            lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
            SendRPC(pc);
        }
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask || !IsEnable) return;

        var now = Utils.GetTimeStamp();

        if (lastTime.TryGetValue(player.PlayerId, out var time) && time + (long)WraithCooldown.GetFloat() < now)
        {
            lastTime.Remove(player.PlayerId);
            if (!player.IsModClient()) player.Notify(GetString("WraithCanVent"));
            SendRPC(player);
        }

        if (lastFixedTime != now)
        {
            lastFixedTime = now;
            Dictionary<byte, long> newList = new();
            List<byte> refreshList = new();
            foreach (var it in InvisTime)
            {
                var pc = Utils.GetPlayerById(it.Key);
                if (pc == null) continue;
                var remainTime = it.Value + (long)WraithDuration.GetFloat() - now;
                if (remainTime < 0)
                {
                    lastTime.Add(pc.PlayerId, now);
                    pc?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(pc.PlayerId, out var id) ? id : Main.LastEnteredVent[pc.PlayerId].Id);
                    NameNotifyManager.Notify(pc, GetString("WraithInvisStateOut"));
                    SendRPC(pc);
                    continue;
                }
                else if (remainTime <= 10)
                {
                    if (!pc.IsModClient()) pc.Notify(string.Format(GetString("WraithInvisStateCountdown"), remainTime));
                }
                newList.Add(it.Key, it.Value);
            }
            InvisTime.Where(x => !newList.ContainsKey(x.Key)).Do(x => refreshList.Add(x.Key));
            InvisTime = newList;
            refreshList.Do(x => SendRPC(Utils.GetPlayerById(x)));
        }
    }
    public static void OnCoEnterVent(PlayerPhysics __instance, int ventId)
    {
        var pc = __instance.myPlayer;
        if (!AmongUsClient.Instance.AmHost || IsInvis(pc.PlayerId)) return;
        new LateTask(() =>
        {
            if (CanGoInvis(pc.PlayerId))
            {
                ventedId.Remove(pc.PlayerId);
                ventedId.Add(pc.PlayerId, ventId);

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, 34, SendOption.Reliable, pc.GetClientId());
                writer.WritePacked(ventId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                InvisTime.Add(pc.PlayerId, Utils.GetTimeStamp());
                SendRPC(pc);
                NameNotifyManager.Notify(pc, GetString("WraithInvisState"), WraithDuration.GetFloat());
            }
            else
            {
                __instance.myPlayer.MyPhysics.RpcBootFromVent(ventId);
                NameNotifyManager.Notify(pc, GetString("WraithInvisInCooldown"));
            }
        }, 0.5f, "Wraith Vent");
    }
    public static void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!pc.Is(CustomRoles.Wraith) || !IsInvis(pc.PlayerId)) return;

        InvisTime.Remove(pc.PlayerId);
        lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
        SendRPC(pc);

        pc?.MyPhysics?.RpcBootFromVent(vent.Id);
        NameNotifyManager.Notify(pc, GetString("WraithInvisStateOut"));
    }
    public static string GetHudText(PlayerControl pc)
    {
        if (pc == null || !GameStates.IsInTask || !PlayerControl.LocalPlayer.IsAlive()) return "";
        var str = new StringBuilder();
        if (IsInvis(pc.PlayerId))
        {
            var remainTime = InvisTime[pc.PlayerId] + (long)WraithDuration.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("WraithInvisStateCountdown"), remainTime));
        }
        else if (lastTime.TryGetValue(pc.PlayerId, out var time))
        {
            var cooldown = time + (long)WraithCooldown.GetFloat() - Utils.GetTimeStamp();
            str.Append(string.Format(GetString("WraithInvisCooldownRemain"), cooldown));
        }
        else
        {
            str.Append(GetString("WraithCanVent"));
        }
        return str.ToString();
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!IsInvis(killer.PlayerId)) return true;
        killer.SetKillCooldown();
        target.RpcCheckAndMurder(target);
        target.SetRealKiller(killer);
        return false;
    }
}