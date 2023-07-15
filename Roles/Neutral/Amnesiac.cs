using System.Collections.Generic;
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

public static class Amnesiac
{
    private static readonly int Id = 666420;
    public static List<byte> playerIdList = new();

    public static OptionItem ArrowsPointingToDeadBody;
    public static OptionItem CopyVulture;

    public static readonly string[] AmnesiacVultureCopyRole =
    {
        "Role.Jester",
        "Role.Opportunist",
    };
    public static readonly CustomRoles[] VultureChangeRoles =
    {
        CustomRoles.Jester, CustomRoles.Opportunist
    };

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Amnesiac);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "AmnesiacArrowsPointingToDeadBody", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        CopyVulture = StringOptionItem.Create(Id + 13, "AmnesiacVultureCopy", AmnesiacVultureCopyRole, 1, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac]);
    }

    public static void Init()
    {
        playerIdList = new();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public static bool IsEnable() => playerIdList.Count > 0;

    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAmnesiacArrow, SendOption.Reliable, -1);
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
        if (target.Object != null)
        {
            foreach (var apc in playerIdList)
            {
                LocateArrow.Remove(apc, target.Object.transform.position);
                SendRPC(apc, false);
            }
        }
        ChangeRoleOnReport(pc, target.GetCustomRole());
    }
    public static void ChangeRoleOnReport(PlayerControl pc, CustomRoles role)
    {
        if (pc.GetCustomRole() == CustomRoles.Veteran) Main.VeteranNumOfUsed.Remove(pc.PlayerId);
        else if (pc.GetCustomRole() == CustomRoles.DovesOfNeace) Main.DovesOfNeaceNumOfUsed.Remove(pc.PlayerId);
        else if (pc.GetCustomRole() == CustomRoles.Mayor) Main.MayorUsedButtonCount.Remove(pc.PlayerId);
        else if (pc.GetCustomRole() == CustomRoles.Paranoia) Main.ParaUsedButtonCount.Remove(pc.PlayerId);
        else if ((pc.GetCustomRole() == CustomRoles.Mario)) Main.MarioVentCount.Remove(pc.PlayerId);


        if (role == CustomRoles.Lawyer) role = Lawyer.CRoleChangeRoles[Lawyer.ChangeRolesAfterTargetKilled.GetValue()];
        else if (role == CustomRoles.Executioner) role = Executioner.CRoleChangeRoles[Executioner.ChangeRolesAfterTargetKilled.GetValue()];
        else if (role == CustomRoles.Vulture) role = VultureChangeRoles[CopyVulture.GetValue()];

        else if (role == CustomRoles.Veteran) Main.VeteranNumOfUsed[pc.PlayerId] = Options.VeteranSkillMaxOfUseage.GetInt();
        else if (role == CustomRoles.DovesOfNeace) Main.DovesOfNeaceNumOfUsed[pc.PlayerId] = Options.DovesOfNeaceMaxOfUseage.GetInt();
        else if (role == CustomRoles.Mayor) Main.MayorUsedButtonCount[pc.PlayerId] = 0;
        else if (role == CustomRoles.Paranoia) Main.ParaUsedButtonCount[pc.PlayerId] = 0;
        else if (role == CustomRoles.Mario) Main.MarioVentCount[pc.PlayerId] = 0;
        Utils.GetPlayerById(pc.PlayerId).RpcSetCustomRole(role);

        pc.RpcGuardAndKill(pc);
        pc.Notify(string.Format(GetString("AmnesiacRoleChange"), role));
    }

    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!Amnesiac.playerIdList.Contains(seer.PlayerId)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (GameStates.IsMeeting) return "";
        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }


}
