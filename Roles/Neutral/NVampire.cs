using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class NVampire
{
    private static readonly int Id = 7052222;
    private static List<byte> playerIdList = new();

    public static OptionItem BiteCooldown;
   // public static OptionItem BiteCooldownIncrese;
    public static OptionItem BiteMax;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowOtherTarget;
    public static OptionItem HasImpostorVision;
    public static OptionItem CanVent;
    public static OptionItem HideBittenRolesOnEject;
    

    private static int BiteLimit = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.NVampire, 1, zeroOne: false);
        BiteCooldown = FloatOptionItem.Create(Id + 10, "NVampireBiteCooldown", new(0f, 990f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire])
            .SetValueFormat(OptionFormat.Seconds);
     //   BiteCooldownIncrese = FloatOptionItem.Create(Id + 11, "NVampireBiteCooldownIncrese", new(0f, 180f, 2.5f), 0f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire])
    //        .SetValueFormat(OptionFormat.Seconds);
        BiteMax = IntegerOptionItem.Create(Id + 12, "NVampireBiteMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "NVampireKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "NVampireTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 15, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire]);
        CanVent = BooleanOptionItem.Create(Id + 16, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire]);
     //   HideBittenRolesOnEject = BooleanOptionItem.Create(Id + 17, "HideBittenRolesOnEject", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NVampire]);        
    }
    public static void Init()
    {
        playerIdList = new();
        BiteLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BiteLimit = BiteMax.GetInt();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNVampireBiteLimit, SendOption.Reliable, -1);
        writer.Write(BiteLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        BiteLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = BiteCooldown.GetFloat();
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && BiteLimit >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (BiteLimit < 1) return;
        if (CanBeBitten(target))
        {
            BiteLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Bitten);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NVampire), GetString("NVampireBittenPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NVampire), GetString("BittenByNVampire")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Bitten.ToString(), "Assign " + CustomRoles.Bitten.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{BiteLimit}次魅惑机会", "NVampire");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NVampire), GetString("NVampireInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{BiteLimit}次魅惑机会", "NVampire");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Bitten) && target.Is(CustomRoles.NVampire)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.NVampire) && target.Is(CustomRoles.Bitten)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Bitten) && target.Is(CustomRoles.Bitten)) return true;
        return false;
    }
    public static string GetBiteLimit() => Utils.ColorString(BiteLimit >= 1 ? Utils.GetRoleColor(CustomRoles.NVampire) : Color.gray, $"({BiteLimit})");
    public static bool CanBeBitten(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNK()) && !pc.Is(CustomRoles.Bitten)
        && !(
            false
            );
    }
}
