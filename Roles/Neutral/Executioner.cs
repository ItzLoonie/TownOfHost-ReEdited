using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Executioner
{
    private static readonly int Id = 10700;
    public static List<byte> playerIdList = new();
    public static byte WinnerID;

    private static OptionItem CanTargetImpostor;
    private static OptionItem CanTargetNeutralKiller;
    private static OptionItem CanTargetNeutralBenign;
    private static OptionItem CanTargetNeutralEvil;
    private static OptionItem CanTargetNeutralChaos;
    public static OptionItem KnowTargetRole;
    public static OptionItem ChangeRolesAfterTargetKilled;


    /// <summary>
    /// Key: エクスキューショナーのPlayerId, Value: ターゲットのPlayerId
    /// </summary>
    public static Dictionary<byte, byte> Target = new();
    public static readonly string[] ChangeRoles =
    {
        "Role.Crewmate",
        "Role.Jester",
        "Role.Opportunist",
     //   CustomRoles.Crewmate.ToString(), CustomRoles.Jester.ToString(), CustomRoles.Opportunist.ToString(),
    };
    public static readonly CustomRoles[] CRoleChangeRoles =
    {
        CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,
    };

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Executioner);
        CanTargetImpostor = BooleanOptionItem.Create(Id + 10, "ExecutionerCanTargetImpostor", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralKiller = BooleanOptionItem.Create(Id + 12, "ExecutionerCanTargetNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralBenign = BooleanOptionItem.Create(Id + 14, "CanTargetNeutralBenign", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralEvil = BooleanOptionItem.Create(Id + 15, "CanTargetNeutralEvil", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralChaos = BooleanOptionItem.Create(Id + 16, "CanTargetNeutralChaos", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 11, "ExecutionerChangeRolesAfterTargetKilled", ChangeRoles, 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
    }
    public static void Init()
    {
        playerIdList = new();
        Target = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        //ターゲット割り当て
        if (AmongUsClient.Instance.AmHost)
        {
            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(CustomRoleTypes.Impostor)) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                else if (!CanTargetNeutralBenign.GetBool() && target.IsNeutralBenign()) continue;
                else if (!CanTargetNeutralEvil.GetBool() && target.IsNeutralEvil()) continue;
                else if (!CanTargetNeutralChaos.GetBool() && target.IsNeutralChaos()) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target.Add(playerId, SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Executioner");
        }
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SendRPC(byte executionerId, byte targetId = 0x73, string Progress = "")
    {
        MessageWriter writer;
        switch (Progress)
        {
            case "SetTarget":
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExecutionerTarget, SendOption.Reliable);
                writer.Write(executionerId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            case "":
                if (!AmongUsClient.Instance.AmHost) return;
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveExecutionerTarget, SendOption.Reliable);
                writer.Write(executionerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            case "WinCheck":
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) break; //まだ勝者が設定されていない場合
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Executioner);
                CustomWinnerHolder.WinnerIds.Add(executionerId);
                break;
        }
    }
    public static void ReceiveRPC(MessageReader reader, bool SetTarget)
    {
        if (SetTarget)
        {
            byte ExecutionerId = reader.ReadByte();
            byte TargetId = reader.ReadByte();
            Target[ExecutionerId] = TargetId;
        }
        else
            Target.Remove(reader.ReadByte());
    }
    public static void ChangeRoleByTarget(PlayerControl target)
    {
        byte Executioner = 0x73;
        Target.Do(x =>
        {
            if (x.Value == target.PlayerId)
                Executioner = x.Key;
        });
        Utils.GetPlayerById(Executioner).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(Executioner);
        SendRPC(Executioner);
        Utils.NotifyRoles();
    }
    public static void ChangeRole(PlayerControl executioner)
    {
        executioner.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(executioner.PlayerId);
        SendRPC(executioner.PlayerId);
        var text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), Translator.GetString(""));
        text = string.Format(text, Utils.ColorString(Utils.GetRoleColor(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]), Translator.GetString(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()].ToString())));
        executioner.Notify(text);
    }
        public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Executioner) && Target.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }

    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Executioner)) return ""; //エクスキューショナー以外処理しない

        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "♦") : "";
    }
    public static bool CheckExileTarget(GameData.PlayerInfo exiled, bool DecidedWinner, bool Check = false)
    {
        foreach (var kvp in Target.Where(x => x.Value == exiled.PlayerId))
        {
            var executioner = Utils.GetPlayerById(kvp.Key);
            if (executioner == null || !executioner.IsAlive() || executioner.Data.Disconnected) continue;
            if (!Check) ExeWin(kvp.Key, DecidedWinner);
            return true;
        }
        return false;
    }
    public static void ExeWin(byte playerId, bool DecidedWinner)
    {
        if (!DecidedWinner)
        {
            SendRPC(playerId, Progress: "WinCheck");
        }
        else
        {
            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
            CustomWinnerHolder.WinnerIds.Add(playerId);
        }
    }
}