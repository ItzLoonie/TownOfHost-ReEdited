using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Text;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Wildling
{
    private static readonly int Id = 650923;
    public static List<byte> playerIdList = new();

    private static OptionItem ProtectDuration;

    private static Dictionary<byte, long> TimeStamp = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ExclusiveRoles, CustomRoles.Wildling, 1, zeroOne: false);
        ProtectDuration = FloatOptionItem.Create(Id + 14, "BKProtectDuration", new(1f, 999f, 1f), 15f, TabGroup.ExclusiveRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Wildling])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        TimeStamp = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TimeStamp.TryAdd(playerId, 0);

        
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBKTimer, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(TimeStamp[playerId].ToString());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        string Time = reader.ReadString();
        TimeStamp.TryAdd(PlayerId, long.Parse(Time));
        TimeStamp[PlayerId] = long.Parse(Time);
    }
    public static bool InProtect(byte playerId) => TimeStamp.TryGetValue(playerId, out var time) && time > Utils.GetTimeStamp(DateTime.Now);
    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return;
        TimeStamp[killer.PlayerId] = Utils.GetTimeStamp(DateTime.Now) + (long)ProtectDuration.GetFloat();
        SendRPC(killer.PlayerId);
        killer.Notify(Translator.GetString("BKInProtect"));
    }
    public static void OnFixedUpdate(PlayerControl pc)
    {
        if (!GameStates.IsInTask || !pc.Is(CustomRoles.Wildling)) return;
        if (TimeStamp[pc.PlayerId] < Utils.GetTimeStamp(DateTime.Now) && TimeStamp[pc.PlayerId] != 0)
        {
            TimeStamp[pc.PlayerId] = 0;
            pc.Notify(Translator.GetString("BKProtectOut"));
        }
    }
    public static string GetHudText(PlayerControl pc)
    {
        if (pc == null || !GameStates.IsInTask || !PlayerControl.LocalPlayer.IsAlive()) return "";
        var str = new StringBuilder();
        if (InProtect(pc.PlayerId))
        {
            var remainTime = TimeStamp[pc.PlayerId] - Utils.GetTimeStamp(DateTime.Now);
            str.Append(string.Format(Translator.GetString("BKSkillTimeRemain"), remainTime));
        }
        else
        {
            str.Append(Translator.GetString("BKSkillNotice"));
        }
        return str.ToString();
    }

    public static void CanUseVent(PlayerControl player)
    {
        bool canUse = false;
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = canUse;
    }

}