using Hazel;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;
using UnityEngine.Rendering;

namespace TOHE.Roles.Neutral;
public static class SoulCollector
{
    private static readonly int Id = 34420;
    public static List<byte> playerIdList = new();
    public static Dictionary<byte, byte> SoulCollectorTarget = new();
    public static Dictionary<byte, int> SoulCollectorPoints = new();

    public static OptionItem SoulCollectorPointsOpt;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SoulCollector);
        SoulCollectorPointsOpt = IntegerOptionItem.Create(Id + 10, "SoulCollectorPointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SoulCollector])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        SoulCollectorTarget = new();
        SoulCollectorPoints = new();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SoulCollectorTarget.Add(playerId, byte.MaxValue);
        SoulCollectorPoints.Add(playerId, 0);
    }

    public static bool IsEnable => playerIdList.Any();

    public static string GetProgressText(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector).ShadeColor(0.25f), SoulCollectorPoints.TryGetValue(playerId, out var x) ? $"({x}/{SoulCollectorPointsOpt.GetInt()})" : "Invalid");

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSoulCollectorLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(SoulCollectorPoints[playerId]);
        writer.Write(SoulCollectorTarget[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte SoulCollectorId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        byte target = reader.ReadByte();
        if (SoulCollectorPoints.ContainsKey(SoulCollectorId))
            SoulCollectorPoints[SoulCollectorId] = Limit;
        else
            SoulCollectorPoints.Add(SoulCollectorId, 0);
        if (SoulCollectorTarget.ContainsKey(SoulCollectorId))
            SoulCollectorTarget[SoulCollectorId] = target;
        else
            SoulCollectorTarget.Add(SoulCollectorId, byte.MaxValue);
    }

    public static void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (!voter.Is(CustomRoles.SoulCollector)) return;
        if (SoulCollectorTarget[voter.PlayerId] != byte.MaxValue) return;

        SoulCollectorTarget[voter.PlayerId] = target.PlayerId;
        Logger.Info($"{voter.GetNameWithRole()} predicted the death of {target.GetNameWithRole()}", "SoulCollector");
        Utils.SendMessage(string.Format(GetString("SoulCollectorTarget"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.SoulCollector), "SoulCollectorTitle"));
        SendRPC(voter.PlayerId);
    }

    public static void OnReportDeadBody()
    {
        foreach (var playerId in SoulCollectorTarget.Keys) SoulCollectorTarget[playerId] = byte.MaxValue;
    }

    public static void OnPlayerDead(PlayerControl deadPlayer)
    {
        if (!IsEnable) return;
        foreach (var playerId in SoulCollectorTarget.Keys)
        {
            var targetId = SoulCollectorTarget[playerId];
            if (targetId == byte.MaxValue) continue;

            if ((targetId == deadPlayer.PlayerId) && (Main.PlayerStates[targetId].deathReason != PlayerState.DeathReason.Disconnected))
            {
                SoulCollectorTarget[playerId] = byte.MaxValue;
                SoulCollectorPoints[playerId]++;
                SendRPC(playerId);
            }
            if (SoulCollectorPoints[playerId] >= SoulCollectorPointsOpt.GetInt())
            {
                SoulCollectorPoints[playerId] = SoulCollectorPointsOpt.GetInt();
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SoulCollector);
                CustomWinnerHolder.WinnerIds.Add(playerId);
            }
        }
    }

}
