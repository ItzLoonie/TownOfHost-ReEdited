using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
class CheckForEndVotingPatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        //Meeting Skip with vote counting on keystroke (m + delete)
        var shouldSkip = false;
        if (Input.GetKeyDown(KeyCode.F6)) 
        {
            shouldSkip = true;
        }
        //
        var voteLog = Logger.Handler("Vote");
        try
        {
            List<MeetingHud.VoterState> statesList = new();
            MeetingHud.VoterState[] states;
            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                //死んでいないディクテーターが投票済み

                //主动叛变
                if (pva.DidVote && pc.PlayerId == pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    if (Options.MadmateSpawnMode.GetInt() == 2 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && Utils.CanBeMadmate(pc))
                    {
                        Main.MadmateNum++;
                        pc.RpcSetCustomRole(CustomRoles.Madmate);
                        ExtendedPlayerControl.RpcSetCustomRole(pc.PlayerId, CustomRoles.Madmate);
                        Utils.NotifyRoles(true, pc, true);
                        Logger.Info("设置职业:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
                    }
                }

                if (pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
                    statesList.Add(new()
                    {
                        VoterId = pva.TargetPlayerId,
                        VotedForId = pva.VotedFor
                    });
                    states = statesList.ToArray();
                    if (AntiBlackout.OverrideExiledPlayer)
                    {
                        __instance.RpcVotingComplete(states, null, true);
                        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
                    }
                    else __instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

                    Logger.Info($"{voteTarget.GetNameWithRole()} 被独裁者驱逐", "Dictator");
                    CheckForDeathOnExile(PlayerState.DeathReason.Vote, pva.VotedFor);
                    Logger.Info("独裁投票，会议强制结束", "Special Phase");
                    voteTarget.SetRealKiller(pc);
                    Main.LastVotedPlayerInfo = voteTarget.Data;
                    if (Main.LastVotedPlayerInfo != null)
                        ConfirmEjections(Main.LastVotedPlayerInfo);
                    return true;
                }

                if (pva.DidVote && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    if (voteTarget != null)
                    {
                        switch (pc.GetCustomRole())
                        {
                            case CustomRoles.Divinator:
                                Divinator.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Eraser:
                                Eraser.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Tracker:
                                Tracker.OnVote(pc, voteTarget);
                                break;
                        }
                    }
                }
            }
            foreach (var ps in __instance.playerStates)
            {
                if (shouldSkip) break; //Meeting Skip with vote counting on keystroke (m + delete)

                //死んでいないプレイヤーが投票していない
                if (!(Main.PlayerStates[ps.TargetPlayerId].IsDead || ps.DidVote)) return false;
            }

            GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
            bool tie = false;

            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"));
                var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if (Options.VoteMode.GetBool())
                {
                    if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                        !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                        !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                        !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                        )
                    {
                        switch (Options.GetWhenSkipVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自票");
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                    {
                        switch (Options.GetWhenNonVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自票");
                                break;
                            case VoteMode.Skip:
                                ps.VotedFor = 253;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票跳过");
                                break;
                            default:
                                break;
                        }
                    }
                }

                //隐藏占卜师的票
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Divinator) && Divinator.HideVote.GetBool()) continue;
                //隐藏抹除者的票
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Eraser) && Eraser.HideVote.GetBool()) continue;

                if (CheckRole(ps.TargetPlayerId, CustomRoles.Tracker) && Tracker.HideVote.GetBool()) continue;

                //主动叛变模式下自票无效
                if (ps.TargetPlayerId == ps.VotedFor && Options.MadmateSpawnMode.GetInt() == 2) continue;

                statesList.Add(new MeetingHud.VoterState()
                {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });
                
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Mayor) && !Options.MayorHideVote.GetBool()) //Mayorの投票数
                {
                    for (var i2 = 0; i2 < Options.MayorAdditionalVote.GetFloat(); i2++)
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                }
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Vindicator) && !Options.VindicatorHideVote.GetBool()) //Vindicator
                {
                    for (var i2 = 0; i2 < Options.VindicatorAdditionalVote.GetFloat(); i2++)
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                }
            }
            states = statesList.ToArray();

            var VotingData = __instance.CustomCalculateVotes();
            byte exileId = byte.MaxValue;
            int max = 0;
            voteLog.Info("===决定驱逐玩家处理开始===");
            foreach (var data in VotingData)
            {
                voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票");
                if (data.Value > max)
                {
                    voteLog.Info(data.Key + "拥有更高票数(" + data.Value + ")");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                }
                else if (data.Value == max)
                {
                    voteLog.Info(data.Key + "与" + exileId + "的票数相同(" + data.Value + ")");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                voteLog.Info($"驱逐ID: {exileId}, 最大: {max}票");
            }

            voteLog.Info($"决定驱逐玩家: {exileId}({Utils.GetVoteName(exileId)})");

            bool braked = false;
            if (tie) //破平者判断
            {
                byte target = byte.MaxValue;
                foreach (var data in VotingData.Where(x => x.Key < 15 && x.Value == max))
                {
                    if (Main.BrakarVoteFor.Contains(data.Key))
                    {
                        if (target != byte.MaxValue)
                        {
                            target = byte.MaxValue;
                            break;
                        }
                        target = data.Key;
                    }
                }
                if (target != byte.MaxValue)
                {
                    Logger.Info("破平者覆盖驱逐玩家", "Brakar Vote");
                    exiledPlayer = Utils.GetPlayerInfoById(target);
                    tie = false;
                    braked = true;
                }
            }

            Collector.CollectAmount(VotingData, __instance);

            if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
            {
                switch ((TieMode)Options.WhenTie.GetValue())
                {
                    case TieMode.Default:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                        break;
                    case TieMode.All:
                        var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                        foreach (var playerId in exileIds)
                            Utils.GetPlayerById(playerId).SetRealKiller(null);
                        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, exileIds);
                        exiledPlayer = null;
                        break;
                    case TieMode.Random:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                        tie = false;
                        break;
                }
            }
            else if (!braked)
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
            exiledPlayer?.Object.SetRealKiller(null);

            //RPC
            if (AntiBlackout.OverrideExiledPlayer)
            {
                __instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
            }
            else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理

            CheckForDeathOnExile(PlayerState.DeathReason.Vote, exileId);

            Main.LastVotedPlayerInfo = exiledPlayer;
            if (Main.LastVotedPlayerInfo != null)
                ConfirmEjections(Main.LastVotedPlayerInfo);

            return false;
        }
        catch (Exception ex)
        {
            Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message), true);
            throw;
        }
    }

    // 参考：https://github.com/music-discussion/TownOfHost-TheOtherRoles
    private static void ConfirmEjections(GameData.PlayerInfo exiledPlayer)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (exiledPlayer == null) return;
        var exileId = exiledPlayer.PlayerId;
        if (exileId is < 0 or > 254) return;
        var realName = exiledPlayer.Object.GetRealName(isMeeting: true);
        Main.LastVotedPlayer = realName;

        var player = Utils.GetPlayerById(exiledPlayer.PlayerId);
        var role = GetString(exiledPlayer.GetCustomRole().ToString());
        var crole = exiledPlayer.GetCustomRole();
        var coloredRole = Utils.GetDisplayRoleName(exileId, true);
        if (Options.ConfirmEgoistOnEject.GetBool() && player.Is(CustomRoles.Egoist))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Egoist), GetRoleString("Temp.Blank") + coloredRole.RemoveHtmlTags());
        if (Options.ConfirmRogueOnEject.GetBool() && player.Is(CustomRoles.Rogue))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Rogue), GetRoleString("Temp.Blank") + coloredRole.RemoveHtmlTags());
        if (Options.ConfirmSidekickOnEject.GetBool() && player.Is(CustomRoles.Sidekick))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sidekick), GetRoleString("Temp.Blank") + coloredRole.RemoveHtmlTags());
        if (Options.ConfirmLoversOnEject.GetBool() && player.Is(CustomRoles.Lovers))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), GetRoleString("Temp.Blank") + coloredRole.RemoveHtmlTags());
        var name = "";
        int impnum = 0;
        int neutralnum = 0;

        if (CustomRolesHelper.RoleExist(CustomRoles.Bard))
        {
            Main.BardCreations++;
            try { name = ModUpdater.Get("https://v1.hitokoto.cn/?encode=text"); }
            catch { name = GetString("ByBardGetFailed"); }
            name += "\n\t\t——" + GetString("ByBard");
            goto EndOfSession;
        }

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var pc_role = pc.GetCustomRole();
            if (pc_role.IsImpostor() && pc != exiledPlayer.Object)
                impnum++;
            else if (pc_role.IsNeutralKilling() && pc != exiledPlayer.Object)
                neutralnum++;
        }
        switch (Options.CEMode.GetInt())
        {
            case 0:
                name = string.Format(GetString("PlayerExiled"), realName);
                break;
            case 1:
                if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Parasite))
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("TeamImpostor")));
                else if (player.GetCustomRole().IsCrewmate() && !player.Is(CustomRoles.Sidekick))
                    name = string.Format(GetString("IsGood"), realName);
                else if (player.GetCustomRole().IsNeutral() && !player.Is(CustomRoles.Parasite) || player.Is(CustomRoles.Sidekick))
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral")));
                break;
            case 2:
                name = string.Format(GetString("PlayerIsRole"), realName, coloredRole);
                if (Options.ShowTeamNextToRoleNameOnEject.GetBool())
                {
                    name += " (";
                    if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Madmate))
                        name += Utils.ColorString(new Color32(255, 25, 25, byte.MaxValue), GetString("TeamImpostor"));
                    else if (player.GetCustomRole().IsNeutral() || player.Is(CustomRoles.Charmed))
                        name += Utils.ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral"));
                    else if (player.GetCustomRole().IsCrewmate())
                        name += Utils.ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate"));
                    name += ")";
                }
                break;
        }
        var DecidedWinner = false;
        //小丑胜利
   /*     if (crole == CustomRoles.Jester)
        {
            name = string.Format(GetString("ExiledJester"), realName, coloredRole);
            DecidedWinner = true;
        }
        //处刑人胜利
        if (Executioner.CheckExileTarget(exiledPlayer, DecidedWinner, true))
        {
            name = string.Format(GetString("ExiledExeTarget"), realName, coloredRole);
            DecidedWinner = true;
        }
        //冤罪师胜利
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exileId))
        {
            if (!(!Options.InnocentCanWinByImp.GetBool() && crole.IsImpostor()))
            {
                if (DecidedWinner) name += string.Format(GetString("ExiledInnocentTargetAddBelow"));
                else name = string.Format(GetString("ExiledInnocentTargetInOneLine"), realName, coloredRole);
                DecidedWinner = true;
            }
        } */

        if (DecidedWinner) name += "<size=0>";
        if (Options.ShowImpRemainOnEject.GetBool() && !DecidedWinner)
        {
            name += "\n";
            string comma = neutralnum > 0 ? "，" : "";
            if (impnum == 0) name += GetString("NoImpRemain") + comma;
            if (impnum == 1) name += GetString("OneImpRemain") + comma;
            if (impnum == 2) name += GetString("TwoImpRemain") + comma;
            if (impnum == 3) name += GetString("ThreeImpRemain") + comma;
        //    else name += string.Format(GetString("ImpRemain"), impnum) + comma;
            if (Options.ShowNKRemainOnEject.GetBool() && neutralnum > 0)
                name += string.Format(GetString("NeutralRemain"), neutralnum);
        }

    EndOfSession:

        name += "<size=0>";
        new LateTask(() =>
        {
            Main.DoBlockNameChange = true;
            if (GameStates.IsInGame) player.RpcSetName(name);
        }, 3.0f, "Change Exiled Player Name");
        new LateTask(() =>
        {
            if (GameStates.IsInGame && !player.Data.Disconnected)
            {
                player.RpcSetName(realName);
                Main.DoBlockNameChange = false;
            }
        }, 11.5f, "Change Exiled Player Name Back");
    }
    public static bool CheckRole(byte id, CustomRoles role)
    {
        var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
        return player != null && player.Is(role);
    }
    public static void TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(PlayerState.DeathReason deathReason, params byte[] playerIds)
    {
        Witch.OnCheckForEndVoting(deathReason, playerIds);
        HexMaster.OnCheckForEndVoting(deathReason, playerIds);
        Virus.OnCheckForEndVoting(deathReason, playerIds);
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            if (CustomRoles.Lovers.IsEnable() && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.LoversSuicide(playerId, true);

            if (CustomRoles.Amor.IsEnable() && Amor.Lovers.Any(lp => lp.PlayerId == playerId))
                Amor.CheckLoversSuicide(playerId, true);

            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    private static void RevengeOnExile(byte playerId, PlayerState.DeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, PlayerState.DeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();
        foreach (var candidate in Main.AllAlivePlayerControls)
        {
            if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

static class ExtendedMeetingHud
{
    public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
    {
        Logger.Info("===计算票数处理开始===", "Vote");
        Dictionary<byte, int> dic = new();
        Main.BrakarVoteFor = new();
        Collector.CollectorVoteFor = new();
        //| 投票された人 | 投票された回数 |
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea ps = __instance.playerStates[i];//该玩家面板里的所有会议中的玩家
            if (ps == null) continue;
            if (ps.VotedFor is not 252 and not byte.MaxValue and not 254)//该玩家面板里是否投了该玩家
            {
                // 默认票数1票
                int VoteNum = 1;

                // 投票给有效玩家时才进行的判断
                var target = Utils.GetPlayerById(ps.VotedFor);
                if (target != null)
                {
                    // 僵尸、活死人无法被票
                    if (target.Is(CustomRoles.Zombie) || target.Is(CustomRoles.Glitch)) VoteNum = 0;
                    // 记录破平者投票
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Brakar))
                        if (!Main.BrakarVoteFor.Contains(target.PlayerId))
                            Main.BrakarVoteFor.Add(target.PlayerId);
                    // 集票者记录数据
                    Collector.CollectorVotes(target, ps);
                }

                //市长附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Mayor)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += Options.MayorAdditionalVote.GetInt();
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Knighted)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += 1;
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Vindicator)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += Options.VindicatorAdditionalVote.GetInt();
                //窃票者附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.TicketsStealer))
                    VoteNum += (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == ps.TargetPlayerId) * Options.TicketsPerKill.GetFloat());

                // 主动叛变模式下自票无效
                if (ps.TargetPlayerId == ps.VotedFor && Options.MadmateSpawnMode.GetInt() == 2) VoteNum = 0;

                //投票を1追加 キーが定義されていない場合は1で上書きして定義
                dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;//统计该玩家被投的数量
            }
        }
        return dic;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingHudStartPatch
{
    public static void NotifyRoleSkillOnMeetingStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        List<(string, byte, string)> msgToSend = new();

        void AddMsg(string text, byte sendTo = 255, string title = "")
            => msgToSend.Add((text, sendTo, title));

        //首次会议技能提示
        if (Options.SendRoleDescriptionFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsModClient()))
            {
                var role = pc.GetCustomRole();
                var sb = new StringBuilder();
                sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + pc.GetRoleInfo(true));
                if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                    Utils.ShowChildrenSettings(opt, ref sb, command: true);
                var txt = sb.ToString();
                sb.Clear().Append(txt.RemoveHtmlTags());
                foreach (var subRole in Main.PlayerStates[pc.PlayerId].SubRoles)
                    sb.Append($"\n\n" + GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                    sb.Append($"\n\n" + GetString($"Lovers") + Utils.GetRoleMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                AddMsg(sb.ToString(), pc.PlayerId);
            }
        if (msgToSend.Count >= 1)
        {
            var msgTemp = msgToSend.ToList();
            new LateTask(() => { msgTemp.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3)); }, 3f, "Skill Description First Meeting");
        }
        msgToSend = new();

        //主动叛变模式提示
        if (Options.MadmateSpawnMode.GetInt() == 2 && CustomRoles.Madmate.GetCount() > 0)
            AddMsg(string.Format(GetString("Message.MadmateSelfVoteModeNotify"), GetString("MadmateSpawnMode.SelfVote")));
        //提示神存活
        if (CustomRoles.God.RoleExist() && Options.NotifyGodAlive.GetBool())
            AddMsg(GetString("GodNoticeAlive"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.God), GetString("GodAliveTitle")));
        //工作狂的生存技巧
        if (MeetingStates.FirstMeeting && CustomRoles.Workaholic.RoleExist() && Options.WorkaholicGiveAdviceAlive.GetBool() && !Options.WorkaholicCannotWinAtDeath.GetBool() && !Options.GhostIgnoreTasks.GetBool())
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Workaholic)))
                Main.WorkaholicAlive.Add(pc.PlayerId);
            List<string> workaholicAliveList = new();
            foreach (var whId in Main.WorkaholicAlive)
                workaholicAliveList.Add(Main.AllPlayerNames[whId]);
            string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
            AddMsg(string.Format(GetString("WorkaholicAdviceAlive"), string.Join(separator, workaholicAliveList)), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Workaholic), GetString("WorkaholicAliveTitle")));
        }
        //Bait Notify
        if (MeetingStates.FirstMeeting && CustomRoles.Bait.RoleExist() && Options.BaitNotification.GetBool())
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Bait)))
                Main.BaitAlive.Add(pc.PlayerId);
            List<string> baitAliveList = new();
            foreach (var whId in Main.BaitAlive)
                baitAliveList.Add(Main.AllPlayerNames[whId]);
            string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
            AddMsg(string.Format(GetString("BaitAdviceAlive"), string.Join(separator, baitAliveList)), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), GetString("BaitAliveTitle")));
        }
        string MimicMsg = "";
        foreach (var pc in Main.AllPlayerControls)
        {
            //黑手党死后技能提示
            if (pc.Is(CustomRoles.Mafia) && !pc.IsAlive())
                AddMsg(GetString("MafiaDeadMsg"), pc.PlayerId);
            if (pc.Is(CustomRoles.Retributionist) && !pc.IsAlive())
                AddMsg(GetString("RetributionistDeadMsg"), pc.PlayerId);
            //网红死亡消息提示
            foreach (var csId in Main.CyberStarDead)
            {
                if (!Options.ImpKnowCyberStarDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                if (!Options.NeutralKnowCyberStarDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;
                AddMsg(string.Format(GetString("CyberStarDead"), Main.AllPlayerNames[csId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.CyberStar), GetString("CyberStarNewsTitle")));
            }

            //侦探报告线索
            if (Main.DetectiveNotify.ContainsKey(pc.PlayerId))
                AddMsg(Main.DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
            //宝箱怪的消息（记录）
            if (pc.Is(CustomRoles.Mimic) && !pc.IsAlive())
                Main.AllAlivePlayerControls.Where(x => x.GetRealKiller()?.PlayerId == pc.PlayerId).Do(x => MimicMsg += $"\n{x.GetNameWithRole(true)}");
            //入殓师的检查
            if (Mortician.msgToSend.ContainsKey(pc.PlayerId))
                AddMsg(Mortician.msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mortician), GetString("MorticianCheckTitle")));
            //通灵师的提示（自己）
            if (Mediumshiper.ContactPlayer.ContainsValue(pc.PlayerId))
                AddMsg(string.Format(GetString("MediumshipNotifySelf"), Main.AllPlayerNames[Mediumshiper.ContactPlayer.Where(x => x.Value == pc.PlayerId).FirstOrDefault().Key], Mediumshiper.ContactLimit[pc.PlayerId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
            //通灵师的提示（目标）
            if (Mediumshiper.ContactPlayer.ContainsKey(pc.PlayerId) && (!Mediumshiper.OnlyReceiveMsgFromCrew.GetBool() || pc.GetCustomRole().IsCrewmate()))
                AddMsg(string.Format(GetString("MediumshipNotifyTarget"), Main.AllPlayerNames[Mediumshiper.ContactPlayer[pc.PlayerId]]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
            if (Main.VirusNotify.ContainsKey(pc.PlayerId))
                AddMsg(Main.VirusNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("VirusNoticeTitle")));
            if (Tracker.msgToSend.ContainsKey(pc.PlayerId))
                AddMsg(Tracker.msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), GetString("TrackerTitle")));
        }
        //宝箱怪的消息（合并）
        if (MimicMsg != "")
        {
            MimicMsg = GetString("MimicDeadMsg") + "\n" + MimicMsg;
            foreach (var ipc in Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostorTeam()))
                AddMsg(MimicMsg, ipc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mimic), GetString("MimicMsgTitle")));
        }

        msgToSend.Do(x => Logger.Info($"To:{x.Item2} {x.Item3} => {x.Item1}", "Skill Notice OnMeeting Start"));

        //总体延迟发送
        new LateTask(() => { msgToSend.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3)); }, 3f, "Skill Notice OnMeeting Start");

        Main.CyberStarDead.Clear();
        Main.DetectiveNotify.Clear();
        Main.VirusNotify.Clear();
        Mortician.msgToSend.Clear();
        Tracker.msgToSend.Clear();
    }
    public static void Prefix(MeetingHud __instance)
    {
        Logger.Info("------------会议开始------------", "Phase");
        ChatUpdatePatch.DoBlockChat = true;
        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
        MeetingStates.MeetingCalled = true;
    }
    public static void Postfix(MeetingHud __instance)
    {
        SoundManager.Instance.ChangeAmbienceVolume(0f);
        if (!GameStates.IsModHost) return;

        //提前储存赌怪游戏组件的模板
        GuessManager.textTemplate = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText);
        GuessManager.textTemplate.enabled = false;

        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null) continue;
            var RoleTextData = Utils.GetRoleText(PlayerControl.LocalPlayer.PlayerId, pc.PlayerId);
            var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
            roleTextMeeting.transform.SetParent(pva.NameText.transform);
            roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            roleTextMeeting.fontSize = 1.5f;
            roleTextMeeting.text = RoleTextData.Item1;
            if (Main.VisibleTasksCount) roleTextMeeting.text += Utils.GetProgressText(pc);
            roleTextMeeting.color = RoleTextData.Item2;
            roleTextMeeting.gameObject.name = "RoleTextMeeting";
            roleTextMeeting.enableWordWrapping = false;
            roleTextMeeting.enabled =
                pc.AmOwner || //対象がLocalPlayer
                (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) || //LocalPlayerが死亡していて幽霊が他人の役職を見れるとき
                (pc.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && Options.LoverKnowRoles.GetBool() && Amor.IsLoverPair(pc, PlayerControl.LocalPlayer)) ||
                (pc.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.ImpKnowAlliesRole.GetBool()) ||
                (pc.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosImp.GetBool()) ||
                (pc.Is(CustomRoles.Madmate) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.ImpKnowWhosMadmate.GetBool()) ||
                (pc.Is(CustomRoleTypes.Impostor) && PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor) && Options.AlliesKnowCrewpostor.GetBool()) ||
                (pc.Is(CustomRoles.Crewpostor) && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && Options.CrewpostorKnowsAllies.GetBool()) ||
                (pc.Is(CustomRoles.Madmate) && PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosMadmate.GetBool()) ||
                (pc.Is(CustomRoles.Jackal) && PlayerControl.LocalPlayer.Is(CustomRoles.Sidekick)) ||
                (pc.Is(CustomRoles.Sidekick) && PlayerControl.LocalPlayer.Is(CustomRoles.Sidekick) && Options.SidekickKnowOtherSidekick.GetBool() && Options.SidekickKnowOtherSidekickRole.GetBool()) ||
                (pc.Is(CustomRoles.Sidekick) && PlayerControl.LocalPlayer.Is(CustomRoles.Jackal)) ||
                (pc.Is(CustomRoles.Workaholic) && Options.WorkaholicVisibleToEveryone.GetBool()) ||
                (Totocalcio.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Lawyer.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Executioner.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Succubus.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Infectious.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Virus.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                (Amor.KnowRole(PlayerControl.LocalPlayer, pc)) ||
                PlayerControl.LocalPlayer.IsRevealedPlayer(pc) ||
                PlayerControl.LocalPlayer.Is(CustomRoles.God) ||
                PlayerControl.LocalPlayer.Is(CustomRoles.GM) ||
                Main.GodMode.Value;
            if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.IsRevealedPlayer(pc) && pc.Is(CustomRoles.Trickster))
            {
                roleTextMeeting.text = Farseer.RandomRole[PlayerControl.LocalPlayer.PlayerId];
                roleTextMeeting.text += Farseer.GetTaskState();
            }
            
            if (EvilTracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && EvilTracker.CanSeeLastRoomInMeeting)
            {
                roleTextMeeting.text = EvilTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.enabled = true;
            }
            if (Tracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && Tracker.CanSeeLastRoomInMeeting)
            {
                roleTextMeeting.text = Tracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.enabled = true;
            }
        }

        if (Options.SyncButtonMode.GetBool())
        {
            Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
            Logger.Info("紧急会议剩余 " + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + " 次使用次数", "SyncButtonMode");
        }
        if (AntiBlackout.OverrideExiledPlayer)
        {
            new LateTask(() =>
            {
                Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"), 255, Utils.ColorString(Color.red, GetString("DefaultSystemMessageTitle")));
            }, 5f, "Warning OverrideExiledPlayer");
        }
        if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
        TemplateManager.SendTemplate("OnMeeting", noErr: true);

        if (AmongUsClient.Instance.AmHost)
            NotifyRoleSkillOnMeetingStart();

        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                }
                ChatUpdatePatch.DoBlockChat = false;
            }, 3f, "SetName To Chat");
        }

        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;
            PlayerControl seer = PlayerControl.LocalPlayer;
            PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
            if (target == null) continue;

            var sb = new StringBuilder();

            //会議画面での名前変更
            //自分自身の名前の色を変更
            //NameColorManager準拠の処理
            pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);
            

            // Guesser Mode //
            if (Options.GuesserMode.GetBool())
            {
                if (Options.CrewmatesCanGuess.GetBool() && seer.GetCustomRole().IsCrewmate())
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                    pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;
                if (Options.ImpostorsCanGuess.GetBool() && seer.GetCustomRole().IsImpostor())
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                    pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;
                if (Options.NeutralKillersCanGuess.GetBool() && seer.GetCustomRole().IsNK())
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                    pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;
                if (Options.PassiveNeutralsCanGuess.GetBool() && seer.GetCustomRole().IsNonNK())
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                    pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;

            }
            //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

            if (seer.KnowDeathReason(target))
                sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");
            if (seer.KnowDeadTeam(target))
            {
                if (target.Is(CustomRoleTypes.Crewmate) && !target.Is(CustomRoles.Madmate) && target.Data.IsDead)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SpeedBooster), "★")); //Necroview
                if (target.Is(CustomRoleTypes.Impostor)  && target.Data.IsDead || target.Is(CustomRoles.Madmate) && target.Data.IsDead)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★")); //Necroview
                if (target.Is(CustomRoleTypes.Neutral) && target.Data.IsDead)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "★")); //Necroview


            }
            //インポスター表示
            switch (seer.GetCustomRole().GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate) && target.GetPlayerTaskState().IsTaskFinished)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★")); //変更対象にSnitchマークをつける
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoleTypes.Crewmate:
                    if (target.Is(CustomRoles.Marshall) && target.GetPlayerTaskState().IsTaskFinished)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Marshall), "★")); //変更対象にSnitchマークをつける
                    sb.Append(Marshall.GetWarningMark(seer, target));
                    break;
            }
            switch (seer.GetCustomRole())
            {
                case CustomRoles.Arsonist:
                    if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), "▲"));
                    break;
                case CustomRoles.Executioner:
                    sb.Append(Executioner.TargetMark(seer, target));
                    break;
                case CustomRoles.Lawyer:
                 //   sb.Append(Lawyer.TargetMark(seer, target));
                    break;
             //   case CustomRoles.Jackal:
             //   case CustomRoles.Sidekick:
                case CustomRoles.Poisoner:
                case CustomRoles.NSerialKiller:
                case CustomRoles.Pelican:
                case CustomRoles.DarkHide:
                case CustomRoles.BloodKnight:
                case CustomRoles.Infectious:
                case CustomRoles.Virus:
                case CustomRoles.Succubus:
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.Jackal:
                    if (target.Is(CustomRoles.Sidekick))
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), " ♥")); //変更対象にSnitchマークをつける
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.Monarch:
                    if (target.Is(CustomRoles.Knighted))
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Knighted), " 亗")); //変更対象にSnitchマークをつける
                    break;
                case CustomRoles.EvilTracker:
                    sb.Append(EvilTracker.GetTargetMark(seer, target));
                    break;
                case CustomRoles.Revolutionist:
                    if (seer.IsDrawPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), "●"));
                    break;
                case CustomRoles.Psychic:
                    if (target.IsRedForPsy(seer) && !seer.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), pva.NameText.text);
                    break;
                case CustomRoles.Mafia:
                    if (seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Retributionist:
                    if (seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Retributionist), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.NiceGuesser:
                case CustomRoles.EvilGuesser:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.Is(CustomRoles.NiceGuesser) ? CustomRoles.NiceGuesser : CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
           //     case CustomRoles.Guesser:
                case CustomRoles.Judge:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + pva.NameText.text;

                    break;
                case CustomRoles.Medicaler:
                    sb.Append(Medicaler.TargetMark(seer, target));
                    break;
                case CustomRoles.Gamer:
                    sb.Append(Gamer.TargetMark(seer, target));
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.Tracker:
                    sb.Append(Tracker.GetTargetMark(seer, target));
                    break;
                case CustomRoles.Amor:
                    sb.Append(Amor.GetLoversMark(seer, target));
                    break;
            }

            bool isLover = false;
            foreach (var subRole in target.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.Lovers:
                        if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                        {
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                            isLover = true;
                        }
                        break;
                    case CustomRoles.Sidekick:
                    if (seer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Sidekick) && Options.SidekickKnowOtherSidekick.GetBool())
                    {
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), " ♥")); //変更対象にSnitchマークをつける
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    }
                    break;
                 //   case CustomRoles.Guesser:
                //    if (!seer.Data.IsDead && !target.Data.IsDead)
                 //       pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;

                //    break;
                }
            }

            //海王相关显示
            if ((seer.Is(CustomRoles.Ntr) || target.Is(CustomRoles.Ntr)) && !seer.Data.IsDead && !isLover)
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
            else if (seer == target && CustomRolesHelper.RoleExist(CustomRoles.Ntr) && !isLover)
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));

            //呪われている場合
            sb.Append(Witch.GetSpelledMark(target.PlayerId, true));
            sb.Append(HexMaster.GetHexedMark(target.PlayerId, true));

            //如果是大明星
            if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★"));

            //球状闪电提示
            if (BallLightning.IsGhost(target))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BallLightning), "■"));

            //医生护盾提示
            if (seer.PlayerId == target.PlayerId)
                sb.Append(Medicaler.GetSheildMark(seer));

            //赌徒提示
            sb.Append(Totocalcio.TargetMark(seer, target));
            sb.Append(Lawyer.LawyerMark(seer, target));

            //会議画面ではインポスター自身の名前にSnitchマークはつけません。

            pva.NameText.text += sb.ToString();
            pva.ColorBlindName.transform.localPosition -= new Vector3(1.35f, 0f, 0f);
        }
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingHudUpdatePatch
{
    private static int bufferTime = 10;
    private static void ClearShootButton(MeetingHud __instance, bool forceAll = false)
     => __instance.playerStates.ToList().ForEach(x => { if ((forceAll || (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead)) && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

    public static void Postfix(MeetingHud __instance)
    {
        //Meeting Skip with vote counting on keystroke (m + delete)
        if (AmongUsClient.Instance.AmHost && Input.GetKeyDown(KeyCode.F6))
        {
            __instance.CheckForEndVoting();
        }
        //

        if (AmongUsClient.Instance.AmHost && Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
        {
            __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
            {
                var player = Utils.GetPlayerById(x.TargetPlayerId);
                if (player != null && !player.Data.IsDead)
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                }
            });
        }

        //投票结束时销毁全部技能按钮
        if (!GameStates.IsVoting && __instance.lastSecond < 1)
        {
            if (GameObject.Find("ShootButton") != null) ClearShootButton(__instance, true);
            return;
        }

        //会议技能UI处理
        bufferTime--;
        if (bufferTime < 0 && __instance.discussionTimer > 0)
        {
            bufferTime = 10;
            var myRole = PlayerControl.LocalPlayer.GetCustomRole();

            //若某玩家死亡则修复会议该玩家状态
            __instance.playerStates.Where(x => (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead) && !x.AmDead).Do(x => x.SetDead(x.DidReport, true));

            //若玩家死亡则销毁技能按钮
            if (myRole is CustomRoles.NiceGuesser or CustomRoles.EvilGuesser or CustomRoles.Judge or CustomRoles.Guesser && !PlayerControl.LocalPlayer.IsAlive())
                ClearShootButton(__instance, true);

            //若黑手党死亡则创建技能按钮
            if (myRole is CustomRoles.Mafia && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                MafiaRevengeManager.CreateJudgeButton(__instance);
            if (myRole is CustomRoles.Retributionist && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                RetributionistRevengeManager.CreateJudgeButton(__instance);

            //销毁死亡玩家身上的技能按钮
            ClearShootButton(__instance);

        }
    }
}
[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
class MeetingHudOnDestroyPatch
{
    public static void Postfix()
    {
        MeetingStates.FirstMeeting = false;
        Logger.Info("------------会议结束------------", "Phase");
        if (AmongUsClient.Instance.AmHost)
        {
            AntiBlackout.SetIsDead();
            Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);

            Main.LastVotedPlayerInfo = null;
            EAC.MeetingTimes = 0;
        }
    }
}