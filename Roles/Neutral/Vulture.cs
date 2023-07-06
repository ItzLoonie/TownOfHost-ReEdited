﻿using System.Collections.Generic;
using Hazel;
using Sentry.Protocol;
using UnityEngine;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;
using static TOHE.Translator;
using System.Diagnostics;
using Hazel.Dtls;
using System.Linq;

namespace TOHE.Roles.Neutral;

public static class Vulture
{
    private static readonly int Id = 999420;
    private static List<byte> playerIdList = new();

    public static List<byte> UnreportablePlayers = new();
    public static Dictionary<byte, int> BodyReportCount = new();
    public static Dictionary<byte, int> AbilityLeftInRound = new();
    public static Dictionary<byte, long> LastReport = new();


    public static OptionItem ArrowsPointingToDeadBody;
    public static OptionItem NumberOfReportsToWin;
    public static OptionItem VultureReportCD;
    public static OptionItem MaxEaten;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vulture);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "VultureArrowsPointingToDeadBody", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        NumberOfReportsToWin = IntegerOptionItem.Create(Id + 11, "VultureNumberOfReportsToWin", new(1, 14, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        VultureReportCD = FloatOptionItem.Create(Id + 12, "VultureReportCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        MaxEaten = IntegerOptionItem.Create(Id + 13, "VultureMaxEatenInOneRound", new(1, 14, 1), 1, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
    }
    public static void Init()
    {
        playerIdList = new();
        UnreportablePlayers = new List<byte>();
        BodyReportCount = new();
        AbilityLeftInRound = new();
        LastReport = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BodyReportCount[playerId] = 0;
        AbilityLeftInRound[playerId] = MaxEaten.GetInt();
        LastReport[playerId] = Utils.GetTimeStamp();
        new LateTask(() =>
        {
            Utils.GetPlayerById(playerId).RpcGuardAndKill(Utils.GetPlayerById(playerId));
            Utils.GetPlayerById(playerId).Notify(GetString("VultureCooldownUp"));
            return;
        }, Vulture.VultureReportCD.GetFloat()+8f, "Vulture CD");  //for some reason that idk vulture cd completes 8s faster when the game starts, so I added 8f for now 
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

    public static void AfterMeetingTasks()
    {
        foreach (var apc in  playerIdList)
        {
            AbilityLeftInRound[apc] = MaxEaten.GetInt();
            LastReport[apc] = Utils.GetTimeStamp();
            new LateTask(() =>
            {
                Utils.GetPlayerById(apc).RpcGuardAndKill(Utils.GetPlayerById(apc));
                Utils.GetPlayerById(apc).Notify(GetString("VultureCooldownUp"));
                return;
            }, Vulture.VultureReportCD.GetFloat(), "Vulture CD");
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
        AbilityLeftInRound[pc.PlayerId]--;
        foreach (var apc in playerIdList)
        {
            LocateArrow.Remove(apc, target.Object.transform.position);
            SendRPC(apc, false);
        }
     
        pc.Notify(GetString("VultureBodyReported"));
        UnreportablePlayers.Remove(target.PlayerId);
        UnreportablePlayers.Add(target.PlayerId);
    }

    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!seer.Is(CustomRoles.Vulture)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (GameStates.IsMeeting) return "";
        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
}

