﻿using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class Medicaler
{
    private static readonly int Id = 8021866;
    public static List<byte> playerIdList = new();
    public static List<byte> ProtectList = new();
    public static Dictionary<byte, int> ProtectLimit = new();
    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;
    private static OptionItem TargetCanSeeProtect;
    private static OptionItem KnowTargetShieldBroken;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medicaler);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "MedicalerCooldown", new(0f, 999f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medicaler])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 12, "MedicalerSkillLimit", new(1, 990, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medicaler])
            .SetValueFormat(OptionFormat.Times);
        TargetCanSeeProtect = BooleanOptionItem.Create(Id + 13, "MedicalerTargetCanSeeProtect", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medicaler]);
        KnowTargetShieldBroken = BooleanOptionItem.Create(Id + 14, "MedicalerKnowTargetShieldBroken", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medicaler]);
    }
    public static void Init()
    {
        playerIdList = new();
        ProtectList = new();
        ProtectLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ProtectLimit.TryAdd(playerId, SkillLimitOpt.GetInt());

        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 剩余{ProtectLimit[playerId]}个护盾", "Medicaler");

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMedicalerProtectLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ProtectLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ProtectLimit.ContainsKey(PlayerId))
            ProtectLimit[PlayerId] = Limit;
        else
            ProtectLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    private static void SendRPCForProtectList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMedicalerProtectList, SendOption.Reliable, -1);
        writer.Write(ProtectList.Count);
        for (int i = 0; i < ProtectList.Count; i++)
            writer.Write(ProtectList[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCForProtectList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ProtectList = new();
        for (int i = 0; i < count; i++)
            ProtectList.Add(reader.ReadByte());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (ProtectLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? SkillCooldown.GetFloat() : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Medicaler) : Color.gray, ProtectLimit.TryGetValue(playerId, out var protectLimit) ? $"({protectLimit})" : "Invalid");
    public static bool InProtect(byte id) => ProtectList.Contains(id) && Main.PlayerStates.TryGetValue(id, out var ps) && !ps.IsDead;
    public static void OnCheckMurderFormedicaler(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return;
        if (!CanUseKillButton(killer.PlayerId)) return;
        if (ProtectList.Contains(target.PlayerId)) return;

        ProtectLimit[killer.PlayerId]--;
        SendRPC(killer.PlayerId);
        ProtectList.Add(target.PlayerId);
        SendRPCForProtectList();
        killer.SetKillCooldownV2(target: target);
        killer.RPCPlayCustomSound("Shield");
        target.RPCPlayCustomSound("Shield");

        Utils.NotifyRoles(killer);
        Utils.NotifyRoles(target);

        Logger.Info($"{killer.GetNameWithRole()} : 剩余{ProtectLimit[killer.PlayerId]}个护盾", "Medicaler");
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!ProtectList.Contains(target.PlayerId)) return false;

        ProtectList.Remove(target.PlayerId);
        SendRPCForProtectList();
        killer.SetKillCooldownV2(target: target, forceAnime: true);
        killer.RpcGuardAndKill(target);
        if (TargetCanSeeProtect.GetBool())
            target.RpcGuardAndKill(target);
        Utils.NotifyRoles(target);
        if (KnowTargetShieldBroken.GetBool())
            Main.AllPlayerControls.Where(x => playerIdList.Contains(x.PlayerId)).Do(x => x.Notify(Translator.GetString("MedicalerTargetShieldBroken")));
        else
            Main.AllPlayerControls.Where(x => playerIdList.Contains(x.PlayerId)).Do(x => Utils.NotifyRoles(x));

        Logger.Info($"{target.GetNameWithRole()} : 来自医生的盾破碎", "Medicaler");
        return true;
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => ((
            seer.Is(CustomRoles.Medicaler) ||
            (seer.PlayerId == target.PlayerId && TargetCanSeeProtect.GetBool())
            ) && InProtect(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medicaler), "●") : "";

    public static string GetSheildMark(PlayerControl seer)
        => ((
            seer.Is(CustomRoles.Medicaler) ||
            TargetCanSeeProtect.GetBool()
            ) && InProtect(seer.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medicaler), "●") : "";
}