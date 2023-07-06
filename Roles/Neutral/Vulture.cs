using System.Collections.Generic;
using Hazel;
using Sentry.Protocol;
using UnityEngine;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;
using static TOHE.Translator;
using System.Diagnostics;
using Hazel.Dtls;
namespace TOHE.Roles.Neutral;

public static class Vulture
{
    private static readonly int Id = 999420;
    private static List<byte> playerIdList = new();

    public static List<byte> UnreportablePlayers = new();
    public static Dictionary <byte, int> BodyReportCount;
    public static bool IsReportCDOver;

    public static OptionItem ArrowsPointingToDeadBody;
    public static OptionItem NumberOfReportsToWin;
    public static OptionItem CanVent;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vulture);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "VultureArrowsPointingToDeadBody", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        NumberOfReportsToWin = IntegerOptionItem.Create(Id + 11, "VultureNumberOfReportsToWin", new(1, 14, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, true).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
    }
    public static void Init()
    {
        playerIdList = new();
        UnreportablePlayers = new List<byte>();
        BodyReportCount = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BodyReportCount[playerId] = 0;

    }
    public static bool IsEnable => playerIdList.Count > 0;

    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVultureArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
    }

    public static void Clear()
    {
        foreach (var apc in playerIdList)
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }
    }

    public static void OnPlayerDead(PlayerControl target)
    {
        if (!ArrowsPointingToDeadBody.GetBool()) return;

        var pos = target.GetTruePosition();
        float minDis = float.MaxValue;
        string minName = "";
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Vector2.Distance(pc.GetTruePosition(), pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
                minName = pc.GetRealName();
            }
        }

        foreach (var pc in playerIdList)
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
            SendRPC(pc, true, target.transform.position);
        }
    }

    public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        BodyReportCount[pc.PlayerId]++;
        LocateArrow.Remove(pc.PlayerId, target.Object.transform.position);
        SendRPC(pc.PlayerId, false);
        pc.Notify(GetString("VultureBodyReported"));
        UnreportablePlayers.Add(target.PlayerId);
        playerIdList.Remove(target.PlayerId);
    }

    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!seer.Is(CustomRoles.Vulture)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (GameStates.IsMeeting) return "";
        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
}