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

namespace TOHE.Roles.Crewmate;

public static class Amnesiac
{
    private static readonly int Id = 666420;
    public static List<byte> playerIdList = new();
    public static Dictionary<byte, float> CurrentKillCooldown = new();
    public static Dictionary<byte, float> MiscopyLimit = new();

    public static OptionItem KillCooldown;
    public static OptionItem CanKill;
    public static OptionItem MiscopyLimitOpt;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Amnesiac);
        KillCooldown = FloatOptionItem.Create(Id + 10, "AmnesiacCopyCooldown", new(0f, 999f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac])
            .SetValueFormat(OptionFormat.Seconds);
        CanKill = BooleanOptionItem.Create(Id + 11, "AmnesiacCanKill", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Amnesiac]);
        MiscopyLimitOpt = IntegerOptionItem.Create(Id + 12, "AmnesiacMiscopyLimit", new(0, 14, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CanKill)
            .SetValueFormat(OptionFormat.Times);
    }

    public static void Init()
    {
        playerIdList = new();
        CurrentKillCooldown = new();
        MiscopyLimit = new();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());
        MiscopyLimit.TryAdd(playerId, MiscopyLimitOpt.GetInt());
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static bool IsEnable() => playerIdList.Count > 0;

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAmnesiacMiscopyLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(MiscopyLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte AmnesiacId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (MiscopyLimit.ContainsKey(AmnesiacId))
            MiscopyLimit[AmnesiacId] = Limit;
        else
            MiscopyLimit.Add(AmnesiacId, MiscopyLimitOpt.GetInt());
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? CurrentKillCooldown[id] : 0f;

    public static void AfterMeetingTasks()
    {
        foreach (var player in playerIdList)
        {
            var pc = Utils.GetPlayerById(player);
            var role = pc.GetCustomRole();
            ////////////           /*remove the settings for current role*/             /////////////////////
            switch (role)
            {
                //case CustomRoles.Addict:
                //    Addict.SuicideTimer.Remove(player);
                //    Addict.ImmortalTimer.Remove(player);
                //    break;
                //case CustomRoles.Bloodhound:
                //    Bloodhound.BloodhoundTargets.Remove(player);
                //    break;
                case CustomRoles.ParityCop:
                    ParityCop.MaxCheckLimit.Remove(player);
                    ParityCop.RoundCheckLimit.Remove(player);
                    break;
                case CustomRoles.Medic:
                    Medic.ProtectLimit.Remove(player);
                    break;
                case CustomRoles.Mediumshiper:
                    Mediumshiper.ContactLimit.Remove(player);
                    break;
                case CustomRoles.Merchant:
                    Merchant.addonsSold.Remove(player);
                    Merchant.bribedKiller.Remove(player);
                    break;
                case CustomRoles.Oracle:
                    Oracle.CheckLimit.Remove(player);
                    break;
                //case CustomRoles.DovesOfNeace:
                //    Main.DovesOfNeaceNumOfUsed.Remove(player);
                //    break;
                case CustomRoles.Paranoia:
                    Main.ParaUsedButtonCount.Remove(player);
                    break;
                case CustomRoles.Snitch:
                    Snitch.IsExposed.Remove(player);
                    Snitch.IsComplete.Remove(player);
                    break;
                //case CustomRoles.Spiritualist:
                //    Spiritualist.LastGhostArrowShowTime.Remove(player);
                //    Spiritualist.ShowGhostArrowUntil.Remove(player);
                //    break;
                //case CustomRoles.Tracker:
                //    Tracker.TrackLimit.Remove(player);
                //    Tracker.TrackerTarget.Remove(player);
                //    break;
                case CustomRoles.Counterfeiter:
                    Counterfeiter.SeelLimit.Remove(player);
                    break;
                //case CustomRoles.SwordsMan:
                //    if (!AmongUsClient.Instance.AmHost) break;
                //    if (!Main.ResetCamPlayerList.Contains(player))
                //        Main.ResetCamPlayerList.Add(player);
                //    break;
                case CustomRoles.Sheriff:
                    Sheriff.CurrentKillCooldown.Remove(player);
                    Sheriff.ShotLimit.Remove(player);
                    break;
                case CustomRoles.Veteran:
                    Main.VeteranNumOfUsed.Remove(player);
                    break;
                case CustomRoles.Judge:
                    Judge.TrialLimit.Remove(player);
                    break;
                case CustomRoles.Mayor:
                    Main.MayorUsedButtonCount.Remove(player);
                    break;
            }
            pc.RpcSetCustomRole(CustomRoles.Amnesiac);
            SetKillCooldown(player);
        }
    }

    public static bool BlacklList(this CustomRoles role)
    {
        return role is CustomRoles.Amnesiac or
            //bcoz of vent cd
            CustomRoles.Grenadier or
            CustomRoles.DovesOfNeace or
            CustomRoles.Veteran or
            CustomRoles.Addict or
            CustomRoles.Chameleon or
            //bcoz of arrows
            CustomRoles.Mortician or
            CustomRoles.Bloodhound or
            CustomRoles.Tracefinder or
            CustomRoles.Spiritualist or
            CustomRoles.Tracker;

    }

    public static bool OnCheckMurder(PlayerControl pc, PlayerControl tpc)
    {
        CustomRoles role = tpc.GetCustomRole();
        if (role.BlacklList())
        {
            pc.Notify(GetString("AmnesiacCanNotCopy"));
            SetKillCooldown(pc.PlayerId);
            return false;
        }
        if (role.IsCrewmate() && (!tpc.GetCustomSubRoles().Any(x => x == CustomRoles.Rascal)))
        {
            ////////////           /*add the settings for new role*/            ////////////
            /* anything that is assigned in onGameStartedPatch.cs comes here */
            switch (role)
            {
                //case CustomRoles.Addict:
                //    Addict.SuicideTimer[pc.PlayerId] = -10f;
                //    Addict.ImmortalTimer[pc.PlayerId] = 420f;
                //    break;
                //case CustomRoles.Bloodhound:
                //    Bloodhound.BloodhoundTargets.Add(pc.PlayerId, new List<byte>());
                //    break;
                case CustomRoles.Deputy:
                    Deputy.SetKillCooldown(pc.PlayerId);
                    break;
                case CustomRoles.ParityCop:
                    ParityCop.MaxCheckLimit.Add(pc.PlayerId, ParityCop.ParityCheckLimitMax.GetInt());
                    ParityCop.RoundCheckLimit.Add(pc.PlayerId, ParityCop.ParityCheckLimitPerMeeting.GetInt());
                    break;
                case CustomRoles.Medic:
                    Medic.ProtectLimit.TryAdd(pc.PlayerId, Medic.SkillLimit);
                    break;
                case CustomRoles.Mediumshiper:
                    Mediumshiper.ContactLimit.Add(pc.PlayerId, Mediumshiper.ContactLimitOpt.GetInt());
                    break;
                case CustomRoles.Merchant:
                    Merchant.addonsSold.Add(pc.PlayerId, 0);
                    Merchant.bribedKiller.Add(pc.PlayerId, new List<byte>());
                    break;
                case CustomRoles.Oracle:
                    Oracle.CheckLimit.TryAdd(pc.PlayerId, Oracle.CheckLimitOpt.GetInt());
                    break;
                //case CustomRoles.DovesOfNeace:
                //    Main.DovesOfNeaceNumOfUsed.Add(pc.PlayerId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                //    break;
                case CustomRoles.Paranoia:
                    Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                    break;
                case CustomRoles.Snitch:
                    Snitch.IsExposed[pc.PlayerId] = false;
                    Snitch.IsComplete[pc.PlayerId] = false;
                    break;
                //case CustomRoles.Spiritualist:
                //    Spiritualist.LastGhostArrowShowTime.Add(pc.PlayerId, 0);
                //    Spiritualist.ShowGhostArrowUntil.Add(pc.PlayerId, 0);
                //    break;
                //case CustomRoles.Tracker:
                //    Tracker.TrackLimit.TryAdd(pc.PlayerId, Tracker.TrackLimitOpt.GetInt());
                //    Tracker.TrackerTarget.Add(pc.PlayerId, byte.MaxValue);
                //    break;
                case CustomRoles.Counterfeiter:
                    Counterfeiter.SeelLimit.Add(pc.PlayerId, Counterfeiter.CounterfeiterSkillLimitTimes.GetInt());
                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.SwordsMan:
                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.CurrentKillCooldown.Add(pc.PlayerId, KillCooldown.GetFloat());
                    Sheriff.ShotLimit.TryAdd(pc.PlayerId, Sheriff.ShotLimitOpt.GetInt());
                    Logger.Info($"{Utils.GetPlayerById(pc.PlayerId)?.GetNameWithRole()} : 残り{Sheriff.ShotLimit[pc.PlayerId]}発", "Sheriff");

                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                //case CustomRoles.Veteran:
                //    Main.VeteranNumOfUsed.Add(pc.PlayerId, Options.VeteranSkillMaxOfUseage.GetInt());
                //    break;
                case CustomRoles.Judge:
                    Judge.TrialLimit.Add(pc.PlayerId, Judge.TrialLimitPerMeeting.GetInt());
                    break;
                case CustomRoles.Mayor:
                    Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                    break;
            }

            pc.RpcSetCustomRole(role);

            pc.RpcGuardAndKill(pc);
            pc.Notify(string.Format(GetString("AmnesiacRoleChange"), Utils.GetRoleName(role)));
            return false;
        }
        if (CanKill.GetBool())
        {
            if (MiscopyLimit[pc.PlayerId] >= 1)
            {
                MiscopyLimit[pc.PlayerId]--;
                SetKillCooldown(pc.PlayerId);
                SendRPC(pc.PlayerId);
                return true;
            }
            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            pc.RpcMurderPlayerV3(pc);
            return false;
        }
        pc.Notify(GetString("AmnesiacCanNotCopy"));
        SetKillCooldown(pc.PlayerId);
        return false;
    }


}