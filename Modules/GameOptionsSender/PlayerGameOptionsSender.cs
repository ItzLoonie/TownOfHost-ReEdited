using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using Mathf = UnityEngine.Mathf;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE.Modules;

public class PlayerGameOptionsSender : GameOptionsSender
{
    public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
    public static void SetDirty(byte playerId) =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .Where(sender => sender.player.PlayerId == playerId)
        .ToList().ForEach(sender => sender.SetDirty());
    public static void SetDirtyToAll() =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .ToList().ForEach(sender => sender.SetDirty());

    public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
    public override bool IsDirty { get; protected set; }

    public PlayerControl player;

    public PlayerGameOptionsSender(PlayerControl player)
    {
        this.player = player;
    }
    public void SetDirty() => IsDirty = true;

    public override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            var opt = BuildGameOptions();
            foreach (var com in GameManager.Instance.LogicComponents)
            {
                if (com.TryCast<LogicOptions>(out var lo))
                    lo.SetGameOptions(opt);
            }
            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else base.SendGameOptions();
    }

    public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
    {
        for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
        {
            if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
            {
                SendOptionsArray(optionArray, i, player.GetClientId());
            }
        }
    }
    public static void RemoveSender(PlayerControl player)
    {
        var sender = AllSenders.OfType<PlayerGameOptionsSender>()
        .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
        if (sender == null) return;
        sender.player = null;
        AllSenders.Remove(sender);
    }
    public override IGameOptions BuildGameOptions()
    {
        if (Main.RealOptionsData == null) Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

        var opt = BasedGameOptions;
        AURoleOptions.SetOpt(opt);
        var state = Main.PlayerStates[player.PlayerId];
        opt.BlackOut(state.IsBlackOut);

        CustomRoles role = player.GetCustomRole();
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
                opt.SetVision(true);
                break;
            case CustomRoleTypes.Neutral:
                AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
                break;
            case CustomRoleTypes.Crewmate:
                AURoleOptions.GuardianAngelCooldown = Spiritcaller.SpiritAbilityCooldown.GetFloat();
                break;
        }

        switch (role)
        {
            case CustomRoles.Terrorist:
            case CustomRoles.SabotageMaster:
       //     case CustomRoles.Mario:
            case CustomRoles.EngineerTOHE:
            case CustomRoles.Phantom:
            case CustomRoles.Crewpostor:
          //  case CustomRoles.Jester:
            case CustomRoles.Monitor:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Chameleon:
                AURoleOptions.EngineerCooldown = Chameleon.ChameleonCooldown.GetFloat() + 1f;
                AURoleOptions.EngineerInVentMaxTime = 1f;
                break;
            case CustomRoles.ShapeMaster:
                AURoleOptions.ShapeshifterCooldown = 1f;
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.Warlock:
                AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                AURoleOptions.ShapeshifterDuration = Options.WarlockShiftDuration.GetFloat();
                break;
            case CustomRoles.Escapee:
                AURoleOptions.ShapeshifterCooldown = Options.EscapeeSSCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.EscapeeSSDuration.GetFloat();
                break;
            case CustomRoles.Miner:
                AURoleOptions.ShapeshifterCooldown = Options.MinerSSCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.MinerSSDuration.GetFloat();
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.ApplyGameOptions(player);
                break;
            case CustomRoles.Tracefinder:
                Tracefinder.ApplyGameOptions();
                break;
            case CustomRoles.BountyHunter:
                BountyHunter.ApplyGameOptions();
                break;
            case CustomRoles.Sheriff:
            case CustomRoles.Jailer:
            case CustomRoles.SwordsMan:
            case CustomRoles.Arsonist:
       //     case CustomRoles.Minimalism:
            case CustomRoles.Innocent:
            case CustomRoles.Revolutionist:
            case CustomRoles.Medic:
            case CustomRoles.Crusader:
            case CustomRoles.Provocateur:
            case CustomRoles.Monarch:
            case CustomRoles.Deputy:
            case CustomRoles.Counterfeiter:
            case CustomRoles.Witness:
            case CustomRoles.Succubus:
            case CustomRoles.CursedSoul:
            case CustomRoles.Admirer:
            case CustomRoles.Amnesiac:
                opt.SetVision(false);
                break;
            case CustomRoles.Pestilence:
                opt.SetVision(PlagueBearer.PestilenceHasImpostorVision.GetBool());
                break;
            case CustomRoles.Pelican:
                Pelican.ApplyGameOptions(opt);
                break;
            case CustomRoles.Refugee:
        //    case CustomRoles.Minion:
                opt.SetVision(true);
                break;
            case CustomRoles.Virus:
                opt.SetVision(Virus.ImpostorVision.GetBool());
                break;
            case CustomRoles.Zombie:
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
                break;
            case CustomRoles.Doctor:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                break;
            case CustomRoles.Mayor:
                AURoleOptions.EngineerCooldown =
                    !Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) || count < Options.MayorNumOfUseButton.GetInt()
                    ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Paranoia:
                AURoleOptions.EngineerCooldown =
                    !Main.ParaUsedButtonCount.TryGetValue(player.PlayerId, out var count2) || count2 < Options.ParanoiaNumOfUseButton.GetInt()
                    ? Options.ParanoiaVentCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
       /*     case CustomRoles.Mare:
                Mare.ApplyGameOptions(player.PlayerId);
                break; */
            case CustomRoles.EvilTracker:
                EvilTracker.ApplyGameOptions(player.PlayerId);
                break;
            case CustomRoles.ShapeshifterTOHE:
                AURoleOptions.ShapeshifterCooldown = Options.ShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.ShapeshiftDur.GetFloat();
                break;
            case CustomRoles.Bomber:
                AURoleOptions.ShapeshifterCooldown = Options.BombCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 2f;
                break;
            case CustomRoles.Nuker:
                AURoleOptions.ShapeshifterCooldown = Options.NukeCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = 2f;
                break;
            case CustomRoles.Mafia:
                AURoleOptions.ShapeshifterCooldown = Options.MafiaShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.MafiaShapeshiftDur.GetFloat();
                break;
            case CustomRoles.ScientistTOHE:
                AURoleOptions.ScientistCooldown = Options.ScientistCD.GetFloat();
                AURoleOptions.ScientistBatteryCharge = Options.ScientistDur.GetFloat();
                break;
            case CustomRoles.Wildling:
                AURoleOptions.ShapeshifterCooldown = Wildling.ShapeshiftCD.GetFloat();
                AURoleOptions.ShapeshifterDuration = Wildling.ShapeshiftDur.GetFloat();
                break;
            case CustomRoles.Jackal:
                Jackal.ApplyGameOptions(opt);
                break;
            case CustomRoles.Sidekick:
                Sidekick.ApplyGameOptions(opt);
                break;
            case CustomRoles.Vulture:
                Vulture.ApplyGameOptions(opt);
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.Poisoner:
                Poisoner.ApplyGameOptions(opt);
                break;
            case CustomRoles.Bandit:
                Bandit.ApplyGameOptions(opt);
                break;
            case CustomRoles.Veteran:
                AURoleOptions.EngineerCooldown = Options.VeteranSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Grenadier:
                AURoleOptions.EngineerCooldown = Options.GrenadierSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
     /*       case CustomRoles.Flashbang:
                AURoleOptions.ShapeshifterCooldown = Options.FlashbangSkillCooldown.GetFloat();
                AURoleOptions.ShapeshifterDuration = Options.FlashbangSkillDuration.GetFloat();
                break; */
            case CustomRoles.Lighter:
                AURoleOptions.EngineerInVentMaxTime = 1;
                AURoleOptions.EngineerCooldown = Options.LighterSkillCooldown.GetFloat();
                break;
            case CustomRoles.TimeMaster:
                AURoleOptions.EngineerCooldown = Options.TimeMasterSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.FFF:
            case CustomRoles.Pursuer:
            case CustomRoles.Necromancer:
            case CustomRoles.Ritualist:
                opt.SetVision(true);
                break;
            case CustomRoles.NSerialKiller:
                NSerialKiller.ApplyGameOptions(opt);
                break;
            case CustomRoles.Pyromaniac:
                Pyromaniac.ApplyGameOptions(opt);
                break;
            case CustomRoles.Werewolf:
                Werewolf.ApplyGameOptions(opt);
                break;
            case CustomRoles.Morphling:
                Morphling.ApplyGameOptions();
                break;
            case CustomRoles.Traitor:
                Traitor.ApplyGameOptions(opt);
                break;
            case CustomRoles.Glitch:
                Glitch.ApplyGameOptions(opt);
                break;
            case CustomRoles.NWitch:
                NWitch.ApplyGameOptions(opt);
                break;
            case CustomRoles.CovenLeader:
                CovenLeader.ApplyGameOptions(opt);
                break;
            case CustomRoles.Shroud:
                Shroud.ApplyGameOptions(opt);
                break;
            case CustomRoles.Maverick:
                Maverick.ApplyGameOptions(opt);
                break;
            case CustomRoles.Medusa:
                Medusa.ApplyGameOptions(opt);
                break;
            case CustomRoles.Jinx:
                Jinx.ApplyGameOptions(opt);
                break;
            case CustomRoles.PotionMaster:
                PotionMaster.ApplyGameOptions(opt);
                break;
            case CustomRoles.Pickpocket:
                Pickpocket.ApplyGameOptions(opt);
                break;
            case CustomRoles.Juggernaut:
                opt.SetVision(Juggernaut.HasImpostorVision.GetBool());
                break;
            case CustomRoles.Reverie:
                opt.SetVision(false);
                break;
            case CustomRoles.Jester:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                opt.SetVision(Options.JesterHasImpostorVision.GetBool());
                break;
            case CustomRoles.Doomsayer:
                opt.SetVision(Doomsayer.ImpostorVision.GetBool());
                break;
            case CustomRoles.Infectious:
                opt.SetVision(Infectious.HasImpostorVision.GetBool());
                break;
            case CustomRoles.Lawyer:
                //Main.NormalOptions.CrewLightMod = Lawyer.LawyerVision.GetFloat();
                break;
            case CustomRoles.Shade:
            case CustomRoles.Parasite:
                opt.SetVision(true);
                break;
        /*    case CustomRoles.Chameleon:
                opt.SetVision(false);
                break; */
            
            case CustomRoles.Gamer:
                Gamer.ApplyGameOptions(opt);
                break;
            case CustomRoles.HexMaster:
                HexMaster.ApplyGameOptions(opt);
                break;
            case CustomRoles.Occultist:
                Occultist.ApplyGameOptions(opt);
                break;
            case CustomRoles.Wraith:
                Wraith.ApplyGameOptions(opt);
                break;
            case CustomRoles.Agitater:
                Agitater.ApplyGameOptions(opt);
                break;
            case CustomRoles.DarkHide:
                DarkHide.ApplyGameOptions(opt);
                break;
            case CustomRoles.Workaholic:
                AURoleOptions.EngineerCooldown = Options.WorkaholicVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.ImperiusCurse:
                AURoleOptions.ShapeshifterCooldown = Options.ImperiusCurseShapeshiftCooldown.GetFloat();
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeImperiusCurseShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.QuickShooter:
                AURoleOptions.ShapeshifterCooldown = QuickShooter.ShapeshiftCooldown.GetFloat();
                break;
            case CustomRoles.Camouflager:
                Camouflager.ApplyGameOptions();
                break;
            case CustomRoles.Assassin:
                Assassin.ApplyGameOptions();
                break;
            case CustomRoles.Vampiress:
                Vampiress.ApplyGameOptions();
                break;
            case CustomRoles.Hacker:
                Hacker.ApplyGameOptions();
                break;
            case CustomRoles.Hangman:
                Hangman.ApplyGameOptions();
                break;
            case CustomRoles.Sunnyboy:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = 60f;
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.ApplyGameOptions(opt);
                break;
            case CustomRoles.Banshee:
                Banshee.ApplyGameOptions(opt);
                break;
            case CustomRoles.DovesOfNeace:
                AURoleOptions.EngineerCooldown = Options.DovesOfNeaceCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Disperser:
                Disperser.ApplyGameOptions();
                break;
            case CustomRoles.Farseer:
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Farseer.Vision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Farseer.Vision.GetFloat());
                break;
            case CustomRoles.Dazzler:
                Dazzler.ApplyGameOptions();
                break;
            case CustomRoles.Devourer:
                Devourer.ApplyGameOptions();
                break;
            case CustomRoles.Addict:
                AURoleOptions.EngineerCooldown = Addict.VentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Mario:
                AURoleOptions.EngineerCooldown = Options.MarioVentCD.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Deathpact:
                Deathpact.ApplyGameOptions();
                break;
            case CustomRoles.Twister:
                Twister.ApplyGameOptions();
                break;
            case CustomRoles.Undertaker:
                Undertaker.ApplyGameOptions();
                break;
            case CustomRoles.Spiritcaller:
                opt.SetVision(Spiritcaller.ImpostorVision.GetBool());
                break;
            case CustomRoles.Pitfall:
                Pitfall.ApplyGameOptions();
                break;
            default:
                opt.SetVision(false);
                break;

        }

        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Bewilder) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Hangman)).Any())
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
        }
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Ghoul) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId).Any())
        {
            Main.KillGhoul.Add(player.PlayerId);
        }
   /*     if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Diseased) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId).Any())
        {
            Main.AllPlayerKillCooldown[player.PlayerId] *= Options.DiseasedMultiplier.GetFloat();
            player.SetKillCooldownV3();
            player.ResetKillCooldown();
        //    player.SyncSettings();
        } */

        if (
            (Main.GrenadierBlinding.Any() &&
            (player.GetCustomRole().IsImpostor() ||
            (player.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool()))
            ) || (
            Main.MadGrenadierBlinding.Any() && !player.GetCustomRole().IsImpostorTeam() && !player.Is(CustomRoles.Madmate))
            )
        {
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Options.GrenadierCauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.GrenadierCauseVision.GetFloat());
            }
        }

        if (Main.Lighter.Any() && player.GetCustomRole() == CustomRoles.Lighter)
        {
            opt.SetVision(false);
            if (Utils.IsActive(SystemTypes.Electrical)) opt.SetFloat(FloatOptionNames.CrewLightMod, Options.LighterVisionOnLightsOut.GetFloat() * 5);
            else opt.SetFloat(FloatOptionNames.CrewLightMod, Options.LighterVisionNormal.GetFloat());
        }
   /*     if ((Main.FlashbangInProtect.Count >= 1 && Main.ForFlashbang.Contains(player.PlayerId) && (!player.GetCustomRole().IsCrewmate())))  
        {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Options.FlashbangVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.FlashbangVision.GetFloat());
        } */

        Dazzler.SetDazzled(player, opt);
        Deathpact.SetDeathpactVision(player, opt);

        Spiritcaller.ReduceVision(opt, player);
        Pitfall.SetPitfallTrapVision(opt, player);

        foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
        {
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Flashman:
                    Main.AllPlayerSpeed[player.PlayerId] = Options.FlashmanSpeed.GetFloat();
                    break;
                case CustomRoles.Torch:
                    if (!Utils.IsActive(SystemTypes.Electrical))
                    opt.SetVision(true);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.TorchVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.TorchVision.GetFloat());
                    if (Utils.IsActive(SystemTypes.Electrical) && !Options.TorchAffectedByLights.GetBool())
                    opt.SetVision(true);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.TorchVision.GetFloat() * 5);
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.TorchVision.GetFloat() * 5);
                    break;
                case CustomRoles.Bewilder:
                    opt.SetVision(false);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
                    break;
                case CustomRoles.Sunglasses:
                    opt.SetVision(false);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.SunglassesVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.SunglassesVision.GetFloat());
                    break;
                case CustomRoles.Reach:
                    opt.SetInt(Int32OptionNames.KillDistance, 2);
                    break;
                case CustomRoles.Madmate:
                    opt.SetVision(Options.MadmateHasImpostorVision.GetBool());
                    break;
            }
        }

        AURoleOptions.EngineerCooldown = Mathf.Max(0.01f, AURoleOptions.EngineerCooldown);

        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
        {
            AURoleOptions.KillCooldown = Mathf.Max(0.01f, killCooldown);
        }

        if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
        {
            AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
        }

        state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
        if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        if (Options.AdditionalEmergencyCooldown.GetBool() &&
            Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
        {
            opt.SetInt(
                Int32OptionNames.EmergencyCooldown,
                Options.AdditionalEmergencyCooldownTime.GetInt());
        }
        if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
        {
            opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
        }
        MeetingTimeManager.ApplyGameOptions(opt);

        AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
        AURoleOptions.ProtectionDurationSeconds = 0f;

        return opt;
    }

    public override bool AmValid()
    {
        return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
    }
}
