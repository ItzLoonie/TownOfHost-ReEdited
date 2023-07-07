using HarmonyLib;
using Hazel;
using Rewired.UI.ControlMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
public static class ParityCop
{
    private static readonly int Id = 888420;
    private static List<byte> playerIdList = new();
    private static Dictionary<byte, int> MaxCheckLimit = new();
    private static Dictionary<byte, int> RoundCheckLimit = new();
    public static readonly string[] pcEgoistCountMode =
    {
        "EgoistCountMode.Original",
        "EgoistCountMode.Neutral",
    };



    private static OptionItem TryHideMsg;
    private static OptionItem ParityCheckLimitMax;
    private static OptionItem ParityCheckLimitPerMeeting;
    private static OptionItem ParityCheckTargetKnow;
    private static OptionItem ParityCheckOtherTargetKnow;
    public static OptionItem ParityCheckEgoistCountType;


    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ParityCop);
        TryHideMsg = BooleanOptionItem.Create(Id + 10, "ParityCopTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ParityCop])
            .SetColor(Color.green);
        ParityCheckLimitMax = IntegerOptionItem.Create(Id + 11, "MaxParityCheckLimit", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ParityCop])
            .SetValueFormat(OptionFormat.Times);
        ParityCheckLimitPerMeeting = IntegerOptionItem.Create(Id + 12, "ParityCheckLimitPerMeeting", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ParityCop])
            .SetValueFormat(OptionFormat.Times);
        ParityCheckEgoistCountType = StringOptionItem.Create(Id + 13, "ParityCheckEgoistickCountMode", pcEgoistCountMode, 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ParityCop]);
        ParityCheckTargetKnow = BooleanOptionItem.Create(Id + 14, "ParityCheckTargetKnow", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.ParityCop]);
        ParityCheckOtherTargetKnow = BooleanOptionItem.Create(Id + 15, "ParityCheckOtherTargetKnow", false, TabGroup.CrewmateRoles, false).SetParent(ParityCheckTargetKnow);
    }
    public static int ParityCheckEgoistInt()
    {
        if (ParityCheckEgoistCountType.GetString() == "EgoistCountMode.Original") return 0;
        else return 1;
    }
    public static void Init()
    {
        playerIdList = new();
        MaxCheckLimit = new();
        RoundCheckLimit = new();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MaxCheckLimit.Add(playerId, ParityCheckLimitMax.GetInt());
        RoundCheckLimit.Add(playerId, ParityCheckLimitPerMeeting.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void OnReportDeadBody()
    {
        RoundCheckLimit.Clear();
        foreach (var pc in playerIdList) RoundCheckLimit.Add(pc, ParityCheckLimitPerMeeting.GetInt());
    }

    public static bool ParityCheckMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.ParityCop)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|sp|jj|tl|trial|审判|判|审|cp", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("ParityCopDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {

            if (TryHideMsg.GetBool()) GuessManager.TryHideMsg();
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId1, out byte targetId2, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target1 = Utils.GetPlayerById(targetId1);
            var target2 = Utils.GetPlayerById(targetId2);
            if (target1 != null && target2 != null)
            {
                Logger.Info($"{pc.GetNameWithRole()} checked {target1.GetNameWithRole()} and {target2.GetNameWithRole()}", "ParityCop");

                if (MaxCheckLimit[pc.PlayerId] < 1 || RoundCheckLimit[pc.PlayerId] < 1)
                {
                    if (MaxCheckLimit[pc.PlayerId] < 1) 
                    { 
                        if (!isUI) Utils.SendMessage(GetString("ParityCheckMax"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("ParityCheckMax"));
                    }
                    else
                    {
                        if (!isUI) Utils.SendMessage(GetString("ParityCheckRound"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("ParityCheckRound"));
                    }
                    return true;
                }
                if (pc.PlayerId == target1.PlayerId || pc.PlayerId == target2.PlayerId)
                {
                    if (!isUI) Utils.SendMessage(GetString("ParityCheckSelf"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTitle")));
                    else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckSelf")) + "\n" + GetString("ParityCheckTitle"));
                    return true;
                }
                else if (target1.GetCustomRole().IsRevealingRole(target1) || target1.GetCustomSubRoles().Any(role => role.IsRevealingRole(target1)) || target2.GetCustomRole().IsRevealingRole(target2) || target1.GetCustomSubRoles().Any(role => role.IsRevealingRole(target1)))
                {
                    if (!isUI) Utils.SendMessage(GetString("ParityCheckReveal"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTitle")));
                    else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckReveal")) + "\n" + GetString("ParityCheckTitle"));
                    return true;
                }
                else
                {
                   
                    if (((target1.GetCustomRole().IsImpostorTeamV2() || target1.GetCustomSubRoles().Any(role => role.IsImpostorTeamV2())) && (target2.GetCustomRole().IsImpostorTeamV2() || target2.GetCustomSubRoles().Any(role => role.IsImpostorTeamV2()))) ||
                    ((target1.GetCustomRole().IsNeutralTeamV2() || target1.GetCustomSubRoles().Any(role => role.IsNeutralTeamV2())) && (target2.GetCustomRole().IsNeutralTeamV2() || target2.GetCustomSubRoles().Any(role => role.IsNeutralTeamV2()))) ||
                    ((target1.GetCustomRole().IsCrewmateTeamV2() && target1.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2())) && (target2.GetCustomRole().IsCrewmateTeamV2() && target2.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2()))))
                    {
                        ////condition 1 t1
                        //Logger.Msg($"t1 role is imp {target1.GetCustomRole().IsImpostorTeamV2()}, t1 addon is imp {target1.GetCustomSubRoles().Any(role => role.IsImpostorTeamV2())}", "ParityCop");
                        ////condition 1 t2
                        //Logger.Msg($"t2 role is imp {target1.GetCustomRole().IsImpostorTeamV2()}, t2 addon is imp {target1.GetCustomSubRoles().Any(role => role.IsImpostorTeamV2())}", "ParityCop");
                        ////condition 2 t1
                        //Logger.Msg($"t1 role is neutral {target1.GetCustomRole().IsNeutralTeamV2()}, t1 addon is neutral {target1.GetCustomSubRoles().Any(role => role.IsNeutralTeamV2())}", "ParityCop");
                        ////condition 2 t2
                        //Logger.Msg($"t2 role is neutral {target2.GetCustomRole().IsNeutralTeamV2()}, t2 addon is neutral {target2.GetCustomSubRoles().Any(role => role.IsNeutralTeamV2())}", "ParityCop");
                        ////condition 3 t1
                        //Logger.Msg($"t1 role is crew {target1.GetCustomRole().IsCrewmateTeamV2()}, t1 addon is crew {target1.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2())}", "ParityCop");
                        ////condition 3 t2
                        //Logger.Msg($"t2 role is crew {target1.GetCustomRole().IsCrewmateTeamV2()}, t2 addon is crew {target1.GetCustomSubRoles().Any(role => role.IsCrewmateTeamV2())}", "ParityCop");


                        if (!isUI) Utils.SendMessage(string.Format(GetString("ParityCheckTrue"),target1.GetRealName(),target2.GetRealName()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTitle")));
                        else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTrue")) + "\n" + GetString("ParityCheckTitle"));
                    }
                    else
                    {
                        if (!isUI) Utils.SendMessage(string.Format(GetString("ParityCheckFalse"), target1.GetRealName(), target2.GetRealName()), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTitle")));
                        else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckFalse")) + "\n" + GetString("ParityCheckTitle"));
                    }

                    if (ParityCheckTargetKnow.GetBool())
                    {
                        string textToSend = $"{target1.GetRealName()}";
                        if (ParityCheckOtherTargetKnow.GetBool())
                            textToSend = textToSend + $" and {target2.GetRealName()}";
                        textToSend = textToSend + GetString("ParityCheckTargetMsg");

                        string textToSend1 = $"{target2.GetRealName()}";
                        if (ParityCheckOtherTargetKnow.GetBool())
                            textToSend1 = textToSend1 + $" and {target1.GetRealName()}";
                        textToSend1 = textToSend1 + GetString("ParityCheckTargetMsg");
                        Utils.SendMessage(textToSend, target1.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTitle")));
                        Utils.SendMessage(textToSend1, target2.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.ParityCop), GetString("ParityCheckTitle")));
                    }
                    MaxCheckLimit[pc.PlayerId]--;
                    RoundCheckLimit[pc.PlayerId]--;
                }
            }
        }
        return true;
    }

    private static bool MsgToPlayerAndRole(string msg, out byte id1, out byte id2, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);
        msg = msg.TrimStart().TrimEnd();
        Logger.Msg(msg, "ParityCop");

        string[] nums = msg.Split(" ");
        if (nums.Length != 2 || !int.TryParse(nums[0], out int num1) || !int.TryParse(nums[1], out int num2))
        {
            Logger.Msg($"nums.Length {nums.Length}, nums0 {nums[0]}, nums1 {nums[1]}", "ParityCop");
            id1 = byte.MaxValue;
            id2 = byte.MaxValue;
            error = GetString("ParityCheckHelp");
            return false;
        }
        else
        {
            id1 = Convert.ToByte(num1);
            id2 = Convert.ToByte(num2);
        }

        //判断选择的玩家是否合理
        PlayerControl target1 = Utils.GetPlayerById(id1);
        PlayerControl target2 = Utils.GetPlayerById(id2);
        if (target1 == null || target1.Data.IsDead || target2 == null || target2.Data.IsDead)
        {
            error = GetString("ParityCheckNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
}
