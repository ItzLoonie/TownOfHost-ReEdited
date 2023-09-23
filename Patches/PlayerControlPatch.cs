using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;
using TOHE.Roles.Double;

namespace TOHE;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
class CheckProtectPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");

        if (__instance.Is(CustomRoles.EvilSpirit))
        {
            if (target.Is(CustomRoles.Spiritcaller))
            {
                Spiritcaller.ProtectSpiritcaller();
            }
            else
            {
                Spiritcaller.HauntPlayer(target);
            }

            __instance.RpcResetAbilityCooldown();
            return true;
        }

        if (__instance.Is(CustomRoles.Sheriff))
        {
            if (__instance.Data.IsDead)
            {
                Logger.Info("守護をブロックしました。", "CheckProtect");
                return false;
            }
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
class CheckMurderPatch
{
    public static Dictionary<byte, float> TimeSinceLastKill = new();
    public static void Update()
    {
        for (byte i = 0; i < 15; i++)
        {
            if (TimeSinceLastKill.ContainsKey(i))
            {
                TimeSinceLastKill[i] += Time.deltaTime;
                if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
            }
        }
    }
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var killer = __instance; //読み替え変数

        Logger.Info($"{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "CheckMurder");

        //死人はキルできない
        if (killer.Data.IsDead)
        {
            Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
            return false;
        }

        //不正キル防止処理
        if (target.Data == null || //PlayerDataがnullじゃないか確認
            target.inVent || target.inMovingPlat //targetの状態をチェック
        )
        {
            Logger.Info("目标处于无法被击杀状态，击杀被取消", "CheckMurder");
            return false;
        }
        if (target.Data.IsDead) //同じtargetへの同時キルをブロック
        {
            Logger.Info("目标处于死亡状态，击杀被取消", "CheckMurder");
            return false;
        }
        if (MeetingHud.Instance != null) //会議中でないかの判定
        {
            Logger.Info("会议中，击杀被取消", "CheckMurder");
            return false;
        }

        var divice = 2000f;
        float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / divice * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
        //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
        //↓許可されない場合
        if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
        {
            Logger.Info("击杀间隔过短，击杀被取消", "CheckMurder");
            return false;
        }
        TimeSinceLastKill[killer.PlayerId] = 0f;
        if (target.Is(CustomRoles.Diseased))
        {
            if (Main.KilledDiseased.ContainsKey(killer.PlayerId))
            {
                // Key already exists, update the value
                Main.KilledDiseased[killer.PlayerId] += 1;
            }
            else
            {
                // Key doesn't exist, add the key-value pair
                Main.KilledDiseased.Add(killer.PlayerId, 1);
            }
        }
        if (target.Is(CustomRoles.Antidote))
        {
            if (Main.KilledAntidote.ContainsKey(killer.PlayerId))
            {
                // Key already exists, update the value
                Main.KilledAntidote[killer.PlayerId] += 1;// Main.AllPlayerKillCooldown.TryGetValue(killer.PlayerId, out float kcd) ? (kcd - Options.AntidoteCDOpt.GetFloat() > 0 ? kcd - Options.AntidoteCDOpt.GetFloat() : 0f) : 0f;
            }
            else
            {
                // Key doesn't exist, add the key-value pair
                Main.KilledAntidote.Add(killer.PlayerId, 1);// Main.AllPlayerKillCooldown.TryGetValue(killer.PlayerId, out float kcd) ? (kcd - Options.AntidoteCDOpt.GetFloat() > 0 ? kcd - Options.AntidoteCDOpt.GetFloat() : 0f) : 0f);
            }
        }

        if (target.Is(CustomRoles.Fragile))
        {
            if ((killer.GetCustomRole().IsImpostorTeamV3() && Options.ImpCanKillFragile.GetBool()) ||
                (killer.GetCustomRole().IsNeutral() && Options.NeutralCanKillFragile.GetBool()) ||
                (killer.GetCustomRole().IsCrewmate() && Options.CrewCanKillFragile.GetBool()))
            {
                if (Options.FragileKillerLunge.GetBool()) killer.RpcMurderPlayer(target);
                else target.RpcMurderPlayer(target);
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Shattered;
                target.SetRealKiller(target);
                killer.ResetKillCooldown();
                return false;
            }
        }

        if (target.Is(CustomRoles.Aware))
        {
            switch (killer.GetCustomRole())
            {
                case CustomRoles.EvilDiviner:
                case CustomRoles.Farseer:
                case CustomRoles.Ritualist:
                    if (!Main.AwareInteracted.ContainsKey(target.PlayerId)) Main.AwareInteracted.Add(target.PlayerId, new());
                    if (!Main.AwareInteracted[target.PlayerId].Contains(Utils.GetRoleName(killer.GetCustomRole()))) Main.AwareInteracted[target.PlayerId].Add(Utils.GetRoleName(killer.GetCustomRole()));
                    break;
            }
        }
        if (target.Is(CustomRoles.Shaman) && !killer.Is(CustomRoles.NWitch))
        {
            if (Main.ShamanTarget != byte.MaxValue && target.IsAlive())
            { 
                target = Utils.GetPlayerById(Main.ShamanTarget);
                Main.ShamanTarget = byte.MaxValue;
            }

        }
        
        if (killer.Is(CustomRoles.Chronomancer))
            Chronomancer.OnCheckMurder(killer);

        killer.ResetKillCooldown();

        //キル可能判定
        if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton())
        {
            Logger.Info(killer.GetNameWithRole() + "击杀者不被允许使用击杀键，击杀被取消", "CheckMurder");
            return false;
        }

        //実際のキラーとkillerが違う場合の入れ替え処理
        if (Sniper.IsEnable) Sniper.TryGetSniper(target.PlayerId, ref killer);
        if (killer != __instance) Logger.Info($"Real Killer={killer.GetNameWithRole()}", "CheckMurder");

        //鹈鹕肚子里的人无法击杀
        if (Pelican.IsEaten(target.PlayerId))
            return false;

        //阻止对活死人的操作

        // 赝品检查
        if (Counterfeiter.OnClientMurder(killer)) return false;
        if (Pursuer.OnClientMurder(killer)) return false;
        if (Addict.IsImmortal(target)) return false;

        //判定凶手技能
        if (killer.PlayerId != target.PlayerId)
        {
            //非自杀场景下才会触发
            switch (killer.GetCustomRole())
            {
                //==========内鬼阵营==========//
                case CustomRoles.BountyHunter: //必须在击杀发生前处理
                    BountyHunter.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.OnCheckMurder(killer);
                    break;
                case CustomRoles.Vampire:
                    if (!Vampire.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Poisoner:
                    if (!Poisoner.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Witness:
                    killer.SetKillCooldown();
                    if (Main.AllKillers.ContainsKey(target.PlayerId))
                        killer.Notify(GetString("WitnessFoundKiller"));
                    else killer.Notify(GetString("WitnessFoundInnocent"));
                    return false;
                case CustomRoles.Undertaker:
                    if (!Undertaker.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Warlock:
                    if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                    { //Warlockが変身時以外にキルしたら、呪われる処理
                        if (target.Is(CustomRoles.Needy) || target.Is(CustomRoles.Lazy)) return false;
                        Main.isCursed = true;
                        killer.SetKillCooldown();
                        //RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                        killer.RPCPlayCustomSound("Line");
                        Main.CursedPlayers[killer.PlayerId] = target;
                        Main.WarlockTimer.Add(killer.PlayerId, 0f);
                        Main.isCurseAndKill[killer.PlayerId] = true;
                        //RPC.RpcSyncCurseAndKill();
                        return false;
                    }
                    if (Main.CheckShapeshift[killer.PlayerId])
                    {//呪われてる人がいないくて変身してるときに通常キルになる
                        killer.RpcCheckAndMurder(target);
                        return false;
                    }
                    if (Main.isCurseAndKill[killer.PlayerId]) killer.RpcGuardAndKill(target);
                    return false;
                case CustomRoles.Assassin:
                    if (!Assassin.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Witch:
                    if (!Witch.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.HexMaster:
                    if (!HexMaster.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Occultist:
                    if (!Occultist.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Puppeteer:
                    if (!Puppeteer.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.NWitch:
                    if (!NWitch.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.CovenLeader:
                    if (!CovenLeader.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Shroud:
                    if (!Shroud.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Capitalism:
                    if (!Main.CapitalismAddTask.ContainsKey(target.PlayerId))
                        Main.CapitalismAddTask.Add(target.PlayerId, 0);
                    Main.CapitalismAddTask[target.PlayerId]++;
                    if (!Main.CapitalismAssignTask.ContainsKey(target.PlayerId))
                        Main.CapitalismAssignTask.Add(target.PlayerId, 0);
                    Main.CapitalismAssignTask[target.PlayerId]++;
                    Logger.Info($"资本主义 {killer.GetRealName()} 又开始祸害人了：{target.GetRealName()}", "Capitalism Add Task");
                    if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer); 
                    killer.SetKillCooldown();
                    return false;
           /*     case CustomRoles.Bomber:
                    return false; */
                case CustomRoles.Gangster:
                    if (Gangster.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.BallLightning:
                    if (BallLightning.CheckBallLightningMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Greedier:
                    Greedier.OnCheckMurder(killer);
                    break;
                case CustomRoles.QuickShooter:
                    QuickShooter.QuickShooterKill(killer);
                    break;
                case CustomRoles.Sans:
                    Sans.OnCheckMurder(killer);
                    break;
                case CustomRoles.Juggernaut:
                    Juggernaut.OnCheckMurder(killer);
                    break;
                case CustomRoles.Reverie:
                    Reverie.OnCheckMurder(killer);
                    break;
                case CustomRoles.Hangman:
                    if (!Hangman.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Swooper:
                    if (!Swooper.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Wraith:
                    if (!Wraith.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Shade:
                    if (!Shade.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Lurker:
                    Lurker.OnCheckMurder(killer);
                    break;
                case CustomRoles.Crusader:
                    Crusader.OnCheckMurder(killer, target);
                    return false;

                //==========中立阵营==========//
                case CustomRoles.Seeker: //必须在击杀发生前处理
                    Seeker.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.PlagueBearer:
                    if (!PlagueBearer.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Pirate:
                    if (!Pirate.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Doppelganger:
                    Doppelganger.OnCheckMurder(killer, target);
                    break;

                case CustomRoles.Arsonist:
                    killer.SetKillCooldown(Options.ArsonistDouseTime.GetFloat());
                    if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Revolutionist:
                    killer.SetKillCooldown(Options.RevolutionistDrawTime.GetFloat());
                    if (!Main.isDraw[(killer.PlayerId, target.PlayerId)] && !Main.RevolutionistTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.RevolutionistTimer.TryAdd(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.SetCurrentDrawTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Farseer:
                    killer.SetKillCooldown(Farseer.FarseerRevealTime.GetFloat());
                    if (!Main.isRevealed[(killer.PlayerId, target.PlayerId)] && !Main.FarseerTimer.ContainsKey(killer.PlayerId))
                    {
                        Main.FarseerTimer.TryAdd(killer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.SetCurrentRevealTarget(killer.PlayerId, target.PlayerId);
                    }
                    return false;
                case CustomRoles.Innocent:
                    target.RpcMurderPlayerV3(killer);
                    return false;
                case CustomRoles.Pelican:
                    if (Pelican.CanEat(killer, target.PlayerId))
                    {
                        Pelican.EatPlayer(killer, target);
                        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
                        killer.SetKillCooldown();
                        killer.RPCPlayCustomSound("Eat");
                        target.RPCPlayCustomSound("Eat");
                    }
                    return false;
                case CustomRoles.FFF:
                    if (!target.Is(CustomRoles.Lovers) && !target.Is(CustomRoles.Ntr))
                    {
                        killer.Data.IsDead = true;
                        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                        killer.RpcMurderPlayerV3(killer);
                        Main.PlayerStates[killer.PlayerId].SetDead();
                        Logger.Info($"{killer.GetRealName()} 击杀了非目标玩家，壮烈牺牲了（bushi）", "FFF");
                        return false;
                    }
                    break;
                case CustomRoles.Gamer:
                    Gamer.CheckGamerMurder(killer, target);
                    return false;
                case CustomRoles.DarkHide:
                    DarkHide.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.Provocateur:
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                    killer.RpcMurderPlayerV3(target);
                    killer.RpcMurderPlayerV3(killer);
                    killer.SetRealKiller(target);
                    Main.Provoked.TryAdd(killer.PlayerId, target.PlayerId);
                    return false;
                case CustomRoles.Totocalcio:
                    Totocalcio.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Romantic:
                    if (!Romantic.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.VengefulRomantic:
                    if (!VengefulRomantic.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Succubus:
                    Succubus.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.CursedSoul:
                    CursedSoul.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Admirer:
                    Admirer.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Imitator:
                    Imitator.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Infectious:
                    Infectious.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Monarch:
                    Monarch.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Deputy:
                    Deputy.OnCheckMurder(killer, target);
                    return false;
                case CustomRoles.Jackal:
                    if (Jackal.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Bandit:
                    if (!Bandit.OnCheckMurder(killer, target)) return false;
                    break;
                case CustomRoles.Shaman:
                    if (Main.ShamanTargetChoosen == false)
                    {
                        Main.ShamanTarget = target.PlayerId;
                        killer.RpcGuardAndKill(killer);
                        Main.ShamanTargetChoosen = true;
                    }
                    else killer.Notify(GetString("ShamanTargetAlreadySelected"));
                    return false;
                case CustomRoles.Agitater:
                    if (!Agitater.OnCheckMurder(killer, target))
                        return false;
                    break;

                //==========船员职业==========//
                case CustomRoles.Sheriff:
                    if (!Sheriff.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.Jailer:
                    if (!Jailer.OnCheckMurder(killer, target))
                        return false;
                    break;
                case CustomRoles.CopyCat:
                    if (!CopyCat.OnCheckMurder(killer, target))
                        return false;
                    break;
                
                case CustomRoles.SwordsMan:
                    if (!SwordsMan.OnCheckMurder(killer))
                        return false;
                    break;
                case CustomRoles.Medic:
                    Medic.OnCheckMurderFormedicaler(killer, target);
                    return false;
                case CustomRoles.Counterfeiter:
                    if (target.Is(CustomRoles.NSerialKiller)) return true;
                    if (Counterfeiter.CanBeClient(target) && Counterfeiter.CanSeel(killer.PlayerId))
                        Counterfeiter.SeelToClient(killer, target);
                    return false;
                case CustomRoles.Pursuer:
                    if (target.Is(CustomRoles.NSerialKiller)) return true;
                    if (Pursuer.CanBeClient(target) && Pursuer.CanSeel(killer.PlayerId))
                        Pursuer.SeelToClient(killer, target);
                    return false;
                case CustomRoles.ChiefOfPolice:
                    ChiefOfPolice.OnCheckMurder(killer, target);
                    return false;
            }
        }

        // 击杀前检查
        if (!killer.RpcCheckAndMurder(target, true))
            return false;
        if (Merchant.OnClientMurder(killer, target)) return false;


        if (killer.Is(CustomRoles.Virus)) Virus.OnCheckMurder(killer, target);
        else if (killer.Is(CustomRoles.Spiritcaller)) Spiritcaller.OnCheckMurder(target);

        // Consigliere
        if (killer.Is(CustomRoles.EvilDiviner))
        {
            
            if (!EvilDiviner.OnCheckMurder(killer, target))
                return false;
        }

        if (killer.Is(CustomRoles.Unlucky))
        {
            var Ue = IRandom.Instance;
            if (Ue.Next(0, 100) < Options.UnluckyKillSuicideChance.GetInt())
            {
                killer.RpcMurderPlayerV3(killer);
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                return false;
            }
        }
        if (killer.Is(CustomRoles.Ludopath))
        {
            var ran = IRandom.Instance;
            int KillCD = ran.Next(1, Options.LudopathRandomKillCD.GetInt());
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = KillCD;
            }
        }


        if (killer.Is(CustomRoles.Swift) && !target.Is(CustomRoles.Glitch) && !target.Is(CustomRoles.Pestilence))
        {
            target.RpcMurderPlayerV3(target);
            if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
            killer.SetKillCooldown();
            target.SetRealKiller(killer);
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                return false;
        }
        if (killer.Is(CustomRoles.BoobyTrap))
        {
            Main.BoobyTrapBody.Add(target.PlayerId);
        }
        if (killer.Is(CustomRoles.Clumsy))
        {
            var miss = IRandom.Instance;
            if (miss.Next(0, 100) < Options.ChanceToMiss.GetInt())
            {
                killer.RpcGuardAndKill(killer);
                killer.SetKillCooldown();
                    return false;
            }
        }
    /*    if (killer.Is(CustomRoles.Werewolf) && !target.Is(CustomRoles.Glitch) && !target.Is(CustomRoles.Pestilence))
        {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Werewolf.KillCooldownAfterKilling.GetFloat();
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Mauled;
        //    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        } */



        if (killer.Is(CustomRoles.Mare) && !Utils.IsActive(SystemTypes.Electrical))
        {
            return false;
        }
   /*     if (killer.Is(CustomRoles.Minimalism))
        {
            return true;
        } */

        if (killer.Is(CustomRoles.PotionMaster))
        {
            
            if (!PotionMaster.OnCheckMurder(killer, target))
                return false;
        }

        // 清道夫清理尸体
        if (killer.Is(CustomRoles.Scavenger))
        {
            if (!target.Is(CustomRoles.Pestilence))
            {
                target.RpcTeleport(new Vector2(Pelican.GetBlackRoomPS().x, Pelican.GetBlackRoomPS().y));
                target.SetRealKiller(killer);
                Main.PlayerStates[target.PlayerId].SetDead();
                target.RpcMurderPlayerV3(target);
                killer.SetKillCooldown();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Scavenger), GetString("KilledByScavenger")));
                return false;
            }
            if (target.Is(CustomRoles.Pestilence))
            {
                target.RpcMurderPlayerV3(target);
                target.SetRealKiller(killer);
                return false;
            }

        }
        // 肢解者肢解受害者
        if (killer.Is(CustomRoles.OverKiller) && killer.PlayerId != target.PlayerId)
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Dismembered;
            _ = new LateTask(() =>
            {
                if (!Main.OverDeadPlayerList.Contains(target.PlayerId)) Main.OverDeadPlayerList.Add(target.PlayerId);
                var ops = target.transform.position;
                var rd = IRandom.Instance;
                for (int i = 0; i < 20; i++)
                {
                    Vector2 location = new(ops.x + ((float)(rd.Next(0, 201) - 100) / 100), ops.y + ((float)(rd.Next(0, 201) - 100) / 100));
                    location += new Vector2(0, 0.3636f);

                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.None, -1);
                    NetHelpers.WriteVector2(location, writer);
                    writer.Write(target.NetTransform.lastSequenceId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    target.NetTransform.SnapTo(location);
                    killer.MurderPlayer(target);

                    if (target.Is(CustomRoles.Avanger))
                    {
                        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId || Pelican.IsEaten(x.PlayerId) || Medic.ProtectList.Contains(x.PlayerId) || target.Is(CustomRoles.Pestilence) || target.Is(CustomRoles.Glitch)).ToList();
                        var rp = pcList[IRandom.Instance.Next(0, pcList.Count)];
                        Main.PlayerStates[rp.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
                        rp.SetRealKiller(target);
                        rp.RpcMurderPlayerV3(rp);
                    }

                    MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
                    messageWriter.WriteNetObject(target);
                    AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
                }
                killer.RpcTeleport(ops);
            }, 0.05f, "OverKiller Murder");
        }

        if (killer.Is(CustomRoles.Cultivator))
        {
            if (Main.CultivatorKillMax[killer.PlayerId] < Options.CultivatorMax.GetInt())
            {
                Main.CultivatorKillMax[killer.PlayerId]++;
                killer.Notify(string.Format(GetString("CultivatorLevelChanged"), Main.CultivatorKillMax[killer.PlayerId]));
                Logger.Info($"Increased the lvl to {Main.CultivatorKillMax[killer.PlayerId]}", "CULTIVATOR");
            }
            else
            {
                killer.Notify(GetString("CultivatorMaxReached"));
                Logger.Info($"Max level reached lvl =  {Main.CultivatorKillMax[killer.PlayerId]}", "CULTIVATOR");

            }
            if (Main.CultivatorKillMax[killer.PlayerId] >= Options.CultivatorKillCooldownLevel.GetInt() && Options.CultivatorOneCanKillCooldown.GetBool())
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.CultivatorOneKillCooldown.GetFloat();
            }
            if (Main.CultivatorKillMax[killer.PlayerId] == Options.CultivatorScavengerLevel.GetInt() && Options.CultivatorTwoCanScavenger.GetBool())
            {
                killer.RpcTeleport(target.transform.position);
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                target.RpcTeleport(new Vector2(Pelican.GetBlackRoomPS().x, Pelican.GetBlackRoomPS().y));
                target.SetRealKiller(killer);
                Main.PlayerStates[target.PlayerId].SetDead();
                target.RpcMurderPlayerV3(target);
                killer.SetKillCooldownV2();
                NameNotifyManager.Notify(target, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultivator), GetString("KilledByCultivator")));
                return false;
            }
            if (Main.CultivatorKillMax[killer.PlayerId] >= Options.CultivatorBomberLevel.GetInt() && Options.CultivatorThreeCanBomber.GetBool())
            {
                Logger.Info("炸弹爆炸了", "Boom");
                CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsModClient()) player.KillFlash();
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    if (player == killer) continue;
                    if (Vector2.Distance(killer.transform.position, player.transform.position) <= Options.BomberRadius.GetFloat())
                    {
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        player.SetRealKiller(killer);
                        player.RpcMurderPlayerV3(player);
                    }
                }
            }
            //if (Main.CultivatorKillMax[killer.PlayerId] == 4 && Options.CultivatorFourCanFlash.GetBool())
            //{
            //    Main.AllPlayerSpeed[killer.PlayerId] = Options.CultivatorSpeed.GetFloat();
            //}
        }

            if (killer.Is(CustomRoles.Werewolf))
            {
                Logger.Info("Werewolf Kill", "Mauled");
                {
                _ = new LateTask(() =>
                    {
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                            if (player == killer) continue;
                            if (player.Is(CustomRoles.NiceMini) && Mini.Age != 18) continue;
                            if (player.Is(CustomRoles.EvilMini) && Mini.Age != 18) continue;
                            if (player.Is(CustomRoles.Pestilence)) continue;
                            if (Vector2.Distance(killer.transform.position, player.transform.position) <= Werewolf.MaulRadius.GetFloat())
                                {
                                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Mauled;
                                player.SetRealKiller(killer);
                                player.RpcMurderPlayerV3(player);
                            }
                        }
                    }, 0.1f, "Werewolf Maul Bug Fix");

                }
            }


        //==キル処理==
        __instance.RpcMurderPlayerV3(target);
        //============

        return false;
    }

    public static bool RpcCheckAndMurder(PlayerControl killer, PlayerControl target, bool check = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (target == null) target = killer;

        //Jackal can kill Sidekick
        if (killer.Is(CustomRoles.Jackal) && target.Is(CustomRoles.Sidekick) && !Jackal.JackalCanKillSidekick.GetBool())
            return false;
        //Sidekick can kill Jackal
        if (killer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Jackal) && !Jackal.SidekickCanKillJackal.GetBool())
            return false;
        if (killer.Is(CustomRoles.Jackal) && target.Is(CustomRoles.Recruit) && !Jackal.JackalCanKillSidekick.GetBool())
            return false;
        //Sidekick can kill Jackal
        if (killer.Is(CustomRoles.Recruit) && target.Is(CustomRoles.Jackal) && !Jackal.SidekickCanKillJackal.GetBool())
            return false;
        //禁止内鬼刀叛徒
        if (killer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && !Options.ImpCanKillMadmate.GetBool())
            return false;

        // Guardian can't die on task completion
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted())
            return false;

        // Romantic partner is protected
        if (Romantic.BetPlayer.ContainsValue(target.PlayerId) && Romantic.isPartnerProtected) return false;

        if (Options.OppoImmuneToAttacksWhenTasksDone.GetBool())
        {
            if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted())
            return false;
        }

        // Monarch immune to kills when a living player is knighted
        if (target.Is(CustomRoles.Monarch) && CustomRoles.Knighted.RoleExist())
            return false;


        // Traitor can't kill Impostors but Impostors can kill it
        if (killer.Is(CustomRoles.Traitor) && target.Is(CustomRoleTypes.Impostor))
            return false;

        // Friendly Fire: OFF
        if (killer.Is(CustomRoles.NSerialKiller) && target.Is(CustomRoles.NSerialKiller))
            return false;
        if (killer.Is(CustomRoles.Juggernaut) && target.Is(CustomRoles.Juggernaut))
            return false;
        if (killer.Is(CustomRoles.Werewolf) && target.Is(CustomRoles.Werewolf))
            return false;
        if (killer.Is(CustomRoles.NWitch) && target.Is(CustomRoles.NWitch))
            return false;
        if (killer.Is(CustomRoles.Shroud) && target.Is(CustomRoles.Shroud))
            return false;
        if (killer.Is(CustomRoles.Jinx) && target.Is(CustomRoles.Jinx))
            return false;
        if (killer.Is(CustomRoles.Wraith) && target.Is(CustomRoles.Wraith))
            return false;
        if (killer.Is(CustomRoles.Shade) && target.Is(CustomRoles.Shade))
            return false;
        if (killer.Is(CustomRoles.HexMaster) && target.Is(CustomRoles.HexMaster))
            return false;
        if (killer.Is(CustomRoles.Occultist) && target.Is(CustomRoles.Occultist))
            return false;
        if (killer.Is(CustomRoles.BloodKnight) && target.Is(CustomRoles.BloodKnight))
            return false;
        if (killer.Is(CustomRoles.Jackal) && target.Is(CustomRoles.Jackal))
            return false;
        if (killer.Is(CustomRoles.Pelican) && target.Is(CustomRoles.Pelican))
            return false;
        if (killer.Is(CustomRoles.Poisoner) && target.Is(CustomRoles.Poisoner))
            return false;
        if (killer.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infectious))
            return false;
        if (killer.Is(CustomRoles.Virus) && target.Is(CustomRoles.Virus))
            return false;
        if (killer.Is(CustomRoles.Parasite) && target.Is(CustomRoles.Parasite))
            return false;
        if (killer.Is(CustomRoles.Traitor) && target.Is(CustomRoles.Traitor))
            return false;
        if (killer.Is(CustomRoles.DarkHide) && target.Is(CustomRoles.DarkHide))
            return false;
        if (killer.Is(CustomRoles.Pickpocket) && target.Is(CustomRoles.Pickpocket))
            return false;
        if (killer.Is(CustomRoles.Spiritcaller) && target.Is(CustomRoles.Spiritcaller))
            return false;
        if (killer.Is(CustomRoles.Medusa) && target.Is(CustomRoles.Medusa))
            return false;
        if (killer.Is(CustomRoles.PotionMaster) && target.Is(CustomRoles.PotionMaster))
            return false;
        if (killer.Is(CustomRoles.Glitch) && target.Is(CustomRoles.Glitch))
            return false;
        if (killer.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Succubus))
            return false;
        if (killer.Is(CustomRoles.Refugee) && target.Is(CustomRoles.Refugee))
            return false;



        //禁止叛徒刀内鬼
        if (killer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && !Options.MadmateCanKillImp.GetBool())
            return false;
        //Bitten players cannot kill Vampire
        if (killer.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infectious))
            return false;
        //Vampire cannot kill bitten players
        if (killer.Is(CustomRoles.Infectious) && target.Is(CustomRoles.Infected))
            return false;
        //Bitten players cannot kill each other
        if (killer.Is(CustomRoles.Infected) && target.Is(CustomRoles.Infected) && !Infectious.TargetKnowOtherTarget.GetBool())
            return false;
        //Sidekick can kill Sidekick
        if (killer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Sidekick) && !Jackal.SidekickCanKillSidekick.GetBool())
            return false;
        //Recruit can kill Recruit
        if (killer.Is(CustomRoles.Recruit) && target.Is(CustomRoles.Recruit) && !Jackal.SidekickCanKillSidekick.GetBool())
            return false;
        //Sidekick can kill Sidekick
        if (killer.Is(CustomRoles.Recruit) && target.Is(CustomRoles.Sidekick) && !Jackal.SidekickCanKillSidekick.GetBool())
            return false;
        //Recruit can kill Recruit
        if (killer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Recruit) && !Jackal.SidekickCanKillSidekick.GetBool())
            return false;


        //医生护盾检查
        if (Medic.OnCheckMurder(killer, target))
            return false;

        if (target.Is(CustomRoles.Medic))
            Medic.IsDead(target);
        if (PlagueBearer.OnCheckMurderPestilence(killer, target))
            return false;

        if (Jackal.ResetKillCooldownWhenSbGetKilled.GetBool() && !killer.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Sidekick) && !killer.Is(CustomRoles.Jackal) && !target.Is(CustomRoles.Jackal) && !GameStates.IsMeeting)
            Jackal.AfterPlayerDiedTask(killer);

        //迷你船员岁数检查
        if (target.Is(CustomRoles.NiceMini) && Mini.Age != 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("Cantkillkid")));
            return false;
        }
        if (target.Is(CustomRoles.EvilMini) && Mini.Age != 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("Cantkillkid")));
            return false;
        }
        if (killer.Is(CustomRoles.EvilMini) && Mini.Age != 18)
        {
            Main.EvilMiniKillcooldown[killer.PlayerId] = Mini.MinorCD.GetFloat();
            Main.AllPlayerKillCooldown[killer.PlayerId] = Mini.MinorCD.GetFloat();
            Main.EvilMiniKillcooldownf = Mini.MinorCD.GetFloat();
            killer.MarkDirtySettings();
            killer.SetKillCooldown();
            return true;
        }
        if (killer.Is(CustomRoles.EvilMini) && Mini.Age == 18)
        {
            Main.AllPlayerKillCooldown[killer.PlayerId] = Mini.MajorCD.GetFloat();
            killer.MarkDirtySettings();
            killer.SetKillCooldown();
            return true;
        }

    /*    if (target.Is(CustomRoles.BoobyTrap) && Options.TrapOnlyWorksOnTheBodyBoobyTrap.GetBool() && !GameStates.IsMeeting)
        {
            Main.BoobyTrapBody.Add(target.PlayerId);
            Main.BoobyTrapKiller.Add(target.PlayerId);
        } */

        if (target.Is(CustomRoles.Lucky))
        {
            var rd = IRandom.Instance;
            if (rd.Next(0, 100) < Options.LuckyProbability.GetInt())
            {
                killer.RpcGuardAndKill(target);
                return false;
            }
        }
      //  if (target.Is(CustomRoles.Diseased))
      //  {
            
      ////      killer.RpcGuardAndKill(killer);
      //   //   killer.SetKillCooldownV3(Main.AllPlayerKillCooldown[killer.PlayerId] *= Options.DiseasedMultiplier.GetFloat());
      //   //   killer.ResetKillCooldown();
      //  //    killer.SyncSettings();
      //  }
        if (Main.ForCrusade.Contains(target.PlayerId))
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.Crusader) && player.IsAlive() && !killer.Is(CustomRoles.Pestilence) && !killer.Is(CustomRoles.Glitch) && !killer.Is(CustomRoles.Minimalism))
                {
                    player.RpcMurderPlayerV3(killer);
                    Main.ForCrusade.Remove(target.PlayerId);
                    killer.RpcGuardAndKill(target);
                    return false;
                }
                if (player.Is(CustomRoles.Crusader) && player.IsAlive() && killer.Is(CustomRoles.Pestilence))
                {
                    killer.RpcMurderPlayerV3(player);
                    Main.ForCrusade.Remove(target.PlayerId);
                    target.RpcGuardAndKill(killer);
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                    return false;
                }
            }
        }

        switch (target.GetCustomRole())
        {
            //击杀幸运儿
            case CustomRoles.Luckey:
                var rd = IRandom.Instance;
                if (rd.Next(0, 100) < Options.LuckeyProbability.GetInt())
                {
                    killer.RpcGuardAndKill(target);
                    return false;
                }
                break;
            //击杀呪狼
            case CustomRoles.CursedWolf:
                if (Main.CursedWolfSpellCount[target.PlayerId] <= 0) break;
                if (killer.Is(CustomRoles.Pestilence)) break;
                if (killer == target) break;
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(target);
                Main.CursedWolfSpellCount[target.PlayerId] -= 1;
                killer.SetRealKiller(target);
                RPC.SendRPCCursedWolfSpellCount(target.PlayerId);
                Logger.Info($"{target.GetNameWithRole()} : {Main.CursedWolfSpellCount[target.PlayerId]}回目", "CursedWolf");
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Curse;
                killer.RpcMurderPlayerV3(killer);
                return false;
            case CustomRoles.Jinx:
                if (Main.JinxSpellCount[target.PlayerId] <= 0) break;
                if (killer.Is(CustomRoles.Pestilence)) break;
                if (killer == target) break;
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(target);
                Main.JinxSpellCount[target.PlayerId] -= 1;
                killer.SetRealKiller(target);
                RPC.SendRPCJinxSpellCount(target.PlayerId);
                Logger.Info($"{target.GetNameWithRole()} : {Main.JinxSpellCount[target.PlayerId]}回目", "Jinx");
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Jinx;
                killer.RpcMurderPlayerV3(killer);
                return false;
            //击杀老兵
            case CustomRoles.Veteran:
                if (Main.VeteranInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                    if (Main.VeteranInProtect[target.PlayerId] + Options.VeteranSkillDuration.GetInt() >= Utils.GetTimeStamp())
                    {
                        if (!killer.Is(CustomRoles.Pestilence))
                        {
                            killer.SetRealKiller(target);
                            target.RpcMurderPlayerV3(killer);
                            Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran Kill");
                            return false;
                        }
                        if (killer.Is(CustomRoles.Pestilence))
                        {
                            target.SetRealKiller(killer);
                            killer.RpcMurderPlayerV3(target);
                            Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{target.GetRealName()}", "Pestilence Reflect");
                            return false;
                        }
                    }
                break;
    /*        case CustomRoles.NSerialKiller:
            if (NSerialKiller.ReflectHarmfulInteractions.GetBool())
            {
                if (killer.Is(CustomRoles.Deputy))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);                    
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                }
                if (killer.Is(CustomRoles.Pursuer))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);                    
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                }
                if (killer.Is(CustomRoles.Counterfeiter))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);                    
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                }
                if (killer.Is(CustomRoles.Infectious))
                {
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);                    
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                }
            }
            break; */
            case CustomRoles.TimeMaster:
                if (Main.TimeMasterInProtect.ContainsKey(target.PlayerId) && killer.PlayerId != target.PlayerId)
                    if (Main.TimeMasterInProtect[target.PlayerId] + Options.TimeMasterSkillDuration.GetInt() >= Utils.GetTimeStamp(DateTime.UtcNow))
                    {
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (!killer.Is(CustomRoles.Pestilence))
                            {
                                if (Main.TimeMasterBackTrack.ContainsKey(player.PlayerId))
                                {
                                    var position = Main.TimeMasterBackTrack[player.PlayerId];
                                    player.RpcTeleport(new Vector2(position.x, position.y));
                                }
                            }
                        }
                        killer.SetKillCooldown(target: target, forceAnime: true);
                        return false;
                    }
                break;
            case CustomRoles.Masochist:
            
                    killer.SetKillCooldown(target: target, forceAnime: true);
                    Main.MasochistKillMax[target.PlayerId]++;
            //    killer.RPCPlayCustomSound("DM");
                target.Notify(string.Format(GetString("MasochistKill"), Main.MasochistKillMax[target.PlayerId]));
                    if (Main.MasochistKillMax[target.PlayerId] >= Options.MasochistKillMax.GetInt())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                        CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                    }
                return false;
            case CustomRoles.Cultivator:
                if (Main.CultivatorKillMax[killer.PlayerId] >= Options.CultivatorImmortalLevel.GetInt() && Options.CultivatorFourCanNotKill.GetBool())
                {
                    killer.RpcTeleport(target.transform.position);
                    RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                    killer.SetKillCooldown(target: target, forceAnime: true);
                    return false;
                }
                break;
            case CustomRoles.Glitch:
                    if (killer.Is(CustomRoles.Pestilence)) break;
                    killer.SetRealKiller(target);
                    target.RpcMurderPlayerV3(killer);
                    Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Glitch Kill");
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Hack;
                    return false;             
            //检查明星附近是否有人
            case CustomRoles.SuperStar:
                if (Main.AllAlivePlayerControls.Where(x =>
                    x.PlayerId != killer.PlayerId &&
                    x.PlayerId != target.PlayerId &&
                    Vector2.Distance(x.transform.position, target.transform.position) < 2f
                    ).ToList().Count >= 1) return false;
                break;
            //玩家被击杀事件
            case CustomRoles.Gamer:
                if (!Gamer.CheckMurder(killer, target))
                    return false;
                break;
            //嗜血骑士技能生效中
            case CustomRoles.BloodKnight:
                if (BloodKnight.InProtect(target.PlayerId))
                {
                    killer.RpcGuardAndKill(target);
                    if (!Options.DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
                    target.Notify(GetString("BKOffsetKill"));
                    return false;
                }
                break;
            case CustomRoles.Banshee:
                if (Banshee.InProtect(target.PlayerId))
                {
                    killer.RpcGuardAndKill(target);
                    if (!Options.DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
                    target.Notify(GetString("BKOffsetKill"));
                    return false;
                }
                break;
            case CustomRoles.Wildling:
                if (Wildling.InProtect(target.PlayerId))
                {
                    killer.RpcGuardAndKill(target);
                    if (!Options.DisableShieldAnimations.GetBool()) target.RpcGuardAndKill();
                    target.Notify(GetString("BKOffsetKill"));
                    return false;
                }
                break;
            case CustomRoles.Spiritcaller:
                if (Spiritcaller.InProtect(target))
                {
                    killer.RpcGuardAndKill(target);
                    target.RpcGuardAndKill();
                    return false;
                }
                break;
        }

        //保镖保护
        if (killer.PlayerId != target.PlayerId)
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
            {
                var pos = target.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > Options.BodyguardProtectRadius.GetFloat()) continue;
                if (pc.Is(CustomRoles.Bodyguard))
                {
                    if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                        Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard");
                    else
                    {
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                        pc.RpcMurderPlayerV3(killer);
                        pc.SetRealKiller(killer);
                        pc.RpcMurderPlayerV3(pc);
                        Logger.Info($"{pc.GetRealName()} 挺身而出与歹徒 {killer.GetRealName()} 同归于尽", "Bodyguard");
                        return false;
                    }
                }
                if (target.Is(CustomRoles.Cyber))
                {
                    if (Main.AllAlivePlayerControls.Where(x =>
                    x.PlayerId != killer.PlayerId &&
                    x.PlayerId != target.PlayerId &&
                    Vector2.Distance(x.transform.position, target.transform.position) < 2f
                    ).ToList().Count >= 1) 
                    return false;

                }
            }
        }

        //首刀保护
        if (Main.ShieldPlayer != byte.MaxValue && Main.ShieldPlayer == target.PlayerId && Utils.IsAllAlive)
        {
            Main.ShieldPlayer = byte.MaxValue;
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            //target.RpcGuardAndKill();
            return false;
        }

        //首刀叛变
        if (Options.MadmateSpawnMode.GetInt() == 1 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && Utils.CanBeMadmate(target))
        {
            Main.MadmateNum++;
            target.RpcSetCustomRole(CustomRoles.Madmate);
            ExtendedPlayerControl.RpcSetCustomRole(target.PlayerId, CustomRoles.Madmate);
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("BecomeMadmateCuzMadmateMode")));
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
            return false;
        }

        if (!check) killer.RpcMurderPlayerV3(target);
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardian ? "(Protected)" : "")}", "MurderPlayer");

        if (RandomSpawn.CustomNetworkTransformPatch.NumOfTP.TryGetValue(__instance.PlayerId, out var num) && num > 2) RandomSpawn.CustomNetworkTransformPatch.NumOfTP[__instance.PlayerId] = 3;
        if (!target.protectedByGuardian || !Doppelganger.DoppelVictim.ContainsKey(target.PlayerId))
            Camouflage.RpcSetSkin(target, ForceRevert: true);
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
        if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

        if (Main.OverDeadPlayerList.Contains(target.PlayerId)) return;

        PlayerControl killer = __instance; //読み替え変数

        if (Main.GodfatherTarget.Contains(target.PlayerId) && !(killer.GetCustomRole().IsImpostor() || killer.GetCustomRole().IsMadmate() || killer.Is(CustomRoles.Madmate)))
        {
            if (Options.GodfatherChangeOpt.GetValue() == 0) killer.RpcSetCustomRole(CustomRoles.Refugee);
            else killer.RpcSetCustomRole(CustomRoles.Madmate);
        }

        //実際のキラーとkillerが違う場合の入れ替え処理
        if (Sniper.IsEnable)
        {
            if (Sniper.TryGetSniper(target.PlayerId, ref killer))
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Sniped;
            }
        }
        if (killer != __instance)
        {
            Logger.Info($"Real Killer={killer.GetNameWithRole()}", "MurderPlayer");

        }
        if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.etc)
        {
            //死因が設定されていない場合は死亡判定
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
        }

        //看看UP是不是被首刀了
        if (Main.FirstDied == byte.MaxValue && target.Is(CustomRoles.Youtuber))
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Youtuber); //UP主被首刀了，哈哈哈哈哈
            CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
        }

        //记录首刀
        if (Main.FirstDied == byte.MaxValue)
            Main.FirstDied = target.PlayerId;

        if (target.Is(CustomRoles.Bait))
        {
            if (killer.PlayerId != target.PlayerId || (target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper or CustomRoles.Shade or CustomRoles.Wraith) || !killer.Is(CustomRoles.Oblivious) || (killer.Is(CustomRoles.Oblivious) && !Options.ObliviousBaitImmune.GetBool()))
            {
                killer.RPCPlayCustomSound("Congrats");
                target.RPCPlayCustomSound("Congrats");
                float delay;
                if (Options.BaitDelayMax.GetFloat() < Options.BaitDelayMin.GetFloat()) delay = 0f;
                else delay = IRandom.Instance.Next((int)Options.BaitDelayMin.GetFloat(), (int)Options.BaitDelayMax.GetFloat() + 1);
                delay = Math.Max(delay, 0.15f);
                if (delay > 0.15f && Options.BaitDelayNotify.GetBool()) killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
                Logger.Info($"{killer.GetNameWithRole()} 击杀诱饵 => {target.GetNameWithRole()}", "MurderPlayer");
                _ = new LateTask(() => { if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data); }, delay, "Bait Self Report");
            }
        }
        if (target.Is(CustomRoles.Burst) && !killer.Data.IsDead)
        {
            target.SetRealKiller(killer);
            Main.BurstBodies.Add(target.PlayerId);
            if (killer.PlayerId != target.PlayerId && !killer.Is(CustomRoles.Pestilence))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Burst), GetString("BurstNotify")));
                _ = new LateTask(() =>
                {
                    if (!killer.inVent && !killer.Data.IsDead && !GameStates.IsMeeting)
                    {
                        target.RpcMurderPlayerV3(killer);
                        killer.SetRealKiller(target);
                        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    }
                    else
                    {
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                        killer.SetKillCooldown(time : Main.AllPlayerKillCooldown[killer.PlayerId] - Options.BurstKillDelay.GetFloat(), forceAnime: true);
                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Burst), GetString("BurstFailed")));                        
                    }
                    Main.BurstBodies.Remove(target.PlayerId);
                }, Options.BurstKillDelay.GetFloat(), "Burst Suicide");
            }
        } 


        if (target.Is(CustomRoles.Trapper) && killer != target)
            killer.TrapperKilled(target);

        Main.AllKillers.Remove(killer.PlayerId);
        Main.AllKillers.Add(killer.PlayerId, Utils.GetTimeStamp());

        switch (target.GetCustomRole())
        {
            case CustomRoles.BallLightning:
                if (killer != target)
                    BallLightning.MurderPlayer(killer, target);
                break;
        }
        switch (killer.GetCustomRole())
        {
        /*    case CustomRoles.BoobyTrap:
                if (!Options.TrapOnlyWorksOnTheBodyBoobyTrap.GetBool() && killer != target)
                {
                    if (!Main.BoobyTrapBody.Contains(target.PlayerId)) Main.BoobyTrapBody.Add(target.PlayerId);
                    if (!Main.KillerOfBoobyTrapBody.ContainsKey(target.PlayerId)) Main.KillerOfBoobyTrapBody.Add(target.PlayerId, killer.PlayerId);
                    Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                    killer.RpcMurderPlayerV3(killer);
                }
                break; */
            case CustomRoles.SwordsMan:
                if (killer != target)
                    SwordsMan.OnMurder(killer);
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.OnMurderPlayer(killer, target);
                break;
            case CustomRoles.Banshee:
                Banshee.OnMurderPlayer(killer, target);
                break;
            case CustomRoles.Wildling:
                Wildling.OnMurderPlayer(killer, target);
                break;
        }

        if (killer.Is(CustomRoles.TicketsStealer) && killer.PlayerId != target.PlayerId)
            killer.Notify(string.Format(GetString("TicketsStealerGetTicket"), ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId) + 1) * Options.TicketsPerKill.GetFloat()).ToString("0.0#####")));
        
        if (killer.Is(CustomRoles.Pickpocket) && killer.PlayerId != target.PlayerId)
            killer.Notify(string.Format(GetString("PickpocketGetVote"), ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId) + 1) * Pickpocket.VotesPerKill.GetFloat()).ToString("0.0#####")));

        if (target.Is(CustomRoles.Avanger))
        {
            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToList();
            var rp = pcList[IRandom.Instance.Next(0, pcList.Count)];
            if (!rp.Is(CustomRoles.Pestilence))
            {
                Main.PlayerStates[rp.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
                rp.SetRealKiller(target);
                rp.RpcMurderPlayerV3(rp);
            }
        }

        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Mediumshiper)))
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshiperKnowPlayerDead")));

        if (Executioner.Target.ContainsValue(target.PlayerId))
            Executioner.ChangeRoleByTarget(target);

        if (target.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(target.PlayerId))
        {
            Executioner.Target.Remove(target.PlayerId);
            Executioner.SendRPC(target.PlayerId);
        }

        if (Lawyer.Target.ContainsValue(target.PlayerId))
            Lawyer.ChangeRoleByTarget(target);
        Hacker.AddDeadBody(target);
        Mortician.OnPlayerDead(target);
        Bloodhound.OnPlayerDead(target);
        Tracefinder.OnPlayerDead(target);
        Vulture.OnPlayerDead(target);
        SoulCollector.OnPlayerDead(target);

        Utils.AfterPlayerDeathTasks(target);

        Main.PlayerStates[target.PlayerId].SetDead();
        target.SetRealKiller(killer, true); //既に追加されてたらスキップ
        Utils.CountAlivePlayers(true);

        Camouflager.isDead(target);
        Utils.TargetDies(__instance, target);

        if (Options.LowLoadMode.GetBool())
        {
            __instance.MarkDirtySettings();
            target.MarkDirtySettings();
            Utils.NotifyRoles(SpecifySeer: killer);
            Utils.NotifyRoles(SpecifySeer: target);
        }
        else
        {
            Utils.SyncAllSettings();
            Utils.NotifyRoles();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
class ShapeshiftPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

        if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
        {
            Logger.Info($"{__instance?.GetNameWithRole()}:Cancel Shapeshift.Prefix", "Shapeshift");
            return;
        }

        Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
        Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

        if (!Pelican.IsEaten(shapeshifter.PlayerId))
        {
            switch (shapeshifter.GetCustomRole())
            {
                case CustomRoles.EvilTracker:
                    EvilTracker.OnShapeshift(shapeshifter, target, shapeshifting);
                    break;
                case CustomRoles.Sniper:
                    Sniper.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Undertaker:
                    Undertaker.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.FireWorks:
                    FireWorks.ShapeShiftState(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Warlock:
                    if (Main.CursedPlayers[shapeshifter.PlayerId] != null)
                    {
                        if (shapeshifting && !Main.CursedPlayers[shapeshifter.PlayerId].Data.IsDead)
                        {
                            var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                            Vector2 cppos = cp.transform.position;
                            Dictionary<PlayerControl, float> cpdistance = new();
                            float dis;
                            foreach (PlayerControl p in Main.AllAlivePlayerControls)
                            {
                                if (p.PlayerId == cp.PlayerId) continue;
                                if (!Options.WarlockCanKillSelf.GetBool() && p.PlayerId == shapeshifter.PlayerId) continue;
                                if (!Options.WarlockCanKillAllies.GetBool() && p.GetCustomRole().IsImpostor()) continue;
                                if (p.Is(CustomRoles.Glitch)) continue;
                                if (p.Is(CustomRoles.Pestilence)) continue;
                                if (Pelican.IsEaten(p.PlayerId) || Medic.ProtectList.Contains(p.PlayerId)) continue;
                                dis = Vector2.Distance(cppos, p.transform.position);
                                cpdistance.Add(p, dis);
                                Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                            }
                            if (cpdistance.Count >= 1)
                            {
                                var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                                PlayerControl targetw = min.Key;
                                if (cp.RpcCheckAndMurder(targetw, true))
                                {
                                    targetw.SetRealKiller(shapeshifter);
                                    Logger.Info($"{targetw.GetNameWithRole()}was killed", "Warlock");
                                    cp.RpcMurderPlayerV3(targetw);//殺す
                                    shapeshifter.RpcGuardAndKill(shapeshifter);
                                    shapeshifter.Notify(GetString("WarlockControlKill"));
                                }
                            }
                            else
                            {
                                shapeshifter.Notify(GetString("WarlockNoTarget"));
                            }
                            Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                        }
                        Main.CursedPlayers[shapeshifter.PlayerId] = null;
                    }
                    break;
                case CustomRoles.Escapee:
                    if (shapeshifting)
                    {
                        if (Main.EscapeeLocation.ContainsKey(shapeshifter.PlayerId))
                        {
                            var position = Main.EscapeeLocation[shapeshifter.PlayerId];
                            Main.EscapeeLocation.Remove(shapeshifter.PlayerId);
                            Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "EscapeeTeleport");
                            shapeshifter.RpcTeleport(new Vector2(position.x, position.y));
                            shapeshifter.RPCPlayCustomSound("Teleport");
                        }
                        else
                        {
                            Main.EscapeeLocation.Add(shapeshifter.PlayerId, new Vector2(shapeshifter.transform.position.x, shapeshifter.transform.position.y));
                        }
                    }
                    break;
                case CustomRoles.Miner:
                    if (Main.LastEnteredVent.ContainsKey(shapeshifter.PlayerId))
                    {
                        int ventId = Main.LastEnteredVent[shapeshifter.PlayerId].Id;
                        var vent = Main.LastEnteredVent[shapeshifter.PlayerId];
                        var position = Main.LastEnteredVentLocation[shapeshifter.PlayerId];
                        Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "MinerTeleport");
                        shapeshifter.RpcTeleport(new Vector2(position.x, position.y));
                    }
                    break;
                case CustomRoles.Bomber:
                    if (shapeshifting)
                    {
                        Logger.Info("炸弹爆炸了", "Boom");
                        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                        foreach (var tg in Main.AllPlayerControls)
                        {
                            if (!tg.IsModClient()) tg.KillFlash();
                            var pos = shapeshifter.transform.position;
                            var dis = Vector2.Distance(pos, tg.transform.position);

                            if (!tg.IsAlive() || Pelican.IsEaten(tg.PlayerId) || Medic.ProtectList.Contains(tg.PlayerId) || (tg.Is(CustomRoleTypes.Impostor) && Options.ImpostorsSurviveBombs.GetBool()) || tg.inVent || tg.Is(CustomRoles.Glitch) || tg.Is(CustomRoles.Pestilence)) continue;
                            if (dis > Options.BomberRadius.GetFloat()) continue;
                            if (tg.PlayerId == shapeshifter.PlayerId) continue;

                            Main.PlayerStates[tg.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                            tg.SetRealKiller(shapeshifter);
                            tg.RpcMurderPlayerV3(tg);
                            Medic.IsDead(tg);
                        }
                        _ = new LateTask(() =>
                        {
                            var totalAlive = Main.AllAlivePlayerControls.Count();
                            //自分が最後の生き残りの場合は勝利のために死なない
                            if (Options.BomberDiesInExplosion.GetBool())
                            {
                                if (totalAlive > 0 && !GameStates.IsEnded)
                                {
                                    Main.PlayerStates[shapeshifter.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                                    shapeshifter.RpcMurderPlayerV3(shapeshifter);
                                }
                            }
                            Utils.NotifyRoles();
                        }, 1.5f, "Bomber Suiscide");
                    }
                    break;
                case CustomRoles.Nuker:
                    if (shapeshifting)
                    {
                        Logger.Info("炸弹爆炸了", "Boom");
                        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
                        foreach (var tg in Main.AllPlayerControls)
                        {
                            if (!tg.IsModClient()) tg.KillFlash();
                            var pos = shapeshifter.transform.position;
                            var dis = Vector2.Distance(pos, tg.transform.position);

                            if (!tg.IsAlive() || Pelican.IsEaten(tg.PlayerId) || Medic.ProtectList.Contains(tg.PlayerId) || tg.inVent || tg.Is(CustomRoles.Glitch) || tg.Is(CustomRoles.Pestilence)) continue;
                            if (dis > Options.NukeRadius.GetFloat()) continue;
                            if (tg.PlayerId == shapeshifter.PlayerId) continue;

                            Main.PlayerStates[tg.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                            tg.SetRealKiller(shapeshifter);
                            tg.RpcMurderPlayerV3(tg);
                            Medic.IsDead(tg);
                        }
                        _ = new LateTask(() =>
                        {
                            var totalAlive = Main.AllAlivePlayerControls.Count();
                            //自分が最後の生き残りの場合は勝利のために死なない
                            //    if (Options.BomberDiesInExplosion.GetBool())
                            {
                                if (totalAlive > 0 && !GameStates.IsEnded)
                                {
                                    Main.PlayerStates[shapeshifter.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                                    shapeshifter.RpcMurderPlayerV3(shapeshifter);
                                }
                            }
                            Utils.NotifyRoles();
                        }, 1.5f, "Nuke");
                    }
                    break;
                case CustomRoles.Assassin:
                    Assassin.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.ImperiusCurse:
                    if (shapeshifting)
                    {
                        _ = new LateTask(() =>
                        {
                            if (!(!GameStates.IsInTask || !shapeshifter.IsAlive() || !target.IsAlive() || shapeshifter.inVent || target.inVent))
                            {
                                var originPs = target.transform.position;
                                target.RpcTeleport(shapeshifter.transform.position);
                                shapeshifter.RpcTeleport(originPs);
                            }
                        }, 1.5f, "ImperiusCurse TP");
                    }
                    break;
                case CustomRoles.QuickShooter:
                    QuickShooter.OnShapeshift(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Camouflager:
                    if (shapeshifting)
                        Camouflager.OnShapeshift();
                    if (!shapeshifting)
                        Camouflager.OnReportDeadBody();
                    break;
                case CustomRoles.Hacker:
                    Hacker.OnShapeshift(shapeshifter, shapeshifting, target);
                    break;
                case CustomRoles.Disperser:
                    if (shapeshifting)
                        Disperser.DispersePlayers(shapeshifter);
                    break;
                case CustomRoles.Dazzler:
                    if (shapeshifting)
                        Dazzler.OnShapeshift(shapeshifter, target);
                    break;
                case CustomRoles.Deathpact:
                    if (shapeshifting)
                        Deathpact.OnShapeshift(shapeshifter, target);
                    break;
                case CustomRoles.Devourer:
                    if (shapeshifting)
                        Devourer.OnShapeshift(shapeshifter, target);
                    break;
                case CustomRoles.Twister:
                    Twister.TwistPlayers(shapeshifter);
                    break;
                case CustomRoles.Pitfall:
                    if (shapeshifting)
                        Pitfall.OnShapeshift(shapeshifter);
                    break;
            }
        }

        //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
        if (!shapeshifting)
        {
            _ = new LateTask(() =>
            {
                Utils.NotifyRoles(NoCache: true);
            },
            1.2f, "ShapeShiftNotify");
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static Dictionary<byte, bool> CanReport;
    public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        if (GameStates.IsMeeting) return false;
        if (Options.DisableMeeting.GetBool()) return false;
        if (!CanReport[__instance.PlayerId])
        {
            WaitReport[__instance.PlayerId].Add(target);
            Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
            return false;
        }

        Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");

        foreach (var kvp in Main.PlayerStates)
        {
            var pc = Utils.GetPlayerById(kvp.Key);
            kvp.Value.LastRoom = pc.GetPlainShipRoom();
        }

        if (!AmongUsClient.Instance.AmHost) return true;

        try
        {
            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (__instance.Data.IsDead) return false;

            //=============================================
            //以下、检查是否允许本次会议
            //=============================================

            var killer = target?.Object?.GetRealKiller();
            var killerRole = killer?.GetCustomRole();

            //杀戮机器无法报告或拍灯
            //     if (__instance.Is(CustomRoles.Minimalism)) return false;

            // if Bait is killed, check the setting condition
            if (!(target != null && target.Object.Is(CustomRoles.Bait) && Options.BaitCanBeReportedUnderAllConditions.GetBool()))
            {
                // Camouflager
                if (Camouflager.DisableReportWhenCamouflageIsActive.GetBool() && Camouflager.IsActive && !(Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())) return false;

                // Comms Camouflage
                if (Options.DisableReportWhenCC.GetBool() && Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() &&
                    !(Options.DisableOnSomeMaps.GetBool() &&
                        ((Options.DisableOnSkeld.GetBool() && Options.IsActiveSkeld) ||
                         (Options.DisableOnMira.GetBool() && Options.IsActiveMiraHQ) ||
                         (Options.DisableOnPolus.GetBool() && Options.IsActivePolus) ||
                         (Options.DisableOnAirship.GetBool() && Options.IsActiveAirship)
                        ))) return false;
            }


            if (target == null) //拍灯事件
            {
                if (__instance.Is(CustomRoles.Jester) && !Options.JesterCanUseButton.GetBool()) return false;
                if (__instance.Is(CustomRoles.Swapper) && !Swapper.CanStartMeeting.GetBool()) return false;
            }
            if (target != null) //拍灯事件
            {
                if (Bloodhound.UnreportablePlayers.Contains(target.PlayerId)) return false;

                if (__instance.Is(CustomRoles.Bloodhound))
                {
                    if (killer != null)
                    {
                        Bloodhound.OnReportDeadBody(__instance, target, killer);
                    }
                    else
                    {
                        __instance.Notify(GetString("BloodhoundNoTrack"));
                    }
                    
                    return false;
                }
                if (Vulture.UnreportablePlayers.Contains(target.PlayerId)) return false;

                if (__instance.Is(CustomRoles.Vulture))
                {
                    long now = Utils.GetTimeStamp();
                    if ((Vulture.AbilityLeftInRound[__instance.PlayerId] > 0) && (now - Vulture.LastReport[__instance.PlayerId] > (long)Vulture.VultureReportCD.GetFloat()))
                    {
                        Vulture.LastReport[__instance.PlayerId] = now;

                        Vulture.OnReportDeadBody(__instance, target);
                        __instance.RpcGuardAndKill(__instance);
                        __instance.Notify(GetString("VultureReportBody"));
                        if (Vulture.AbilityLeftInRound[__instance.PlayerId] > 0)
                        {
                            _ = new LateTask(() =>
                            {
                                if (GameStates.IsInTask) 
                                { 
                                    if (!Options.DisableShieldAnimations.GetBool()) __instance.RpcGuardAndKill(__instance);
                                    __instance.Notify(GetString("VultureCooldownUp"));
                                }
                                return;
                            }, Vulture.VultureReportCD.GetFloat(), "Vulture CD");
                        }

                        Logger.Info($"{__instance.GetRealName()} ate {target.PlayerName} corpse", "Vulture");
                        return false;
                    }
                }

                // 清洁工来扫大街咯
                if (__instance.Is(CustomRoles.Cleaner))
                {
                    Main.CleanerBodies.Remove(target.PlayerId);
                    Main.CleanerBodies.Add(target.PlayerId);
                    __instance.RpcGuardAndKill(__instance);
                    __instance.Notify(GetString("CleanerCleanBody"));
              //      __instance.ResetKillCooldown();
                    __instance.SetKillCooldownV3(Options.KillCooldownAfterCleaning.GetFloat());
                    Logger.Info($"{__instance.GetRealName()} 清理了 {target.PlayerName} 的尸体", "Cleaner");
                    return false;
                }
                if (__instance.Is(CustomRoles.Medusa))
                {
                    Main.MedusaBodies.Remove(target.PlayerId);
                    Main.MedusaBodies.Add(target.PlayerId);
                    __instance.RpcGuardAndKill(__instance);
                    __instance.Notify(GetString("MedusaStoneBody"));
              //      __instance.ResetKillCooldown();
                    __instance.SetKillCooldownV3(Medusa.KillCooldownAfterStoneGazing.GetFloat());
                    Logger.Info($"{__instance.GetRealName()} stoned {target.PlayerName} body", "Medusa");
                    return false;
                }


                // 被赌杀的尸体无法被报告
                if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Gambled) return false;

                // 清道夫的尸体无法被报告
                if (killerRole == CustomRoles.Scavenger) return false;

                // 被清理的尸体无法报告
                if (Main.CleanerBodies.Contains(target.PlayerId)) return false;

                if (Main.MedusaBodies.Contains(target.PlayerId)) return false;

                // 胆小鬼不敢报告
                var tpc = Utils.GetPlayerById(target.PlayerId);
                if (__instance.Is(CustomRoles.Oblivious))
                {
                    if (!tpc.Is(CustomRoles.Bait) || (tpc.Is(CustomRoles.Bait) && Options.ObliviousBaitImmune.GetBool())) /* && (target?.Object != null)*/
                    {
                        return false;
                    } 
                }

                var tar = Utils.GetPlayerById(target.PlayerId);
                if (__instance.Is(CustomRoles.Amnesiac))
                {
                    if (tar.GetCustomRole().IsImpostor())
                    {
                        __instance.RpcSetCustomRole(CustomRoles.Refugee);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                /*    if (tar.GetCustomRole().IsCoven())
                    {
                        __instance.RpcSetCustomRole(CustomRoles.Banshee);
                        Banshee.Add(__instance.PlayerId);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    } */

                    if (tar.GetCustomRole().IsMadmate() || tar.Is(CustomRoles.Madmate))
                    {
                        __instance.RpcSetCustomRole(CustomRoles.Refugee);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.GetCustomRole().IsCrewmate() && !tar.Is(CustomRoles.Madmate))
                    {
                        if (tar.IsAmneCrew())
                        {
                            __instance.RpcSetCustomRole(tar.GetCustomRole());
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Sheriff))
                        {
                            Sheriff.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Sheriff);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Admirer))
                        {
                            Admirer.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Admirer);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Cleanser))
                        {
                            Sheriff.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Cleanser);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.CopyCat))
                        {
                            CopyCat.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.CopyCat);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Counterfeiter))
                        {
                            Counterfeiter.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Counterfeiter);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Crusader))
                        {
                            Crusader.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Crusader);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Farseer))
                        {
                            Farseer.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Farseer);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Jailer))
                        {
                            Jailer.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Jailer);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Judge))
                        {
                            Judge.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Judge);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Medic))
                        {
                            Medic.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Medic);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Mediumshiper))
                        {
                            Mediumshiper.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Mediumshiper);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Monarch))
                        {
                            Monarch.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Monarch);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else if (tar.Is(CustomRoles.Monitor))
                        {
                            Monitor.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Monitor);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.Swapper))
                        {
                            Swapper.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.Swapper);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.SabotageMaster))
                        {
                            SabotageMaster.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.SabotageMaster);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }
                        else if (tar.Is(CustomRoles.SwordsMan))
                        {
                            SwordsMan.Add(__instance.PlayerId);
                            __instance.RpcSetCustomRole(CustomRoles.SwordsMan);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        else
                        {
                            __instance.RpcSetCustomRole(CustomRoles.EngineerTOHE);
                            __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                            tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                            Main.TasklessCrewmate.Add(__instance.PlayerId);
                        }

                    }

                    if (tar.GetCustomRole().IsAmneNK())
                    {
                    //    Sheriff.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(tar.GetCustomRole());
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.GetCustomRole().IsAmneMaverick())
                    {
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 0)
                        {
                        Amnesiac.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Amnesiac);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 1)
                        {
                        NWitch.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.NWitch);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 2)
                        {
                        Pursuer.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Pursuer);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 3)
                        {
                        Totocalcio.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Totocalcio);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 4)
                        {
                        Maverick.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Maverick);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                        if (Amnesiac.IncompatibleNeutralMode.GetValue() == 5)
                        {
                        Imitator.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Imitator);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                        }
                    }

                    if (tar.Is(CustomRoles.Jackal))
                    {
                        Sidekick.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Sidekick);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.Is(CustomRoles.Juggernaut))
                    {
                        Juggernaut.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.Juggernaut);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }

                    if (tar.Is(CustomRoles.BloodKnight))
                    {
                        BloodKnight.Add(__instance.PlayerId);
                        __instance.RpcSetCustomRole(CustomRoles.BloodKnight);
                        __instance.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("YouRememberedRole")));
                        tar.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amnesiac), GetString("RememberedYourRole")));
                    }


                        return false;
                }

                if (__instance.Is(CustomRoles.Unlucky) && (target?.Object == null || !target.Object.Is(CustomRoles.Bait)))
                {
                    var Ue = IRandom.Instance;
                    if (Ue.Next(0, 100) < Options.UnluckyReportSuicideChance.GetInt())
                    {
                        __instance.RpcMurderPlayerV3(__instance);
                        Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                        return false;
                    }
                }   

                if (target.Object.Is(CustomRoles.BoobyTrap) && Options.TrapTrapsterBody.GetBool() && !__instance.Is(CustomRoles.Pestilence))
                    {
                        var killerID = target.PlayerId;
                        Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Trap;
                        __instance.SetRealKiller(Utils.GetPlayerById(killerID));

                        __instance.RpcMurderPlayerV3(__instance);
                        RPC.PlaySoundRPC(killerID, Sounds.KillSound);
                        if (Options.TrapConsecutiveTrapsterBodies.GetBool())
                        {
                            Main.BoobyTrapBody.Add(__instance.PlayerId);
                        }
                        return false;
                    } 



                // 报告了诡雷尸体
                if (Main.BoobyTrapBody.Contains(target.PlayerId) && __instance.IsAlive() && !__instance.Is(CustomRoles.Pestilence))
                {
                /*    if (!Options.TrapOnlyWorksOnTheBodyBoobyTrap.GetBool())
                    {
                        var killerID = Main.KillerOfBoobyTrapBody[target.PlayerId];
                        Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        __instance.SetRealKiller(Utils.GetPlayerById(killerID));

                        __instance.RpcMurderPlayerV3(__instance);
                        RPC.PlaySoundRPC(killerID, Sounds.KillSound);

                        if (!Main.BoobyTrapBody.Contains(__instance.PlayerId)) Main.BoobyTrapBody.Add(__instance.PlayerId);
                        if (!Main.KillerOfBoobyTrapBody.ContainsKey(__instance.PlayerId)) Main.KillerOfBoobyTrapBody.Add(__instance.PlayerId, killerID);
                        return false;
                    }
                    else */
                    {
                        var killerID2 = target.PlayerId;
                        Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Trap;
                        __instance.SetRealKiller(Utils.GetPlayerById(killerID2));

                        __instance.RpcMurderPlayerV3(__instance);
                        RPC.PlaySoundRPC(killerID2, Sounds.KillSound);
                        if (Options.TrapConsecutiveBodies.GetBool())
                        {
                            Main.BoobyTrapBody.Add(__instance.PlayerId);
                        }
                        return false;
                    } 
                }

                if (target.Object.Is(CustomRoles.Unreportable)) return false;
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
                }
            }

            AfterReportTasks(__instance, target);

        }
        catch (Exception e)
        {
            Logger.Exception(e, "ReportDeadBodyPatch");
            Logger.SendInGame("Error: " + e.ToString());
        }

        return true;
    }
    public static void AfterReportTasks(PlayerControl player, GameData.PlayerInfo target)
    {
        //=============================================
        //以下、ボタンが押されることが確定したものとする。
        //=============================================

        if (target == null) //ボタン
        {
            if (player.Is(CustomRoles.Mayor))
            {
                Main.MayorUsedButtonCount[player.PlayerId] += 1;
            }
        }
        else
        {
            var tpc = Utils.GetPlayerById(target.PlayerId);
            if (tpc != null && !tpc.IsAlive())
            {
                // 侦探报告
                if (player.Is(CustomRoles.Detective) && player.PlayerId != target.PlayerId)
                {
                    string msg;
                    msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleName());
                    if (Options.DetectiveCanknowKiller.GetBool())
                    {
                        var realKiller = tpc.GetRealKiller();
                        if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                        else msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetDisplayRoleName());
                    }
                    Main.DetectiveNotify.Add(player.PlayerId, msg);
                }
                if (player.Is(CustomRoles.Sleuth) && player.PlayerId != target.PlayerId)
                {
                    string msg;
                    msg = string.Format(GetString("SleuthNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleName());
                    if (Options.SleuthCanKnowKillerRole.GetBool())
                    {
                        var realKiller = tpc.GetRealKiller();
                        if (realKiller == null) msg += "；" + GetString("SleuthNoticeKillerNotFound");
                        else msg += "；" + string.Format(GetString("SleuthNoticeKiller"), realKiller.GetDisplayRoleName());
                    }
                    Main.SleuthNotify.Add(player.PlayerId, msg);
                }
            }

            if (Main.InfectedBodies.Contains(target.PlayerId)) Virus.OnKilledBodyReport(player);
        }

        Main.LastVotedPlayerInfo = null;
        Main.ArsonistTimer.Clear();
        Main.FarseerTimer.Clear();
        Main.GuesserGuessed.Clear();
        Main.VeteranInProtect.Clear();
        Main.GrenadierBlinding.Clear();
        Main.MadGrenadierBlinding.Clear();
        Main.Lighter.Clear();
        Main.AllKillers.Clear();
        Divinator.didVote.Clear();
        Oracle.didVote.Clear();
        Bloodhound.Clear();
        Vulture.Clear();
        Main.GodfatherTarget.Clear();

        Camouflager.OnReportDeadBody();
        Psychic.OnReportDeadBody();
        BountyHunter.OnReportDeadBody();
        SerialKiller.OnReportDeadBody();
        SoulCollector.OnReportDeadBody();
        Puppeteer.OnReportDeadBody();
        Sniper.OnReportDeadBody();
        Undertaker.OnReportDeadBody();
        Vampire.OnStartMeeting();
        Poisoner.OnStartMeeting();
        Pelican.OnReportDeadBody();
        Bandit.OnReportDeadBody();
        Agitater.OnReportDeadBody();
        Counterfeiter.OnReportDeadBody();
        QuickShooter.OnReportDeadBody();
        Eraser.OnReportDeadBody();
        Hacker.OnReportDeadBody();
        Divinator.OnReportDeadBody();
        Judge.OnReportDeadBody();
    //    Councillor.OnReportDeadBody();
        Greedier.OnReportDeadBody();
        Tracker.OnReportDeadBody();
        Addict.OnReportDeadBody();
        Oracle.OnReportDeadBody();
        Deathpact.OnReportDeadBody();
        ParityCop.OnReportDeadBody();
        Doomsayer.OnReportDeadBody();
        BallLightning.OnReportDeadBody();
        CovenLeader.OnReportDeadBody();
        NWitch.OnReportDeadBody();
        Seeker.OnReportDeadBody();
        Jailer.OnReportDeadBody();
        Romantic.OnReportDeadBody();


        Mortician.OnReportDeadBody(player, target);
        Tracefinder.OnReportDeadBody(player, target);
        Mediumshiper.OnReportDeadBody(target);
        Spiritualist.OnReportDeadBody(target);

        foreach (var pid in Main.AwareInteracted.Keys)
        {
            var Awarepc = Utils.GetPlayerById(pid);
            if (Main.AwareInteracted[pid].Count > 0 && Awarepc.IsAlive())
            {
                string rolelist = "Someone";
                _ = new LateTask(() =>
                {
                    if (Options.AwareknowRole.GetBool())
                        rolelist = string.Join(", ", Main.AwareInteracted[pid]);
                    Utils.SendMessage(string.Format(GetString("AwareInteracted"), rolelist), pid, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Aware), GetString("AwareTitle")));
                    Main.AwareInteracted[pid] = new();
                }, 0.5f, "AwareCheckMsg");
            }
        }

        foreach (var x in Main.RevolutionistStart)
        {
            var tar = Utils.GetPlayerById(x.Key);
            if (tar == null) continue;
            tar.Data.IsDead = true;
            Main.PlayerStates[tar.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
            tar.RpcExileV2();
            Main.PlayerStates[tar.PlayerId].SetDead();
            Logger.Info($"{tar.GetRealName()} 因会议革命失败", "Revolutionist");
        }
        Main.RevolutionistTimer.Clear();
        Main.RevolutionistStart.Clear();
        Main.RevolutionistLastTime.Clear();

        Main.AllPlayerControls
            .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
            .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));

        MeetingTimeManager.OnReportDeadBody();

        Utils.NotifyRoles(isForMeeting: true, NoCache: true, CamouflageIsForMeeting: true);

        Utils.SyncAllSettings();
    }
    public static async void ChangeLocalNameAndRevert(string name, int time)
    {
        //async Taskじゃ警告出るから仕方ないよね。
        var revertName = PlayerControl.LocalPlayer.name;
        PlayerControl.LocalPlayer.RpcSetNameEx(name);
        await Task.Delay(time);
        PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
class FixedUpdatePatch
{
    private static long LastFixedUpdate = new();
    private static StringBuilder Mark = new(20);
    private static StringBuilder Suffix = new(120);
    private static int LevelKickBufferTime = 10;
    private static Dictionary<byte, int> BufferTime = new();
    public static void Postfix(PlayerControl __instance)
    {
        var player = __instance;

        if (!GameStates.IsModHost) return;

        bool lowLoad = false;
        if (Options.LowLoadMode.GetBool())
        {
            BufferTime.TryAdd(player.PlayerId, 10);
            BufferTime[player.PlayerId]--;
            if (BufferTime[player.PlayerId] > 0) lowLoad = true;
            else BufferTime[player.PlayerId] = 10;
        }

        Sniper.OnFixedUpdate(player);
        Zoom.OnFixedUpdate();
        if (!lowLoad)
        {
            NameNotifyManager.OnFixedUpdate(player);
            TargetArrow.OnFixedUpdate(player);
            LocateArrow.OnFixedUpdate(player);
        }


        if (AmongUsClient.Instance.AmHost)
        {
            if (GameStates.IsLobby)
            {
                if (((ModUpdater.hasUpdate && ModUpdater.forceUpdate) || ModUpdater.isBroken || !Main.AllowPublicRoom || !VersionChecker.IsSupported) && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);

                //踢出低等级的人
                if (!lowLoad && !player.AmOwner && Options.KickLowLevelPlayer.GetInt() != 0 && (
                    (player.Data.PlayerLevel != 0 && player.Data.PlayerLevel < Options.KickLowLevelPlayer.GetInt()) ||
                    player.Data.FriendCode == ""
                    ))
                {
                    LevelKickBufferTime--;
                    if (LevelKickBufferTime <= 0)
                    {
                        LevelKickBufferTime = 20;
                        AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                        string msg = string.Format(GetString("KickBecauseLowLevel"), player.GetRealName().RemoveHtmlTags());
                        Logger.SendInGame(msg);
                        Logger.Info(msg, "LowLevel Kick");
                    }
                }
            }

            if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
            {
                var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                __instance.ReportDeadBody(info);
            }

            DoubleTrigger.OnFixedUpdate(player);
            Vampire.OnFixedUpdate(player);
            Poisoner.OnFixedUpdate(player);
            BountyHunter.FixedUpdate(player);
            Seeker.FixedUpdate(player);
            SerialKiller.FixedUpdate(player);
            
            if (PlagueBearer.IsEnable && GameStates.IsInTask)
                if (player.Is(CustomRoles.PlagueBearer) && PlagueBearer.IsPlaguedAll(player))
                {
                    player.RpcSetCustomRole(CustomRoles.Pestilence);
                    player.Notify(GetString("PlagueBearerToPestilence"));
                    player.RpcGuardAndKill(player);
                    if (!PlagueBearer.PestilenceList.Contains(player.PlayerId))
                        PlagueBearer.PestilenceList.Add(player.PlayerId);
                    PlagueBearer.SetKillCooldownPestilence(player.PlayerId);
                    PlagueBearer.playerIdList.Remove(player.PlayerId);
                }

            if (Agitater.IsEnable && GameStates.IsInTask && Agitater.AgitaterHasBombed && Agitater.CurrentBombedPlayer == player.PlayerId)
            {
                if (!player.IsAlive())
                {
                    Agitater.ResetBomb();
                }
                else
                {
                    Vector2 puppeteerPos = player.transform.position;
                    Dictionary<byte, float> targetDistance = new();
                    float dis;
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        if (!target.IsAlive()) continue;
                        if (target.PlayerId != player.PlayerId && target.PlayerId != Agitater.LastBombedPlayer && !target.Data.IsDead)
                        {
                            dis = Vector2.Distance(puppeteerPos, target.transform.position);
                            targetDistance.Add(target.PlayerId, dis);
                        }
                    }
                    if (targetDistance.Count != 0)
                    {
                        var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                        PlayerControl target = Utils.GetPlayerById(min.Key);
                        var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)];
                        if (min.Value <= KillRange && player.CanMove && target.CanMove)
                            Agitater.PassBomb(player, target);
                    }
                }
            }


            #region Warlock Timer
            if (GameStates.IsInTask && Main.WarlockTimer.ContainsKey(player.PlayerId))//処理を1秒遅らせる
            {
                if (player.IsAlive())
                {
                    if (Main.WarlockTimer[player.PlayerId] >= 1f)
                    {
                        player.RpcResetAbilityCooldown();
                        Main.isCursed = false;//変身クールを１秒に変更
                        player.SyncSettings();
                        Main.WarlockTimer.Remove(player.PlayerId);
                    }
                    else Main.WarlockTimer[player.PlayerId] = Main.WarlockTimer[player.PlayerId] + Time.fixedDeltaTime;//時間をカウント
                }
                else
                {
                    Main.WarlockTimer.Remove(player.PlayerId);
                }
            }
            #endregion

            #region Arsonist Timer
            if (GameStates.IsInTask && Main.ArsonistTimer.ContainsKey(player.PlayerId))//アーソニストが誰かを塗っているとき
            {
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId))
                {
                    Main.ArsonistTimer.Remove(player.PlayerId);
                    Utils.NotifyRoles(SpecifySeer: __instance);
                    RPC.ResetCurrentDousingTarget(player.PlayerId);
                }
                else
                {
                    var ar_target = Main.ArsonistTimer[player.PlayerId].Item1;//塗られる人
                    var ar_time = Main.ArsonistTimer[player.PlayerId].Item2;//塗った時間
                    if (!ar_target.IsAlive())
                    {
                        Main.ArsonistTimer.Remove(player.PlayerId);
                    }
                    else if (ar_time >= Options.ArsonistDouseTime.GetFloat())//時間以上一緒にいて塗れた時
                    {
                        player.SetKillCooldown();
                        Main.ArsonistTimer.Remove(player.PlayerId);//塗が完了したのでDictionaryから削除
                        Main.isDoused[(player.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                        player.RpcSetDousedPlayer(ar_target, true);
                        Utils.NotifyRoles(SpecifySeer: player);//名前変更
                        RPC.ResetCurrentDousingTarget(player.PlayerId);
                    }
                    else
                    {

                        float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                        float dis = Vector2.Distance(player.transform.position, ar_target.transform.position);//距離を出す
                        if (dis <= range)//一定の距離にターゲットがいるならば時間をカウント
                        {
                            Main.ArsonistTimer[player.PlayerId] = (ar_target, ar_time + Time.fixedDeltaTime);
                        }
                        else//それ以外は削除
                        {
                            Main.ArsonistTimer.Remove(player.PlayerId);
                            Utils.NotifyRoles(SpecifySeer: player);
                            RPC.ResetCurrentDousingTarget(player.PlayerId);

                            Logger.Info($"Canceled: {player.GetNameWithRole()}", "Arsonist");
                        }
                    }
                }
            }
            #endregion

            #region Revolutionist Timer
            if (GameStates.IsInTask && Main.RevolutionistTimer.ContainsKey(player.PlayerId))//当革命家拉拢一个玩家时
            {
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId))
                {
                    Main.RevolutionistTimer.Remove(player.PlayerId);
                    Utils.NotifyRoles(SpecifySeer: player);
                    RPC.ResetCurrentDrawTarget(player.PlayerId);
                }
                else
                {
                    var rv_target = Main.RevolutionistTimer[player.PlayerId].Item1;//拉拢的人
                    var rv_time = Main.RevolutionistTimer[player.PlayerId].Item2;//拉拢时间
                    if (!rv_target.IsAlive())
                    {
                        Main.RevolutionistTimer.Remove(player.PlayerId);
                    }
                    else if (rv_time >= Options.RevolutionistDrawTime.GetFloat())//在一起时间超过多久
                    {
                        player.SetKillCooldown();
                        Main.RevolutionistTimer.Remove(player.PlayerId);//拉拢完成从字典中删除
                        Main.isDraw[(player.PlayerId, rv_target.PlayerId)] = true;//完成拉拢
                        player.RpcSetDrawPlayer(rv_target, true);
                        Utils.NotifyRoles(SpecifySeer: player);
                        RPC.ResetCurrentDrawTarget(player.PlayerId);
                        if (IRandom.Instance.Next(1, 100) <= Options.RevolutionistKillProbability.GetInt())
                        {
                            rv_target.SetRealKiller(player);
                            Main.PlayerStates[rv_target.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                            player.RpcMurderPlayerV3(rv_target);
                            Main.PlayerStates[rv_target.PlayerId].SetDead();
                            Logger.Info($"Revolutionist: {player.GetNameWithRole()} killed {rv_target.GetNameWithRole()}", "Revolutionist");
                        }
                    }
                    else
                    {
                        float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                        float dis = Vector2.Distance(player.transform.position, rv_target.transform.position);//超出距离
                        if (dis <= range)//在一定距离内则计算时间
                        {
                            Main.RevolutionistTimer[player.PlayerId] = (rv_target, rv_time + Time.fixedDeltaTime);
                        }
                        else//否则删除
                        {
                            Main.RevolutionistTimer.Remove(player.PlayerId);
                            Utils.NotifyRoles(SpecifySeer: __instance);
                            RPC.ResetCurrentDrawTarget(player.PlayerId);

                            Logger.Info($"Canceled: {__instance.GetNameWithRole()}", "Revolutionist");
                        }
                    }
                }
            }
            if (GameStates.IsInTask && player.IsDrawDone() && player.IsAlive())
            {
                if (Main.RevolutionistStart.ContainsKey(player.PlayerId)) //如果存在字典
                {
                    if (Main.RevolutionistLastTime.ContainsKey(player.PlayerId))
                    {
                        long nowtime = Utils.GetTimeStamp();
                        if (Main.RevolutionistLastTime[player.PlayerId] != nowtime) Main.RevolutionistLastTime[player.PlayerId] = nowtime;
                        int time = (int)(Main.RevolutionistLastTime[player.PlayerId] - Main.RevolutionistStart[player.PlayerId]);
                        int countdown = Options.RevolutionistVentCountDown.GetInt() - time;
                        Main.RevolutionistCountdown.Clear();
                        if (countdown <= 0)//倒计时结束
                        {
                            Utils.GetDrawPlayerCount(player.PlayerId, out var y);
                            foreach (var pc in y.Where(x => x != null && x.IsAlive()))
                            {
                                pc.Data.IsDead = true;
                                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                                pc.RpcMurderPlayerV3(pc);
                                Main.PlayerStates[pc.PlayerId].SetDead();
                                Utils.NotifyRoles(SpecifySeer: pc);
                            }
                            player.Data.IsDead = true;
                            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                            player.RpcMurderPlayerV3(player);
                            Main.PlayerStates[player.PlayerId].SetDead();
                        }
                        else
                        {
                            Main.RevolutionistCountdown.Add(player.PlayerId, countdown);
                        }
                    }
                    else
                    {
                        Main.RevolutionistLastTime.TryAdd(player.PlayerId, Main.RevolutionistStart[player.PlayerId]);
                    }
                }
                else //如果不存在字典
                {
                    Main.RevolutionistStart.TryAdd(player.PlayerId, Utils.GetTimeStamp());
                }
            }
            #endregion

            Farseer.OnPostFix(player);
            Addict.FixedUpdate(player);
            Deathpact.OnFixedUpdate(player);

            if (!lowLoad)
            {
                //检查老兵技能是否失效
                if (GameStates.IsInTask && player.Is(CustomRoles.Veteran))
                {
                    if (Main.VeteranInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.VeteranSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.VeteranInProtect.Remove(player.PlayerId);
                        if (!Options.DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
                        else player.RpcResetAbilityCooldown();
                        player.Notify(string.Format(GetString("VeteranOffGuard"), Main.VeteranNumOfUsed[player.PlayerId]));
                    }
                }

                #region 检查迷你船员是否要增加年龄
                if (GameStates.IsInTask && player.Is(CustomRoles.NiceMini))
                {
                    if (Mini.Age < 18 && player.IsAlive())
                    {
                        if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                        LastFixedUpdate = Utils.GetTimeStamp();
                        Mini.GrowUpTime ++;
                        if (Mini.GrowUpTime >= Mini.GrowUpDuration.GetInt()/18)
                        {
                            Mini.Age += 1;                            
                            Mini.GrowUpTime = 0;                         
                            player.RpcGuardAndKill();
                            Logger.Info($"年龄增加1", "Child");
                            if (Mini.UpDateAge.GetBool())
                            {
                                Mini.SendRPC();
                                Utils.NotifyRoles();
                            }
                        }
                    }
                }
                if (GameStates.IsInTask && player.Is(CustomRoles.EvilMini))
                {
                    if (Mini.Age < 18)
                    {
                        if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                        LastFixedUpdate = Utils.GetTimeStamp();
                        Mini.GrowUpTime++;
                        if (Main.EvilMiniKillcooldown[player.PlayerId] >= 1f)
                        {
                            Main.EvilMiniKillcooldown[player.PlayerId]--;

                        }
                        if (Mini.GrowUpTime >= Mini.GrowUpDuration.GetInt() / 18)
                        {
                            Main.EvilMiniKillcooldownf = Main.EvilMiniKillcooldown[player.PlayerId];
                            Logger.Info($"记录击杀冷却{Main.EvilMiniKillcooldownf}", "Child");
                            Main.AllPlayerKillCooldown[player.PlayerId] = Main.EvilMiniKillcooldownf;
                            Main.EvilMiniKillcooldown[player.PlayerId] = Main.EvilMiniKillcooldownf;
                            player.MarkDirtySettings();
                            Mini.Age += 1;
                            Mini.GrowUpTime = 0;
                            Logger.Info($"年龄增加1", "Child");
                            player.SetKillCooldown();

                            if (Mini.UpDateAge.GetBool())
                            {
                                Mini.SendRPC();
                                Utils.NotifyRoles();
                            }
                            Logger.Info($"重置击杀冷却{Main.EvilMiniKillcooldownf -1f}", "Child");
                        }
                    }
                }
                #endregion

                //检查掷雷兵技能是否生效
                if (GameStates.IsInTask && player.Is(CustomRoles.Grenadier))
                {
                    if (Main.GrenadierBlinding.TryGetValue(player.PlayerId, out var gtime) && gtime + Options.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.GrenadierBlinding.Remove(player.PlayerId);
                        if (!Options.DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
                        else player.RpcResetAbilityCooldown();
                        player.Notify(GetString("GrenadierSkillStop"));
                        Utils.MarkEveryoneDirtySettings();
                    }
                    if (Main.MadGrenadierBlinding.TryGetValue(player.PlayerId, out var mgtime) && mgtime + Options.GrenadierSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.MadGrenadierBlinding.Remove(player.PlayerId);
                        if (!Options.DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
                        else player.RpcResetAbilityCooldown();
                        player.Notify(GetString("GrenadierSkillStop"));
                        Utils.MarkEveryoneDirtySettings();
                    }
                }

                if (GameStates.IsInTask && player.Is(CustomRoles.Lighter))
                {
                    if (Main.Lighter.TryGetValue(player.PlayerId, out var ltime) && ltime + Options.LighterSkillDuration.GetInt() < Utils.GetTimeStamp())
                    {
                        Main.Lighter.Remove(player.PlayerId);
                        if (!Options.DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
                        else player.RpcResetAbilityCooldown();
                        player.Notify(GetString("LighterSkillStop"));
                        player.MarkDirtySettings();
                    }
                }

                //检查马里奥是否完成
                if (GameStates.IsInTask && player.Is(CustomRoles.Mario) && Main.MarioVentCount[player.PlayerId] > Options.MarioVentNumWin.GetInt())
                {
                    Main.MarioVentCount[player.PlayerId] = Options.MarioVentNumWin.GetInt();
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Mario); //马里奥这个多动症赢了
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }
                foreach (var mini in Main.AllPlayerControls)
                {
                    if (GameStates.IsInTask && mini.Is(CustomRoles.NiceMini) && Mini.Age < 18 && !mini.IsAlive())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                    //    CustomWinnerHolder.WinnerIds.Add(mini.PlayerId); // Nice Mini does not win (Crewmates should not solo win unless Egoist)
                    }
                }

                if (GameStates.IsInTask && player.Is(CustomRoles.Vulture) && Vulture.BodyReportCount[player.PlayerId] >= Vulture.NumberOfReportsToWin.GetInt())
                {
                    Vulture.BodyReportCount[player.PlayerId] = Vulture.NumberOfReportsToWin.GetInt();
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vulture);
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }

                if (Main.AllKillers.TryGetValue(player.PlayerId, out var ktime) && ktime + Options.WitnessTime.GetInt() < Utils.GetTimeStamp()) 
                    Main.AllKillers.Remove(player.PlayerId);

                Pelican.OnFixedUpdate();
                BallLightning.OnFixedUpdate();
                BloodKnight.OnFixedUpdate(player);
                Puppeteer.OnFixedUpdate(player);
                CovenLeader.OnFixedUpdate(player);
                Shroud.OnFixedUpdate(player);
                NWitch.OnFixedUpdate(player);
                Banshee.OnFixedUpdate(player);
                Wildling.OnFixedUpdate(player);
                Spiritcaller.OnFixedUpdate(player);
                Pitfall.OnFixedUpdate(player);
                Swooper.OnFixedUpdate(player);
                Wraith.OnFixedUpdate(player);
                Shade.OnFixedUpdate(player);
                Chameleon.OnFixedUpdate(player);

                if (GameStates.IsInTask)
                {
                    if (Options.LadderDeath.GetBool() && player.IsAlive()) FallFromLadder.FixedUpdate(player);

                    if (GameStates.IsInGame && CustomRoles.Lovers.IsEnable()) LoversSuicide();

                    if (player == PlayerControl.LocalPlayer)
                        DisableDevice.FixedUpdate();

                    if (player == PlayerControl.LocalPlayer)
                        AntiAdminer.FixedUpdate();

                    if (player == PlayerControl.LocalPlayer)
                        Monitor.FixedUpdate();
                }

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock) || pc.Is(CustomRoles.Assassin))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                        if (pc.Is(CustomRoles.Poisoner))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                    }

                if (!Main.DoBlockNameChange && AmongUsClient.Instance.AmHost)
                    Utils.ApplySuffix(__instance);
            }
        }

        //LocalPlayer専用
        if (__instance.AmOwner)
        {
            //キルターゲットの上書き処理
            if (GameStates.IsInTask && !__instance.Is(CustomRoleTypes.Impostor) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
            {
                var players = __instance.GetPlayersInAbilityRangeSorted(false);
                PlayerControl closest = players.Count <= 0 ? null : players[0];
                HudManager.Instance.KillButton.SetTarget(closest);
            }
        }

        var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
        var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();

        if (RoleText != null && __instance != null && !lowLoad)
        {
            if (GameStates.IsLobby)
            {
                if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                {
                    if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                        __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                    else if (Main.version.CompareTo(ver.version) == 0)
                        __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                    else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                }
                else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
            }
            if (GameStates.IsInGame)
            {
                var RoleTextData = Utils.GetRoleText(PlayerControl.LocalPlayer.PlayerId, __instance.PlayerId);
                RoleText.text = RoleTextData.Item1;
                RoleText.color = RoleTextData.Item2;
                if (__instance.AmOwner) RoleText.enabled = true;
                else if (ExtendedPlayerControl.KnowRoleTarget(PlayerControl.LocalPlayer, __instance)) RoleText.enabled = true;
                else RoleText.enabled = false;
                if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.IsRevealedPlayer(__instance) && __instance.Is(CustomRoles.Trickster))
                {
                    RoleText.text = Farseer.RandomRole[PlayerControl.LocalPlayer.PlayerId];
                    RoleText.text += Farseer.GetTaskState();
                }

                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                    if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }
                if (Main.VisibleTasksCount) //他プレイヤーでVisibleTasksCountは有効なら
                    RoleText.text += Utils.GetProgressText(__instance); //ロールの横にタスクなど進行状況表示


                var seer = PlayerControl.LocalPlayer;
                var target = __instance;

                string RealName = target.GetRealName();

                Mark.Clear();
                Suffix.Clear();


                if (target.AmOwner && GameStates.IsInTask)
                {
                    switch (target.GetCustomRole())
                    {
                        case CustomRoles.Arsonist:
                            if (target.IsDouseDone())
                                RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                            break;

                        case CustomRoles.Revolutionist:
                            if (target.IsDrawDone())
                                RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), string.Format(GetString("EnterVentWinCountDown"), Main.RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10));
                            break;
                    }

                    if (Pelican.IsEaten(seer.PlayerId))
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"));

                    if (Deathpact.IsInActiveDeathpact(seer))
                        RealName = Deathpact.GetDeathpactString(seer);

                    if (NameNotifyManager.GetNameNotify(target, out var name))
                        RealName = name;
                }

                RealName = RealName.ApplyNameColorData(seer, target, false);
                var seerRole = seer.GetCustomRole();

                if (target.GetPlayerTaskState().IsTaskFinished)
                {
                    seerRole = seer.GetCustomRole();

                    if (seerRole.IsImpostor())
                    {
                        if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate))
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★"));
                    }

                    if (seerRole.IsCrewmate() && !seer.Is(CustomRoles.Madmate))
                    {
                        if (target.Is(CustomRoles.Marshall))
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Marshall), "★"));
                    }
                }

                Mark.Append(Snitch.GetWarningMark(seer, target));
                Mark.Append(Marshall.GetWarningMark(seer, target));
                Mark.Append(Executioner.TargetMark(seer, target));
                Mark.Append(Gamer.TargetMark(seer, target));
                Mark.Append(Totocalcio.TargetMark(seer, target));
                Mark.Append(Romantic.TargetMark(seer, target));
                Mark.Append(Lawyer.LawyerMark(seer, target));
                Mark.Append(Snitch.GetWarningArrow(seer, target));

                if (seer.Is(CustomRoles.EvilTracker))
                    Mark.Append(EvilTracker.GetTargetMark(seer, target));

                if (seer.Is(CustomRoles.Tracker))
                    Mark.Append(Tracker.GetTargetMark(seer, target));

                if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★"));

                if (target.Is(CustomRoles.Cyber) && Options.CyberKnown.GetBool())
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), "★"));

                if (BallLightning.IsGhost(target))
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BallLightning), "■"));


                seerRole = seer.GetCustomRole();
                switch (seerRole)
                {
                    case CustomRoles.Lookout:
                        if (seer.IsAlive() && target.IsAlive())
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(seerRole), " " + target.PlayerId.ToString()) + " ");
                        break;

                    case CustomRoles.PlagueBearer:
                        if (PlagueBearer.isPlagued(seer.PlayerId, target.PlayerId))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>●</color>");
                        break;

                    case CustomRoles.Arsonist:
                        if (seer.IsDousedPlayer(target))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>▲</color>");

                        else if (Main.currentDousingTarget != byte.MaxValue && Main.currentDousingTarget == target.PlayerId)
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>△</color>");
                        break;

                    case CustomRoles.Revolutionist:
                        if (seer.IsDrawPlayer(target))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>●</color>");

                        else if (Main.currentDrawTarget != byte.MaxValue && Main.currentDrawTarget == target.PlayerId)
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>○</color>");
                        break;

                    case CustomRoles.Farseer:
                        if (Main.currentDrawTarget != byte.MaxValue && Main.currentDrawTarget == target.PlayerId)
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>○</color>");
                        break;

                    case CustomRoles.Medic:
                        if ((Medic.WhoCanSeeProtect.GetInt() is 0 or 1) && (Medic.InProtect(target.PlayerId) || Medic.TempMarkProtected == target.PlayerId))
                            Mark.Append($"<color={Utils.GetRoleColorCode(seerRole)}>✚</color>");
                        break;

                    case CustomRoles.Puppeteer:
                        Mark.Append(Puppeteer.TargetMark(seer, target));
                        break;

                    case CustomRoles.CovenLeader:
                        Mark.Append(CovenLeader.TargetMark(seer, target));
                        break;

                    case CustomRoles.NWitch:
                        Mark.Append(NWitch.TargetMark(seer, target));
                        break;

                    case CustomRoles.Shroud:
                        Mark.Append(Shroud.TargetMark(seer, target));
                        break;
                }
                if (target.Is(CustomRoles.NiceMini) && Mini.EveryoneCanKnowMini.GetBool())
                    Mark.Append(Utils.ColorString(Color.white, Mini.Age != 18 && Mini.UpDateAge.GetBool() ? $"({Mini.Age})" : ""));

                if (target.Is(CustomRoles.EvilMini) && Mini.EveryoneCanKnowMini.GetBool())
                    Mark.Append(Utils.ColorString(Color.white, Mini.Age != 18 && Mini.UpDateAge.GetBool() ? $"({Mini.Age})" : ""));
                    
                if ((Medic.WhoCanSeeProtect.GetInt() is 0 or 2) && seer.PlayerId == target.PlayerId && (Medic.InProtect(seer.PlayerId) || Medic.TempMarkProtected == seer.PlayerId))
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}>✚</color>");

                if (seer.Data.IsDead && Medic.InProtect(target.PlayerId) && !seer.Is(CustomRoles.Medic))
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Medic)}>✚</color>");

                if (Sniper.IsEnable && target.AmOwner)
                    Mark.Append(Sniper.GetShotNotify(target.PlayerId));

                if (target.Is(CustomRoles.Lovers) && seer.Is(CustomRoles.Lovers))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target.Is(CustomRoles.Lovers) && seer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target.Is(CustomRoles.Ntr) || seer.Is(CustomRoles.Ntr))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }
                else if (target == seer && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♥</color>");
                }


                Suffix.Append(Snitch.GetSnitchArrow(seer, target));
                Suffix.Append(BountyHunter.GetTargetArrow(seer, target));
                Suffix.Append(Mortician.GetTargetArrow(seer, target));
                Suffix.Append(EvilTracker.GetTargetArrow(seer, target));
                Suffix.Append(Bloodhound.GetTargetArrow(seer, target));
                Suffix.Append(Tracker.GetTrackerArrow(seer, target));
                Suffix.Append(Deathpact.GetDeathpactPlayerArrow(seer, target));
                Suffix.Append(Deathpact.GetDeathpactMark(seer, target));
                Suffix.Append(Spiritualist.GetSpiritualistArrow(seer, target));
                Suffix.Append(Tracefinder.GetTargetArrow(seer, target));

                if (Vulture.ArrowsPointingToDeadBody.GetBool())
                    Suffix.Append(Vulture.GetTargetArrow(seer, target));

                if (GameStates.IsInTask)
                {
                    if (seer.Is(CustomRoles.AntiAdminer))
                    {
                        AntiAdminer.FixedUpdate();
                        if (target.AmOwner)
                        {
                            if (AntiAdminer.IsAdminWatch) Suffix.Append("<color=#ff1919>⚠</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.AntiAdminer), GetString("AdminWarning")));
                            if (AntiAdminer.IsVitalWatch) Suffix.Append("<color=#ff1919>⚠</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.AntiAdminer), GetString("VitalsWarning")));
                            if (AntiAdminer.IsDoorLogWatch) Suffix.Append("<color=#ff1919>⚠</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.AntiAdminer), GetString("DoorlogWarning")));
                            if (AntiAdminer.IsCameraWatch) Suffix.Append("<color=#ff1919>⚠</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.AntiAdminer), GetString("CameraWarning")));
                        }
                    }
                    if (seer.Is(CustomRoles.Monitor))
                    {
                        Monitor.FixedUpdate();
                        if (target.AmOwner)
                        {
                            if (Monitor.IsAdminWatch) Suffix.Append("<color=#7223DA>★</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monitor), GetString("AdminWarning")));
                            if (Monitor.IsVitalWatch) Suffix.Append("<color=#7223DA>★</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monitor), GetString("VitalsWarning")));
                            if (Monitor.IsDoorLogWatch) Suffix.Append("<color=#7223DA>★</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monitor), GetString("DoorlogWarning")));
                            if (Monitor.IsCameraWatch) Suffix.Append("<color=#7223DA>★</color>" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monitor), GetString("CameraWarning")));
                        }
                    }
                    if (player.Is(CustomRoles.TimeMaster))
                    {
                        if (Main.TimeMasterInProtect.TryGetValue(player.PlayerId, out var vtime) && vtime + Options.TimeMasterSkillDuration.GetInt() < Utils.GetTimeStamp())
                        {
                            Main.TimeMasterInProtect.Remove(player.PlayerId);
                            if (!Options.DisableShieldAnimations.GetBool()) player.RpcGuardAndKill();
                            else player.RpcResetAbilityCooldown();
                            player.Notify(GetString("TimeMasterSkillStop"));
                        }
                    }
                }

                /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                    Mark = isBlocked ? "(true)" : "(false)";}*/

                // Devourer
                bool targetDevoured = Devourer.HideNameOfConsumedPlayer.GetBool() && Devourer.PlayerSkinsCosumed.Any(a => a.Value.Contains(target.PlayerId));
                if (targetDevoured)
                    RealName = GetString("DevouredName");

                // Camouflage
                if ((Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() &&
                    !(Options.DisableOnSomeMaps.GetBool() &&
                        ((Options.DisableOnSkeld.GetBool() && Options.IsActiveSkeld) ||
                         (Options.DisableOnMira.GetBool() && Options.IsActiveMiraHQ) ||
                         (Options.DisableOnPolus.GetBool() && Options.IsActivePolus) ||
                         (Options.DisableOnAirship.GetBool() && Options.IsActiveAirship)
                        )))
                        || Camouflager.IsActive)
                    RealName = $"<size=0%>{RealName}</size> ";


                string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";

                target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                if (Suffix.ToString() != "")
                {
                    RoleText.transform.SetLocalY(0.35f);
                    target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();
                }
                else
                {
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
            else
            {
                RoleText.transform.SetLocalY(0.2f);
            }
        }
    }
    //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (Options.LoverSuicide.GetBool() && Main.isLoversDead == false)
        {
            foreach (var loversPlayer in Main.LoversPlayers)
            {
                //生きていて死ぬ予定でなければスキップ
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                Main.isLoversDead = true;
                foreach (var partnerPlayer in Main.LoversPlayers)
                {
                    //本人ならスキップ
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    //残った恋人を全て殺す(2人以上可)
                    //生きていて死ぬ予定もない場合は心中
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        if (partnerPlayer.Is(CustomRoles.Lovers))
                        {
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (isExiled)
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            else
                                partnerPlayer.RpcMurderPlayerV3(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
        roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        roleText.fontSize -= 1.2f;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
class SetColorPatch
{
    public static bool IsAntiGlitchDisabled = false;
    public static bool Prefix(PlayerControl __instance, int bodyColor)
    {
        //色変更バグ対策
        if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
        return true;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {

        Witch.OnEnterVent(pc);
        HexMaster.OnEnterVent(pc);
        Occultist.OnEnterVent(pc);

        if (pc.Is(CustomRoles.Mayor) && Options.MayorHasPortableButton.GetBool())
        {
            if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt())
            {
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc?.ReportDeadBody(null);
            }
        }
      /*  if (pc.Is(CustomRoles.Wraith)) // THIS WAS FOR WEREWOLF TESTING PURPOSES, PLEASE IGNORE
        {
            pc?.MyPhysics?.RpcBootFromVent(__instance.Id);            
        } */

        if (pc.Is(CustomRoles.Paranoia))
        {
            if (Main.ParaUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.ParanoiaNumOfUseButton.GetInt())
            {
                Main.ParaUsedButtonCount[pc.PlayerId] += 1;
                if (AmongUsClient.Instance.AmHost)
                {
                    _ = new LateTask(() =>
                    {
                        Utils.SendMessage(GetString("SkillUsedLeft") + (Options.ParanoiaNumOfUseButton.GetInt() - Main.ParaUsedButtonCount[pc.PlayerId]).ToString(), pc.PlayerId);
                    }, 4.0f, "Skill Remain Message");
                }
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc?.NoCheckStartMeeting(pc?.Data);
            }
        }

        if (pc.Is(CustomRoles.Mario))
        {
            Main.MarioVentCount.TryAdd(pc.PlayerId, 0);
            Main.MarioVentCount[pc.PlayerId]++;
            Utils.NotifyRoles(SpecifySeer: pc);
            if (AmongUsClient.Instance.AmHost && Main.MarioVentCount[pc.PlayerId] >= Options.MarioVentNumWin.GetInt())
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Mario); //马里奥这个多动症赢了
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            }
        }

        if (!AmongUsClient.Instance.AmHost) return;

        Main.LastEnteredVent.Remove(pc.PlayerId);
        Main.LastEnteredVent.Add(pc.PlayerId, __instance);
        Main.LastEnteredVentLocation.Remove(pc.PlayerId);
        Main.LastEnteredVentLocation.Add(pc.PlayerId, pc.GetTruePosition());

        Swooper.OnEnterVent(pc, __instance);
        Wraith.OnEnterVent(pc, __instance);
        Shade.OnEnterVent(pc, __instance);
        Addict.OnEnterVent(pc, __instance);
        Chameleon.OnEnterVent(pc, __instance);
        Lurker.OnEnterVent(pc);

        if (pc.Is(CustomRoles.Veteran) && !Main.VeteranInProtect.ContainsKey(pc.PlayerId))
        {
            Main.VeteranInProtect.Remove(pc.PlayerId);
            Main.VeteranInProtect.Add(pc.PlayerId, Utils.GetTimeStamp(DateTime.Now));
            Main.VeteranNumOfUsed[pc.PlayerId] -= 1;
            if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
            pc.RPCPlayCustomSound("Gunload");
            pc.Notify(GetString("VeteranOnGuard"), Options.VeteranSkillDuration.GetFloat());
        }
        if (pc.Is(CustomRoles.Unlucky))
        {
            var Ue = IRandom.Instance;
            if (Ue.Next(0, 100) < Options.UnluckyVentSuicideChance.GetInt())
            {
                pc.RpcMurderPlayerV3(pc);
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            }
        }
        if (pc.Is(CustomRoles.Grenadier))
        {
            if (Main.GrenadierNumOfUsed[pc.PlayerId] >= 1)
            {
                if (pc.Is(CustomRoles.Madmate))
                {
                    Main.MadGrenadierBlinding.Remove(pc.PlayerId);
                    Main.MadGrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                else
                {
                    Main.GrenadierBlinding.Remove(pc.PlayerId);
                    Main.GrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.GetCustomRole().IsImpostor() || (x.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("FlashBang");
                pc.Notify(GetString("GrenadierSkillInUse"), Options.GrenadierSkillDuration.GetFloat());
                Main.GrenadierNumOfUsed[pc.PlayerId] -= 1;
                Utils.MarkEveryoneDirtySettings();
            }
        }
        if (pc.Is(CustomRoles.DovesOfNeace))
        {
            if (Main.DovesOfNeaceNumOfUsed[pc.PlayerId] < 1)
            {
                pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                pc.Notify(GetString("DovesOfNeaceMaxUsage"));
            }
            else
            {
                Main.DovesOfNeaceNumOfUsed[pc.PlayerId] -= 1;
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                Main.AllAlivePlayerControls.Where(x => 
                pc.Is(CustomRoles.Madmate) ?
                (x.CanUseKillButton() && x.GetCustomRole().IsCrewmate()) :
                (x.CanUseKillButton())
                ).Do(x =>
                {
                     x.RPCPlayCustomSound("Dove");
                     x.ResetKillCooldown();
                     x.SetKillCooldown();
                     if (x.Is(CustomRoles.SerialKiller))
                        { SerialKiller.OnReportDeadBody(); }
                    x.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DovesOfNeace), GetString("DovesOfNeaceSkillNotify")));
                });
                pc.RPCPlayCustomSound("Dove");
                pc.Notify(string.Format(GetString("DovesOfNeaceOnGuard"), Main.DovesOfNeaceNumOfUsed[pc.PlayerId]));
            }
        }
        if (pc.Is(CustomRoles.Lighter))
        {
            if (Main.LighterNumOfUsed[pc.PlayerId] >= 1)
            {
                Main.Lighter.Remove(pc.PlayerId);
                Main.Lighter.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!Options.DisableShieldAnimations.GetBool()) pc.RpcGuardAndKill(pc);
                pc.Notify(GetString("LighterSkillInUse"), Options.LighterSkillDuration.GetFloat());
                Main.LighterNumOfUsed[pc.PlayerId] -= 1;
                pc.MarkDirtySettings();
            }
            else
            {
                pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
            }
        }
        if (pc.Is(CustomRoles.TimeMaster))
        {
            if (Main.TimeMasterNumOfUsed[pc.PlayerId] >= 1)
            {
                Main.TimeMasterNumOfUsed[pc.PlayerId] -= 1;
                Main.TimeMasterInProtect.Remove(pc.PlayerId);
                Main.TimeMasterInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!pc.IsModClient())
                    pc.RpcGuardAndKill(pc);
                pc.Notify(GetString("TimeMasterOnGuard"), Options.TimeMasterSkillDuration.GetFloat());
                foreach (var player in Main.AllPlayerControls)
                {
                    if (Main.TimeMasterBackTrack.ContainsKey(player.PlayerId))
                    {
                        var position = Main.TimeMasterBackTrack[player.PlayerId];
                        player.RpcTeleport(new Vector2 (position.x, position.y));
                        if (pc != player)
                            player?.MyPhysics?.RpcBootFromVent(player.PlayerId);
                        Main.TimeMasterBackTrack.Remove(player.PlayerId);
                    }
                    else
                    {
                        Main.TimeMasterBackTrack.Add(player.PlayerId, new Vector2(player.transform.position.x, player.transform.position.y));
                    }
                }
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
class CoEnterVentPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (AmongUsClient.Instance.IsGameStarted &&
            __instance.myPlayer.IsDouseDone())
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc != __instance.myPlayer)
                {
                    //生存者は焼殺
                    pc.SetRealKiller(__instance.myPlayer);
                    Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                    pc.RpcMurderPlayerV3(pc);
                    Main.PlayerStates[pc.PlayerId].SetDead();
                }
            }
            foreach (var pc in Main.AllPlayerControls) pc.KillFlash();
            CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
            CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
            return true;
        }

        if (AmongUsClient.Instance.IsGameStarted && __instance.myPlayer.IsDrawDone())//完成拉拢任务的玩家跳管后
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Revolutionist);//革命者胜利
            Utils.GetDrawPlayerCount(__instance.myPlayer.PlayerId, out var x);
            CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
            foreach (var apc in x) CustomWinnerHolder.WinnerIds.Add(apc.PlayerId);//胜利玩家
            return true;
        }

        //处理弹出管道的阻塞
        if ((__instance.myPlayer.Data.Role.Role != RoleTypes.Engineer && //不是工程师
        !__instance.myPlayer.CanUseImpostorVentButton()) || //不能使用内鬼的跳管按钮
        (__instance.myPlayer.Is(CustomRoles.Mayor) && Main.MayorUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count) && count >= Options.MayorNumOfUseButton.GetInt()) ||
        (__instance.myPlayer.Is(CustomRoles.Paranoia) && Main.ParaUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count2) && count2 >= Options.ParanoiaNumOfUseButton.GetInt()) ||
        (__instance.myPlayer.Is(CustomRoles.Veteran) && Main.VeteranNumOfUsed.TryGetValue(__instance.myPlayer.PlayerId, out var count3) && count3 < 1) ||
        (__instance.myPlayer.Is(CustomRoles.DovesOfNeace) && Main.DovesOfNeaceNumOfUsed.TryGetValue(__instance.myPlayer.PlayerId, out var count4) && count4 < 1)
        )
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
            writer.WritePacked(127);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            _ = new LateTask(() =>
            {
                int clientId = __instance.myPlayer.GetClientId();
                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                writer2.Write(id);
                AmongUsClient.Instance.FinishRpcImmediately(writer2);
            }, 0.5f, "Fix DesyncImpostor Stuck");
            return false;
        }

        if (__instance.myPlayer.Is(CustomRoles.Swooper))
            Swooper.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.Wraith))
            Wraith.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.Shade))
            Shade.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.Chameleon))
            Chameleon.OnCoEnterVent(__instance, id);

        if (__instance.myPlayer.Is(CustomRoles.DovesOfNeace)) __instance.myPlayer.Notify(GetString("DovesOfNeaceMaxUsage"));
        if (__instance.myPlayer.Is(CustomRoles.Veteran)) __instance.myPlayer.Notify(GetString("VeteranMaxUsage"));

        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
class SetNamePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
    {
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class PlayerControlDiePatch
{
    //https://github.com/Hyz-sui/TownOfHost-H
    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        __instance.RpcRemovePet();
    }
}
[HarmonyPatch(typeof(GameData), nameof(GameData.CompleteTask))]
class GameDataCompleteTaskPatch
{
    public static void Postfix(PlayerControl pc)
    {
        Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
        Main.PlayerStates[pc.PlayerId].UpdateTask(pc);
        Utils.NotifyRoles();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        var player = __instance;

        if (Workhorse.OnCompleteTask(player)) //タスク勝利をキャンセル
            return false;

        //来自资本主义的任务
        if (Main.CapitalismAddTask.ContainsKey(player.PlayerId))
        {
            var taskState = player.GetPlayerTaskState();
            taskState.AllTasksCount += Main.CapitalismAddTask[player.PlayerId];
            Main.CapitalismAddTask.Remove(player.PlayerId);
            taskState.CompletedTasksCount++;
            GameData.Instance.RpcSetTasks(player.PlayerId, Array.Empty<byte>()); //タスクを再配布
            player.SyncSettings();
            Utils.NotifyRoles(SpecifySeer: player);
            return false;
        }

        return true;
    }
    public static void Postfix(PlayerControl __instance)
    {
        var pc = __instance;
        Snitch.OnCompleteTask(pc);

        var isTaskFinish = pc.GetPlayerTaskState().IsTaskFinished;
        if (isTaskFinish && pc.Is(CustomRoles.Snitch) && pc.Is(CustomRoles.Madmate))
        {
            foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
                NameColorManager.Add(impostor.PlayerId, pc.PlayerId, "#ff1919");
            Utils.NotifyRoles(SpecifySeer: pc);
        }
        if ((isTaskFinish &&
            pc.GetCustomRole() is CustomRoles.Doctor or CustomRoles.Sunnyboy) ||
            pc.GetCustomRole() is CustomRoles.SpeedBooster)
        {
            //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
            Utils.MarkEveryoneDirtySettings();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
class PlayerControlProtectPlayerPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
class PlayerControlRemoveProtectionPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
class PlayerControlSetRolePatch
{
    public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
    {
        var target = __instance;
        var targetName = __instance.GetNameWithRole();
        Logger.Info($" {targetName} => {roleType}", "PlayerControl.RpcSetRole");
        if (!ShipStatus.Instance.enabled) return true;
        if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
        {
            var targetIsKiller = target.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId);
            var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
            
            foreach (var seer in Main.AllPlayerControls)
            {
                var self = seer.PlayerId == target.PlayerId;
                var seerIsKiller = seer.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId);

                if (target.Is(CustomRoles.EvilSpirit))
                {
                    ghostRoles[seer] = RoleTypes.GuardianAngel;
                }
                else if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                {
                    ghostRoles[seer] = RoleTypes.ImpostorGhost;
                }
                else
                {
                    ghostRoles[seer] = RoleTypes.CrewmateGhost;
                }
            }
            if (target.Is(CustomRoles.EvilSpirit))
            {
                roleType = RoleTypes.GuardianAngel;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
            {
                roleType = RoleTypes.CrewmateGhost;
            }
            else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
            {
                roleType = RoleTypes.ImpostorGhost;
            }
            else
            {
                foreach ((var seer, var role) in ghostRoles)
                {
                    Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                    target.RpcSetRoleDesync(role, seer.GetClientId());
                }
                return false;
            }
        }
        return true;
    }
}
