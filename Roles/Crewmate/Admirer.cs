using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.Crewmate;

public static class Admirer
{
    private static readonly int Id = 30000;
    private static List<byte> playerIdList = new();

    public static OptionItem AdmireCooldown;
    public static OptionItem KnowTargetRole;
    private static int AdmireLimit = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Id + 10, "AdmireCooldown", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "AdmirerKnowTargetRole", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer]);
    }
    public static void Init()
    {
        playerIdList = new();
        AdmireLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AdmireLimit = 1;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAdmireLimit, SendOption.Reliable, -1);
        writer.Write(AdmireLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        AdmireLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AdmireLimit >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && AdmireLimit >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (AdmireLimit < 1) return;
        if (CanBeAdmired(target))
        {
            AdmireLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Admired);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmiredPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerAdmired")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmireLimit}次魅惑机会", "Admirer");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Admired) && target.Is(CustomRoles.Admirer)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Admirer) && target.Is(CustomRoles.Admired)) return true;
        return false;
    }
    public static string GetAdmireLimit() => Utils.ColorString(AdmireLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Admirer).ShadeColor(0.25f) : Color.gray, $"({AdmireLimit})");
    public static bool CanBeAdmired(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal);
    }
}
