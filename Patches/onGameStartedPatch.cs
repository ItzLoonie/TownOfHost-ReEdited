using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Modules.CustomRoleSelector;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        Main.OverrideWelcomeMsg = "";
        try
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            if (Options.DisableVanillaRoles.GetBool())
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            }

            Main.PlayerStates = new();

            Main.AllPlayerKillCooldown = new();
            Main.AllPlayerSpeed = new();
            Main.AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            Main.WarlockTimer = new();
            Main.AssassinTimer = new();
            Main.isDoused = new();
            Main.isDraw = new();
            Main.isRevealed = new();
            Main.ArsonistTimer = new();
            Main.RevolutionistTimer = new();
            Main.RevolutionistStart = new();
            Main.RevolutionistLastTime = new();
            Main.RevolutionistCountdown = new();
            Main.TimeMasterBackTrack = new();
            Main.TimeMasterNum = new();
            Main.FarseerTimer = new();
            Main.CursedPlayers = new();
            Main.MafiaRevenged = new();
            Main.NecromancerRevenged = new();
            Main.RetributionistRevenged = new();
            Main.isCurseAndKill = new();
            Main.isCursed = false;
            Main.PuppeteerList = new();
            Main.CovenLeaderList = new();
            Main.TaglockedList = new();
            Main.ShroudList = new();
            Main.DetectiveNotify = new();
            Main.SleuthNotify = new();
            Main.ForCrusade = new();
            Main.KillGhoul = new();
            Main.CyberStarDead = new();
            Main.KilledDiseased = new();
            Main.KilledAntidote = new();
            Main.WorkaholicAlive = new();
            Main.BaitAlive = new();
            Main.BoobyTrapBody = new();
            Main.KillerOfBoobyTrapBody = new();
            Main.CleanerBodies = new();
            Main.BurstBodies = new();
            Main.MedusaBodies = new();
            Main.InfectedBodies = new();
            Main.VirusNotify = new();

            Main.LastEnteredVent = new();
            Main.LastEnteredVentLocation = new();
            Main.EscapeeLocation = new();

            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();
            Main.clientIdList = new();

            Main.CapitalismAddTask = new();
            Main.CapitalismAssignTask = new();
            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();
            Main.SpeedBoostTarget = new();
            Main.MayorUsedButtonCount = new();
            Main.ParaUsedButtonCount = new();
            Main.MarioVentCount = new();
            Main.VeteranInProtect = new();
            Main.VeteranNumOfUsed = new();
            Main.GrenadierNumOfUsed = new();
            Main.TimeMasterNumOfUsed = new();
            Main.GrenadierBlinding = new();
            Main.LighterNumOfUsed = new();
            Main.Lighter = new();
            Main.AllKillers = new();
            Main.MadGrenadierBlinding = new();
            Main.CursedWolfSpellCount = new();
            Main.JinxSpellCount = new();
            Main.OverDeadPlayerList = new();
            Main.Provoked = new();
            Main.ShieldPlayer = Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : byte.MaxValue;
            Main.FirstDied = byte.MaxValue;
            Main.MadmateNum = 0;
            Main.BardCreations = 0;
            Main.DovesOfNeaceNumOfUsed = new();
            Main.GodfatherTarget = new();
            Main.AwareInteracted = new();
            Main.ShamanTarget = byte.MaxValue;
            Main.ShamanTargetChoosen = false;

            ReportDeadBodyPatch.CanReport = new();

            Options.UsedButtonCount = 0;

            GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;
            GameOptionsManager.Instance.currentNormalGameOptions.TaskBarMode = (AmongUs.GameOptions.TaskBarMode)2;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.currentDousingTarget = byte.MaxValue;
            Main.currentDrawTarget = byte.MaxValue;
            Main.PlayerColors = new();

            //名前の記録
            //Main.AllPlayerNames = new();
            RPC.SyncAllPlayerNames();

            Camouflage.Init();
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.FormatNameMode.GetInt() == 1) pc.RpcSetName(Palette.GetColorName(colorId));
                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                //Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                Main.RefixCooldownDelay = 0;
            }
            FallFromLadder.Reset();
            BountyHunter.Init();
            SerialKiller.Init();
            EvilDiviner.Init();
            FireWorks.Init();
            Sniper.Init();
            TimeThief.Init();
        //    Mare.Init();
            Witch.Init();
            HexMaster.Init();
            Occultist.Init();
            SabotageMaster.Init();
            Executioner.Init();
            Lawyer.Init();
            Jackal.Init();
            Sidekick.Init();
            Sheriff.Init();
            CopyCat.Init();
            Cleanser.Init();
            SwordsMan.Init();
            EvilTracker.Init();
            Snitch.Init();
            Vampire.Init();
            Poisoner.Init();
            AntiAdminer.Init();
            Monitor.Init();
            TimeManager.Init();
            LastImpostor.Init();
            TargetArrow.Init();
            LocateArrow.Init();
            DoubleTrigger.Init();
            Workhorse.Init();
            Pelican.Init();
            Counterfeiter.Init();
            Pursuer.Init();
            Gangster.Init();
            Medic.Init();
            Gamer.Init();
            BallLightning.Init();
            DarkHide.Init();
            Greedier.Init();
            Collector.Init();
            QuickShooter.Init();
            Camouflager.Init();
            Divinator.Init();
            Jailer.Init();
            Oracle.Init();
            Eraser.Init();
            Assassin.Init();
            Sans.Init();
            Juggernaut.Init();
            Hacker.Init();
            Psychic.Init();
            Glitch.Init();
            Deputy.Init();
            Pickpocket.Init();
            Hangman.Init();
            Judge.Init();
            Councillor.Init();
            Mortician.Init();
            Mediumshiper.Init();
            Swooper.Init();
            Wraith.Init();
            Shade.Init();
            SoulCollector.Init();
            BloodKnight.Init();
            Banshee.Init();
            Totocalcio.Init();
            Romantic.Init();
            VengefulRomantic.Init();
            RuthlessRomantic.Init();
            Succubus.Init();
            Crusader.Init();
            CursedSoul.Init();
            Admirer.Init();
            Medusa.Init();
            Marshall.Init();
            Amnesiac.Init();
            Infectious.Init();
            Monarch.Init();
            Virus.Init();
            CovenLeader.Init();
            Bloodhound.Init();
            Tracker.Init();
            Merchant.Init();
            NSerialKiller.Init();
            Maverick.Init();
            Jinx.Init();
            DoubleShot.Init();
            Dazzler.Init();
            Addict.Init();
            Deathpact.Init();
            Tracefinder.Init();
            Devourer.Init();
            PotionMaster.Init();
            NWitch.Init();
            Traitor.Init();
            Spiritualist.Init();
            Vulture.Init();
            Chameleon.Init();
            Wildling.Init();
            Morphling.Init();
            ParityCop.Init(); // *giggle* party cop
            Baker.Init();
            Spiritcaller.Init();
            Lurker.Init();
            PlagueBearer.Init();
            Reverie.Init();
            Doomsayer.Init();
            Pirate.Init();
            Shroud.Init();
            Werewolf.Init();
            Chronomancer.Init();
            Seeker.Init();
            Pitfall.Init();
            Agitater.Init();

            SoloKombatManager.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            NameNotifyManager.Reset();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Change Role Setting Postfix");
            Logger.Fatal(ex.ToString(), "Change Role Setting Postfix");
        }
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            if (Main.EnableGM.Value)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }
                   

            SelectCustomRoles();
            SelectAddonRoles();
            CalculateVanillaRoleCount();

            //指定原版特殊职业数量
            var roleOpt = Main.NormalOptions.roleOptions;
            int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
            roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + addScientistNum, addScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));
            int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
            roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + addEngineerNum, addEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));
            int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
            roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + addShapeshifterNum, addShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

            Dictionary<(byte, byte), RoleTypes> rolesMap = new();

            // 注册反职业
            foreach (var kv in RoleResult.Where(x => x.Value.IsDesyncRole()))
                AssignDesyncRole(kv.Value, kv.Key, senders, rolesMap, BaseRole: kv.Value.GetDYRole());


            MakeDesyncSender(senders, rolesMap);

        }
        catch (Exception e)
        {
            Utils.ErrorEnd("Select Role Prefix");
            Logger.Fatal(e.Message, "Select Role Prefix");
        }
        //以下、バニラ側の役職割り当てが入る
    }

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            List<(PlayerControl, RoleTypes)> newList = new();
            foreach (var sd in RpcSetRoleReplacer.StoragedData)
            {
                var kp = RoleResult.Where(x => x.Key.PlayerId == sd.Item1.PlayerId).FirstOrDefault();
                newList.Add((sd.Item1, kp.Value.GetRoleTypes()));
                if (sd.Item2 == kp.Value.GetRoleTypes())
                    Logger.Warn($"注册原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                else
                    Logger.Warn($"覆盖原版职业 => {sd.Item1.GetRealName()}: {sd.Item2} => {kp.Value.GetRoleTypes()}", "Override Role Select");
            }
            if (Main.EnableGM.Value) newList.Add((PlayerControl.LocalPlayer, RoleTypes.Crewmate));
            RpcSetRoleReplacer.StoragedData = newList;

            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                Main.PlayerStates[pc.PlayerId].SetMainRole(role);
            }

            // 个人竞技模式用
            if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
            {
                foreach (var pair in Main.PlayerStates)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                goto EndOfSelectRolePatch;
            }

            var rd = IRandom.Instance;

            foreach (var kv in RoleResult)
            {
                if (kv.Value.IsDesyncRole()) continue;
                AssignCustomRole(kv.Value, kv.Key);
            }

            if (CustomRoles.Lovers.IsEnable() && (CustomRoles.FFF.IsEnable() ? -1 : rd.Next(1, 100)) <= Options.LoverSpawnChances.GetInt()) AssignLoversRolesFromList();
            foreach (var role in AddonRolesList)
            {
                if (rd.Next(1, 100) <= (Options.CustomAdtRoleSpawnRate.TryGetValue(role, out var sc) ? sc.GetFloat() : 0))
                    if (role.IsEnable()) AssignSubRoles(role);
            }

            //RPCによる同期
            foreach (var pair in Main.PlayerStates)
            {
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                foreach (var subRole in pair.Value.SubRoles)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
                switch (pc.GetCustomRole())
                {
                    case CustomRoles.BountyHunter:
                        BountyHunter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Reverie:
                        Reverie.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SerialKiller:
                        SerialKiller.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Witch:
                        Witch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.HexMaster:
                        HexMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Occultist:
                        Occultist.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Crusader:
                        Crusader.Add(pc.PlayerId);
                        Crusader.CrusaderLimit[pc.PlayerId] = Crusader.SkillLimitOpt.GetInt();
                        break;
                    case CustomRoles.Warlock:
                        Main.CursedPlayers.Add(pc.PlayerId, null);
                        Main.isCurseAndKill.Add(pc.PlayerId, false);
                        break;
                    case CustomRoles.FireWorks:
                        FireWorks.Add(pc.PlayerId);
                        break;
                    case CustomRoles.TimeThief:
                        TimeThief.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Camouflager:
                        Camouflager.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sniper:
                        Sniper.Add(pc.PlayerId);
                        break;
               /*     case CustomRoles.Mare:
                        Mare.Add(pc.PlayerId);
                        break; */
                    case CustomRoles.Vampire:
                        Vampire.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SwordsMan:
                        SwordsMan.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pickpocket:
                        Pickpocket.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Arsonist:
                        foreach (var ar in Main.AllPlayerControls)
                            Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                        break;
                    case CustomRoles.Revolutionist:
                        foreach (var ar in Main.AllPlayerControls)
                            Main.isDraw.Add((pc.PlayerId, ar.PlayerId), false);
                        break;
                    case CustomRoles.Farseer:
                        foreach (var ar in Main.AllPlayerControls)
                        { 
                            Main.isRevealed.Add((pc.PlayerId, ar.PlayerId), false);
                        }
                        Farseer.RandomRole.Add(pc.PlayerId, Farseer.GetRandomCrewRoleString());
                        Farseer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Executioner:
                        Executioner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Medusa:
                        Medusa.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Necromancer:
                        Necromancer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lawyer:
                        Lawyer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Jackal:
                        Jackal.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sidekick:
                        Sidekick.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Baker:
                        Baker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Poisoner:
                        Poisoner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sheriff:
                        Sheriff.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Marshall:
                        Marshall.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Glitch:
                        Glitch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CopyCat:
                        CopyCat.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Cleanser:
                        Cleanser.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Agitater:
                        Agitater.Add(pc.PlayerId);
                        break;
                    case CustomRoles.QuickShooter:
                        QuickShooter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mayor:
                        Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.TimeMaster:
                        Main.TimeMasterNum[pc.PlayerId] = 0;
                        Main.TimeMasterNumOfUsed.Add(pc.PlayerId, Options.TimeMasterMaxUses.GetInt());
                        break;
                    case CustomRoles.Masochist:
                        Main.MasochistKillMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Paranoia:
                        Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.SabotageMaster:
                        SabotageMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilTracker:
                        EvilTracker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Snitch:
                        Snitch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.AntiAdminer:
                        AntiAdminer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Monitor:
                        Monitor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mario:
                        Main.MarioVentCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.TimeManager:
                        TimeManager.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pelican:
                        Pelican.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Counterfeiter:
                        Counterfeiter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Jailer:
                        Jailer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pursuer:
                        Pursuer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Gangster:
                        Gangster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Medic:
                        Medic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilDiviner:
                        EvilDiviner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.PotionMaster:
                        PotionMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Divinator:
                        Divinator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Oracle:
                        Oracle.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Gamer:
                        Gamer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BallLightning:
                        BallLightning.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DarkHide:
                        DarkHide.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Greedier:
                        Greedier.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Collector:
                        Collector.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CursedWolf:
                        Main.CursedWolfSpellCount[pc.PlayerId] = Options.GuardSpellTimes.GetInt();
                        break;
                    case CustomRoles.Jinx:
                        Main.JinxSpellCount[pc.PlayerId] = Jinx.JinxSpellTimes.GetInt();
                        Jinx.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Eraser:
                        Eraser.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Assassin:
                        Assassin.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sans:
                        Sans.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Juggernaut:
                        Juggernaut.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hacker:
                        Hacker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Psychic:
                        Psychic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hangman:
                        Hangman.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Judge:
                        Judge.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Councillor:
                        Councillor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mortician:
                        Mortician.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Tracefinder:
                        Tracefinder.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mediumshiper:
                        Mediumshiper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Veteran:
                        Main.VeteranNumOfUsed.Add(pc.PlayerId, Options.VeteranSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Grenadier:
                        Main.GrenadierNumOfUsed.Add(pc.PlayerId, Options.GrenadierSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Swooper:
                        Swooper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Wraith:
                        Wraith.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Shade:
                        Shade.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lighter:
                        Main.LighterNumOfUsed.Add(pc.PlayerId, Options.LighterSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.SoulCollector:
                        SoulCollector.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Chameleon:
                        Chameleon.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BloodKnight:
                        BloodKnight.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Banshee:
                        Banshee.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Totocalcio:
                        Totocalcio.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Romantic:
                        Romantic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.VengefulRomantic:
                        VengefulRomantic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.RuthlessRomantic:
                        RuthlessRomantic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Succubus:
                        Succubus.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CursedSoul:
                        CursedSoul.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Admirer:
                        Admirer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Amnesiac:
                        Amnesiac.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DovesOfNeace:
                        Main.DovesOfNeaceNumOfUsed.Add(pc.PlayerId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Infectious:
                        Infectious.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Monarch:
                        Monarch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Deputy:
                        Deputy.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Virus:
                        Virus.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Wildling:
                        Wildling.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Bloodhound:
                        Bloodhound.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Tracker:
                        Tracker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Merchant:
                        Merchant.Add(pc.PlayerId);
                        break;
                    case CustomRoles.NSerialKiller:
                        NSerialKiller.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Werewolf:
                        Werewolf.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Traitor:
                        Traitor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.NWitch:
                        NWitch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CovenLeader:
                        CovenLeader.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Shroud:
                        Shroud.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Maverick:
                        Maverick.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Dazzler:
                        Dazzler.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Addict:
                        Addict.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Deathpact:
                        Deathpact.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Morphling:
                        Morphling.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Devourer:
                        Devourer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Spiritualist:
                        Spiritualist.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Vulture:
                        Vulture.Add(pc.PlayerId);
                        break;
                    case CustomRoles.PlagueBearer:
                        PlagueBearer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.ParityCop:
                        ParityCop.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Spiritcaller:
                        Spiritcaller.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lurker:
                        Lurker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Doomsayer:
                        Doomsayer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pirate:
                        Pirate.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Chronomancer:
                        Chronomancer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Seeker:
                        Seeker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pitfall:
                        Pitfall.Add(pc.PlayerId);
                        break;
                }
                foreach (var subRole in pc.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Aware:
                            Main.AwareInteracted[pc.PlayerId] = new();
                            break;
                        // ここに属性のAddを追加
                        default:
                            break;
                    }
                }
            }

        EndOfSelectRolePatch:

            HudManager.Instance.SetHudActive(true);
      //      HudManager.Instance.Chat.SetVisible(true);
            List<PlayerControl> AllPlayers = new();
            CustomRpcSender sender = CustomRpcSender.Create("SelectRoles Sender", SendOption.Reliable);
            foreach (var pc in Main.AllPlayerControls)
                pc.ResetKillCooldown();

            //役職の人数を戻す
            var roleOpt = Main.NormalOptions.roleOptions;
            int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
            ScientistNum -= addScientistNum;
            roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));
            int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
            EngineerNum -= addEngineerNum;
            roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));
            int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
            ShapeshifterNum -= addShapeshifterNum;
            roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:
                    GameEndChecker.SetPredicateToNormal();
                    break;
                case CustomGameMode.SoloKombat:
                    GameEndChecker.SetPredicateToSoloKombat();
                    break;
            }

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
            Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.Where(p => p.GetCustomRole() is CustomRoles.Arsonist or CustomRoles.NWitch or CustomRoles.Ritualist or CustomRoles.Revolutionist or CustomRoles.Farseer or CustomRoles.Sidekick or CustomRoles.Shaman or CustomRoles.KB_Normal).Select(p => p.PlayerId));
            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Postfix");
            Logger.Fatal(ex.ToString(), "Select Role Prefix");
        }
    }
    private static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (player == null) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);

        var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
        var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

        //Desync役職視点
        foreach (var target in Main.AllPlayerControls)
            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? othersRole : selfRole;

        //他者視点
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId))
            rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;

        RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
        //ホスト視点はロール決定
        player.SetRole(othersRole);
        player.Data.IsDead = true;

        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignRoles");
    }
    public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            var sender = senders[seer.PlayerId];
            foreach (var target in Main.AllPlayerControls)
            {
                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                {
                    sender.RpcSetRole(seer, role, target.GetClientId());
                }
            }
        }
    }

    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;
        SetColorPatch.IsAntiGlitchDisabled = true;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);
        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignRoles");

        SetColorPatch.IsAntiGlitchDisabled = false;
    }
    private static void ForceAssignRole(CustomRoles role, List<PlayerControl> AllPlayers, CustomRpcSender sender, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate, bool skip = false, int Count = -1)
    {
        var count = 1;

        if (Count != -1)
            count = Count;
        for (var i = 0; i < count; i++)
        {
            if (!AllPlayers.Any()) break;
            var rand = new Random();
            var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
            AllPlayers.Remove(player);
            Main.AllPlayerCustomRoles[player.PlayerId] = role;
            if (!skip)
            {
                if (!player.IsModClient())
                {
                    int playerCID = player.GetClientId();
                    sender.RpcSetRole(player, BaseRole, playerCID);
                    //Desyncする人視点で他プレイヤーを科学者にするループ
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == player) continue;
                        sender.RpcSetRole(pc, RoleTypes.Scientist, playerCID);
                    }
                    //他視点でDesyncする人の役職を科学者にするループ
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == player) continue;
                        if (pc.PlayerId == 0) player.SetRole(RoleTypes.Scientist); //ホスト視点用
                        else sender.RpcSetRole(player, RoleTypes.Scientist, pc.GetClientId());
                    }
                }
                else
                {
                    //ホストは別の役職にする
                    player.SetRole(hostBaseRole); //ホスト視点用
                    sender.RpcSetRole(player, hostBaseRole);
                }
            }
        }
    }

    private static void AssignLoversRolesFromList()
    {
        if (CustomRoles.Lovers.IsEnable())
        {
            //Loversを初期化
            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;
            //ランダムに2人選出
            AssignLoversRoles();
        }
    }
    private static void AssignLoversRoles(int RawCount = -1)
    {
        var allPlayers = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.GM) 
                || (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt()) 
                || pc.Is(CustomRoles.Ntr) 
                || pc.Is(CustomRoles.Dictator) 
                || pc.Is(CustomRoles.God) 
                || pc.Is(CustomRoles.FFF) 
                || pc.Is(CustomRoles.Sunnyboy)
                || pc.GetCustomRole().IsCoven()
                || pc.Is(CustomRoles.Bomber)
                || pc.Is(CustomRoles.Nuker) 
                || pc.Is(CustomRoles.Provocateur)
                || (pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeInLove.GetBool()))
                continue;
            allPlayers.Add(pc);
        }
        var role = CustomRoles.Lovers;
        var rd = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[rd.Next(0, allPlayers.Count)];
            Main.LoversPlayers.Add(player);
            allPlayers.Remove(player);
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info("注册恋人:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignLovers");
        }
        RPC.SyncLoversPlayers();
    }
    private static void AssignSubRoles(CustomRoles role, int RawCount = -1)
    {
        var allPlayers = Main.AllAlivePlayerControls.Where(x => CustomRolesHelper.CheckAddonConfilct(role, x)).ToList();
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[IRandom.Instance.Next(0, allPlayers.Count)];
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info("注册附加职业:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "Assign " + role.ToString());
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    private class RpcSetRoleReplacer
    {
        public static bool doReplace = false;
        public static Dictionary<byte, CustomRpcSender> senders;
        public static List<(PlayerControl, RoleTypes)> StoragedData = new();
        // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
        public static List<CustomRpcSender> OverriddenSenderList;
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (doReplace && senders != null)
            {
                StoragedData.Add((__instance, roleType));
                return false;
            }
            else return true;
        }
        public static void Release()
        {
            foreach (var sender in senders)
            {
                if (OverriddenSenderList.Contains(sender.Value)) continue;
                if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                    throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                foreach (var pair in StoragedData)
                {
                    pair.Item1.SetRole(pair.Item2);
                    sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                        .Write((ushort)pair.Item2)
                        .EndRpc();
                }
                sender.Value.EndMessage();
            }
            doReplace = false;
        }
        public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
        {
            RpcSetRoleReplacer.senders = senders;
            StoragedData = new();
            OverriddenSenderList = new();
            doReplace = true;
        }
    }
}
