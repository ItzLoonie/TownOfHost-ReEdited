using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE;

[Flags]
public enum CustomGameMode
{
    Standard = 0x01,
    SoloKombat = 0x02,
    All = int.MaxValue
}

[HarmonyPatch]
public static class Options
{
    private static Task taskOptionsLoad;
    [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
    public static void OptionsLoadStart()
    {
        Logger.Info("Options.Load Start", "Options");
        taskOptionsLoad = Task.Run(Load);
    }
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    public static void WaitOptionsLoad()
    {
        taskOptionsLoad.Wait();
        Logger.Info("Options.Load End", "Options");
    }
    // オプションId
    public const int PresetId = 0;

    // プリセット
    private static readonly string[] presets =
    {
        Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
        Main.Preset4.Value, Main.Preset5.Value
    };

    // ゲームモード
    public static OptionItem GameMode;
    public static CustomGameMode CurrentGameMode
        => GameMode.GetInt() switch
        {
            1 => CustomGameMode.SoloKombat,
            _ => CustomGameMode.Standard
        };

    public static readonly string[] gameModes =
    {
        "Standard", "SoloKombat"
    };

    // MapActive
    public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
    public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
    public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
    public static bool IsActiveAirship => AddedTheAirship.GetBool() || Main.NormalOptions.MapId == 4;

    // 役職数・確率
    public static Dictionary<CustomRoles, int> roleCounts;
    public static Dictionary<CustomRoles, float> roleSpawnChances;
    public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
    public static Dictionary<CustomRoles, StringOptionItem> CustomRoleSpawnChances;
    public static Dictionary<CustomRoles, IntegerOptionItem> CustomAdtRoleSpawnRate;
    public static readonly string[] rates =
    {
        "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
        "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
    };
    public static readonly string[] ratesZeroOne =
    {
        "RoleOff", /*"Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
        "Rate60", "Rate70", "Rate80", "Rate90", */"RoleRate",
    };
    public static readonly string[] ratesToggle =
    {
        "RoleOff", "RoleRate", "RoleOn"
    };
    public static readonly string[] CheatResponsesName =
    {
        "Ban", "Kick", "NoticeMe","NoticeEveryone"
    };
    public static readonly string[] ConfirmEjectionsMode =
    {
        "ConfirmEjections.None",
        "ConfirmEjections.Team",
        "ConfirmEjections.Role"
    };

    // 各役職の詳細設定
    public static OptionItem EnableGM;
    public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
    public static OptionItem GhostsDoTasks;

    public static OptionItem DisableMeeting;
    public static OptionItem DisableCloseDoor;
    public static OptionItem DisableSabotage;
    public static OptionItem DisableTaskWin;

    public static OptionItem KillFlashDuration;
    //public static OptionItem ShareLobby;
    //public static OptionItem ShareLobbyMinPlayer;
    public static OptionItem DisableVanillaRoles;
    public static OptionItem DisableHiddenRoles;
    public static OptionItem DisableSunnyboy;
    public static OptionItem DisableBard;
    public static OptionItem DisableSaboteur;
    public static OptionItem CEMode;
    public static OptionItem ConfirmEjectionsNK;
    public static OptionItem ConfirmEjectionsNonNK;
    public static OptionItem ConfirmEjectionsNeutralAsImp;
    public static OptionItem ShowImpRemainOnEject;
    public static OptionItem ShowNKRemainOnEject;
    public static OptionItem ShowTeamNextToRoleNameOnEject;
    public static OptionItem CheatResponses;
    public static OptionItem LowLoadMode;

    // Detailed Ejections //
    public static OptionItem ExtendedEjections;
    public static OptionItem ConfirmEgoistOnEject;
    public static OptionItem ConfirmSidekickOnEject;
    public static OptionItem ConfirmLoversOnEject;
    public static OptionItem ConfirmRogueOnEject;


    public static OptionItem NonNeutralKillingRolesMinPlayer;
    public static OptionItem NonNeutralKillingRolesMaxPlayer;
    public static OptionItem NeutralKillingRolesMinPlayer;
    public static OptionItem NeutralKillingRolesMaxPlayer;
    public static OptionItem CovenRolesMinPlayer;
    public static OptionItem CovenRolesMaxPlayer;
    public static OptionItem NeutralRoleWinTogether;
    public static OptionItem NeutralWinTogether;

    public static OptionItem DefaultShapeshiftCooldown;
    public static OptionItem DeadImpCantSabotage;
    public static OptionItem ImpKnowAlliesRole;
    public static OptionItem ImpKnowWhosMadmate;
    public static OptionItem MadmateKnowWhosImp;
    public static OptionItem MadmateKnowWhosMadmate;
    public static OptionItem MadmateHasImpostorVision;
    //public static OptionItem MadmateCanFixSabotage;
    public static OptionItem ImpCanKillMadmate;
    public static OptionItem MadmateCanKillImp;
    public static OptionItem JackalCanKillSidekick;
    public static OptionItem SidekickCanKillJackal;
    public static OptionItem SidekickKnowOtherSidekick;
    public static OptionItem SidekickKnowOtherSidekickRole;
    public static OptionItem SidekickCanKillSidekick;

    public static OptionItem ShapeMasterShapeshiftDuration;
    public static OptionItem EGCanGuessImp;
    public static OptionItem EGCanGuessAdt;
    public static OptionItem EGCanGuessTaskDoneSnitch;
    public static OptionItem EGCanGuessTime;
    public static OptionItem EGTryHideMsg;
    public static OptionItem WarlockCanKillAllies;
    public static OptionItem WarlockCanKillSelf;
    public static OptionItem ScavengerKillCooldown;
    public static OptionItem ZombieKillCooldown;
    public static OptionItem ZombieSpeedReduce;
    public static OptionItem EvilWatcherChance;
    public static OptionItem GGCanGuessCrew;
    public static OptionItem GGCanGuessAdt;
    public static OptionItem GGCanGuessTime;
    public static OptionItem GGTryHideMsg;
    public static OptionItem LuckeyProbability;
    public static OptionItem LuckyProbability;
    public static OptionItem VindicatorAdditionalVote;
    public static OptionItem VindicatorHideVote;
    public static OptionItem MayorAdditionalVote;
    public static OptionItem MayorHasPortableButton;
    public static OptionItem MayorNumOfUseButton;
    public static OptionItem MayorHideVote;
    public static OptionItem DoctorTaskCompletedBatteryCharge;
    public static OptionItem SpeedBoosterUpSpeed;
    public static OptionItem SpeedBoosterTimes;
    public static OptionItem GlitchCanVote;
    public static OptionItem TrapperBlockMoveTime;
    public static OptionItem DetectiveCanknowKiller;
    public static OptionItem TransporterTeleportMax;
    public static OptionItem CanTerroristSuicideWin;
    public static OptionItem InnocentCanWinByImp;
    public static OptionItem WorkaholicVentCooldown;
    public static OptionItem WorkaholicCannotWinAtDeath;
    public static OptionItem WorkaholicVisibleToEveryone;
    public static OptionItem WorkaholicGiveAdviceAlive;
    public static OptionItem BaitNotification;
    public static OptionItem JackalWinWithSidekick;
    public static OptionItem ArsonistDouseTime;
    public static OptionItem ArsonistCooldown;
    public static OptionItem JesterCanUseButton;
    public static OptionItem JesterCanVent;
    public static OptionItem LegacyMafia;
    public static OptionItem NotifyGodAlive;
    public static OptionItem MarioVentNumWin;
    public static OptionItem VeteranSkillCooldown;
    public static OptionItem VeteranSkillDuration;
    public static OptionItem VeteranSkillMaxOfUseage;
    public static OptionItem BodyguardProtectRadius;
    public static OptionItem ParanoiaNumOfUseButton;
    public static OptionItem ParanoiaVentCooldown;
    public static OptionItem ImpKnowCyberStarDead;
    public static OptionItem NeutralKnowCyberStarDead;
    public static OptionItem EveryOneKnowSuperStar;
    public static OptionItem MNKillCooldown;
    public static OptionItem MafiaCanKillNum;
    public static OptionItem RetributionistCanKillNum;
    public static OptionItem BomberRadius;
    public static OptionItem CleanerKillCooldown;
    public static OptionItem GuardSpellTimes;
    public static OptionItem FlashWhenTrapBoobyTrap;
    public static OptionItem CapitalismSkillCooldown;
    public static OptionItem GrenadierSkillCooldown;
    public static OptionItem GrenadierSkillDuration;
    public static OptionItem GrenadierCauseVision;
    public static OptionItem GrenadierCanAffectNeutral;
    public static OptionItem RevolutionistDrawTime;
    public static OptionItem RevolutionistCooldown;
    public static OptionItem RevolutionistDrawCount;
    public static OptionItem RevolutionistKillProbability;
    public static OptionItem RevolutionistVentCountDown;
    public static OptionItem ShapeImperiusCurseShapeshiftDuration;
    public static OptionItem ImperiusCurseShapeshiftCooldown;
    public static OptionItem CrewpostorCanKillAllies;
    public static OptionItem CrewpostorKnowsAllies;
    public static OptionItem AlliesKnowCrewpostor;
    public static OptionItem ImpCanBeSeer;
    public static OptionItem CrewCanBeSeer;
    public static OptionItem NeutralCanBeSeer;
    public static OptionItem ImpCanBeGuesser;
    public static OptionItem CrewCanBeGuesser;
    public static OptionItem NeutralCanBeGuesser;
    public static OptionItem ImpCanBeWatcher;
    public static OptionItem CrewCanBeWatcher;
    public static OptionItem NeutralCanBeWatcher;
    public static OptionItem ImpCanBeNecroview;
    public static OptionItem CrewCanBeNecroview;
    public static OptionItem NeutralCanBeNecroview;
    public static OptionItem ImpCanBeOblivious;
    public static OptionItem CrewCanBeOblivious;
    public static OptionItem NeutralCanBeOblivious;
    public static OptionItem ObliviousBaitImmune;
    public static OptionItem ImpCanBeTiebreaker;
    public static OptionItem CrewCanBeTiebreaker;
    public static OptionItem NeutralCanBeTiebreaker;
    public static OptionItem CrewmateCanBeSidekick;
    public static OptionItem NeutralCanBeSidekick;
    public static OptionItem ImpostorCanBeSidekick;
    public static OptionItem ImpCanBeOnbound;
    public static OptionItem CrewCanBeOnbound;
    public static OptionItem NeutralCanBeOnbound;
    public static OptionItem ImpCanBeUnreportable;
    public static OptionItem CrewCanBeUnreportable;
    public static OptionItem NeutralCanBeUnreportable;
    public static OptionItem ImpCanBeLucky;
    public static OptionItem CrewCanBeLucky;
    public static OptionItem NeutralCanBeLucky;
    public static OptionItem ControlCooldown;
    public static OptionItem InhibitorCD;
    public static OptionItem JesterVision;
    public static OptionItem PhantomCanVent;
    public static OptionItem PhantomSnatchesWin;
   // public static OptionItem LawyerVision;
    public static OptionItem ImpCanBeBait;
    public static OptionItem CrewCanBeBait;
    public static OptionItem NeutralCanBeBait;
    public static OptionItem BaitDelayMin;
    public static OptionItem BaitDelayMax;
    public static OptionItem BaitDelayNotify;
    public static OptionItem ImpCanBeTrapper;
    public static OptionItem CrewCanBeTrapper;
    public static OptionItem NeutralCanBeTrapper;
    public static OptionItem ImpCanBeFool;
    public static OptionItem CrewCanBeFool;
    public static OptionItem NeutralCanBeFool;
    public static OptionItem LighterVision;
    public static OptionItem DovesOfNeaceCooldown;
    public static OptionItem DovesOfNeaceMaxOfUseage;
    public static OptionItem BTKillCooldown;
    public static OptionItem TrapOnlyWorksOnTheBodyBoobyTrap;

    //public static OptionItem NSerialKillerKillCD;
    //public static OptionItem NSerialKillerHasImpostorVision;
    //public static OptionItem NSerialKillerCanVent;

    public static OptionItem ParasiteCD;

    public static OptionItem ShapeshiftCD;
    public static OptionItem ShapeshiftDur;

    public static OptionItem MafiaShapeshiftCD;
    public static OptionItem MafiaShapeshiftDur;

    public static OptionItem ScientistDur;
    public static OptionItem ScientistCD;

    public static OptionItem GCanGuessImp;
    public static OptionItem GCanGuessCrew;
    public static OptionItem GCanGuessAdt;
    public static OptionItem GCanGuessTaskDoneSnitch;
    public static OptionItem GTryHideMsg;

//Task Management
    public static OptionItem DisableShortTasks;
    public static OptionItem DisableCleanVent;
    public static OptionItem DisableCalibrateDistributor;
    public static OptionItem DisableChartCourse;
    public static OptionItem DisableStabilizeSteering;
    public static OptionItem DisableCleanO2Filter;
    public static OptionItem DisableUnlockManifolds;
    public static OptionItem DisablePrimeShields;
    public static OptionItem DisableMeasureWeather;
    public static OptionItem DisableBuyBeverage;
    public static OptionItem DisableAssembleArtifact;
    public static OptionItem DisableSortSamples;
    public static OptionItem DisableProcessData;
    public static OptionItem DisableRunDiagnostics;
    public static OptionItem DisableRepairDrill;
    public static OptionItem DisableAlignTelescope;
    public static OptionItem DisableRecordTemperature;
    public static OptionItem DisableFillCanisters;
    public static OptionItem DisableMonitorTree;
    public static OptionItem DisableStoreArtifacts;
    public static OptionItem DisablePutAwayPistols;
    public static OptionItem DisablePutAwayRifles;
    public static OptionItem DisableMakeBurger;
    public static OptionItem DisableCleanToilet;
    public static OptionItem DisableDecontaminate;
    public static OptionItem DisableSortRecords;
    public static OptionItem DisableFixShower;
    public static OptionItem DisablePickUpTowels;
    public static OptionItem DisablePolishRuby;
    public static OptionItem DisableDressMannequin;
    public static OptionItem DisableCommonTasks;
    public static OptionItem DisableSwipeCard;
    public static OptionItem DisableFixWiring;
    public static OptionItem DisableEnterIdCode;
    public static OptionItem DisableInsertKeys;
    public static OptionItem DisableScanBoardingPass;
    public static OptionItem DisableLongTasks;
    public static OptionItem DisableSubmitScan;
    public static OptionItem DisableUnlockSafe;
    public static OptionItem DisableStartReactor;
    public static OptionItem DisableResetBreaker;
    public static OptionItem DisableAlignEngineOutput;
    public static OptionItem DisableInspectSample;
    public static OptionItem DisableEmptyChute;
    public static OptionItem DisableClearAsteroids;
    public static OptionItem DisableWaterPlants;
    public static OptionItem DisableOpenWaterways;
    public static OptionItem DisableReplaceWaterJug;
    public static OptionItem DisableRebootWifi;
    public static OptionItem DisableDevelopPhotos;
    public static OptionItem DisableRewindTapes;
    public static OptionItem DisableStartFans;
    public static OptionItem DisableOtherTasks;
    public static OptionItem DisableUploadData;
    public static OptionItem DisableEmptyGarbage;
    public static OptionItem DisableFuelEngines;
    public static OptionItem DisableDivertPower;
    public static OptionItem DisableActivateWeatherNodes;

    //デバイスブロック
    public static OptionItem DisableDevices;
    public static OptionItem DisableSkeldDevices;
    public static OptionItem DisableSkeldAdmin;
    public static OptionItem DisableSkeldCamera;
    public static OptionItem DisableMiraHQDevices;
    public static OptionItem DisableMiraHQAdmin;
    public static OptionItem DisableMiraHQDoorLog;
    public static OptionItem DisablePolusDevices;
    public static OptionItem DisablePolusAdmin;
    public static OptionItem DisablePolusCamera;
    public static OptionItem DisablePolusVital;
    public static OptionItem DisableAirshipDevices;
    public static OptionItem DisableAirshipCockpitAdmin;
    public static OptionItem DisableAirshipRecordsAdmin;
    public static OptionItem DisableAirshipCamera;
    public static OptionItem DisableAirshipVital;
    public static OptionItem DisableDevicesIgnoreConditions;
    public static OptionItem DisableDevicesIgnoreImpostors;
    public static OptionItem DisableDevicesIgnoreNeutrals;
    public static OptionItem DisableDevicesIgnoreCrewmates;
    public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

    // Maps
    public static OptionItem RandomMapsMode;
    public static OptionItem AddedTheSkeld;
    public static OptionItem AddedMiraHQ;
    public static OptionItem AddedPolus;
    public static OptionItem AddedTheAirship;
    public static OptionItem AddedDleks;
    public static OptionItem RandomSpawn;
    public static OptionItem AirshipAdditionalSpawn;
    public static OptionItem AirshipVariableElectrical;
    public static OptionItem DisableAirshipMovingPlatform;

    // Sabotage
    public static OptionItem CommsCamouflage;
    public static OptionItem DisableReportWhenCC;
    public static OptionItem SabotageTimeControl;
    public static OptionItem PolusReactorTimeLimit;
    public static OptionItem AirshipReactorTimeLimit;
    public static OptionItem LightsOutSpecialSettings;
    public static OptionItem DisableAirshipViewingDeckLightsPanel;
    public static OptionItem DisableAirshipGapRoomLightsPanel;
    public static OptionItem DisableAirshipCargoLightsPanel;

    //Guesser Mode//
    public static OptionItem GuesserMode;
    public static OptionItem CrewmatesCanGuess;
    public static OptionItem ImpostorsCanGuess;
    public static OptionItem NeutralKillersCanGuess;
    public static OptionItem PassiveNeutralsCanGuess;
    public static OptionItem HideGuesserCommands;
    public static OptionItem CanGuessAddons;
    public static OptionItem ImpCanGuessImp;
    public static OptionItem CrewCanGuessCrew;

    // 投票モード
    public static OptionItem VoteMode;
    public static OptionItem WhenSkipVote;
    public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
    public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
    public static OptionItem WhenSkipVoteIgnoreEmergency;
    public static OptionItem WhenNonVote;
    public static OptionItem WhenTie;
    public static readonly string[] voteModes =
    {
        "Default", "Suicide", "SelfVote", "Skip"
    };
    public static readonly string[] tieModes =
    {
        "TieMode.Default", "TieMode.All", "TieMode.Random"
    };
    public static readonly string[] madmateSpawnMode =
    {
        "MadmateSpawnMode.Assign",
        "MadmateSpawnMode.FirstKill",
        "MadmateSpawnMode.SelfVote",
    };
    public static readonly string[] madmateCountMode =
    {
        "MadmateCountMode.None",
        "MadmateCountMode.Imp",
        "MadmateCountMode.Crew",
    };
    public static readonly string[] sidekickCountMode =
    {
        "SidekickCountMode.Jackal",
        "SidekickCountMode.None",
        "SidekickCountMode.Original",
    };
    public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
    public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

    // ボタン回数
    public static OptionItem SyncButtonMode;
    public static OptionItem SyncedButtonCount;
    public static int UsedButtonCount = 0;

    // 全員生存時の会議時間
    public static OptionItem AllAliveMeeting;
    public static OptionItem AllAliveMeetingTime;

    // 追加の緊急ボタンクールダウン
    public static OptionItem AdditionalEmergencyCooldown;
    public static OptionItem AdditionalEmergencyCooldownThreshold;
    public static OptionItem AdditionalEmergencyCooldownTime;

    //転落死
    public static OptionItem LadderDeath;
    public static OptionItem LadderDeathChance;

    // タスク上書き
    public static OverrideTasksData TerroristTasks;
    public static OverrideTasksData TransporterTasks;
    public static OverrideTasksData WorkaholicTasks;
    public static OverrideTasksData CrewpostorTasks;
    public static OverrideTasksData PhantomTasks;

    // その他
    public static OptionItem FixFirstKillCooldown;
    public static OptionItem ShieldPersonDiedFirst;
    public static OptionItem GhostCanSeeOtherRoles;
    public static OptionItem GhostCanSeeOtherVotes;
    public static OptionItem GhostCanSeeDeathReason;
    public static OptionItem GhostIgnoreTasks;
    public static OptionItem KPDCamouflageMode;

    // Guess Restrictions //
    public static OptionItem TerroristCanGuess;
    public static OptionItem WorkaholicCanGuess;
    public static OptionItem PhantomCanGuess;
    public static OptionItem GodCanGuess;

    // プリセット対象外
    public static OptionItem AllowConsole;
    public static OptionItem NoGameEnd;
    public static OptionItem AutoDisplayLastRoles;
    public static OptionItem AutoDisplayKillLog;
    public static OptionItem AutoDisplayLastResult;
    public static OptionItem SuffixMode;
    public static OptionItem HideGameSettings;
    public static OptionItem FormatNameMode;
    public static OptionItem ColorNameMode;
    public static OptionItem DisableEmojiName;
    public static OptionItem ChangeNameToRoleInfo;
    public static OptionItem SendRoleDescriptionFirstMeeting;
    public static OptionItem RoleAssigningAlgorithm;
    public static OptionItem EndWhenPlayerBug;

    public static OptionItem EnableUpMode;
    public static OptionItem AutoKickStart;
    public static OptionItem AutoKickStartAsBan;
    public static OptionItem AutoKickStartTimes;
    public static OptionItem AutoKickStopWords;
    public static OptionItem AutoKickStopWordsAsBan;
    public static OptionItem AutoKickStopWordsTimes;
    public static OptionItem KickAndroidPlayer;
    public static OptionItem KickXboxPlayer;
    public static OptionItem KickPlayStationPlayer;
    public static OptionItem KickNintendoPlayer;
    public static OptionItem ApplyDenyNameList;
    public static OptionItem KickPlayerFriendCodeNotExist;
    public static OptionItem KickLowLevelPlayer;
    public static OptionItem ApplyBanList;
    public static OptionItem ApplyModeratorList;
    public static OptionItem AutoWarnStopWords;

    public static OptionItem DIYGameSettings;
    public static OptionItem PlayerCanSetColor;

    //Add-Ons
    public static OptionItem NameDisplayAddons;
    public static OptionItem AddBracketsToAddons;
    public static OptionItem NoLimitAddonsNumMax;
    public static OptionItem BewilderVision;
    public static OptionItem ImpCanBeAvanger;
    public static OptionItem MadmateSpawnMode;
    public static OptionItem MadmateCountMode;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;
    public static OptionItem MadSnitchTasks;
    public static OptionItem FlashmanSpeed;
    public static OptionItem LoverSpawnChances;
    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;
    public static OptionItem ImpCanBeEgoist;
    public static OptionItem ImpEgoistVisibalToAllies;
    public static OptionItem CrewCanBeEgoist;
    public static OptionItem TicketsPerKill;
    public static OptionItem ImpCanBeDualPersonality;
    public static OptionItem CrewCanBeDualPersonality;
    //public static OptionItem SidekickCountMode;

    public static readonly string[] suffixModes =
    {
        "SuffixMode.None",
        "SuffixMode.Version",
        "SuffixMode.Streaming",
        "SuffixMode.Recording",
        "SuffixMode.RoomHost",
        "SuffixMode.OriginalName",
        "SuffixMode.DoNotKillMe",
        "SuffixMode.NoAndroidPlz",
        "SuffixMode.AutoHost"    };
    public static readonly string[] roleAssigningAlgorithms =
    {
        "RoleAssigningAlgorithm.Default",
        "RoleAssigningAlgorithm.NetRandom",
        "RoleAssigningAlgorithm.HashRandom",
        "RoleAssigningAlgorithm.Xorshift",
        "RoleAssigningAlgorithm.MersenneTwister",
    };
    public static readonly string[] formatNameModes =
    {
        "FormatNameModes.None",
        "FormatNameModes.Color",
        "FormatNameModes.Snacks",
    };
    public static SuffixModes GetSuffixMode() => (SuffixModes)SuffixMode.GetValue();

    public static int SnitchExposeTaskLeft = 1;

    public static bool IsLoaded = false;

    static Options()
    {
        ResetRoleCounts();
    }
    public static void ResetRoleCounts()
    {
        roleCounts = new Dictionary<CustomRoles, int>();
        roleSpawnChances = new Dictionary<CustomRoles, float>();

        foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
        {
            roleCounts.Add(role, 0);
            roleSpawnChances.Add(role, 0);
        }
    }

    public static void SetRoleCount(CustomRoles role, int count)
    {
        roleCounts[role] = count;

        if (CustomRoleCounts.TryGetValue(role, out var option))
        {
            option.SetValue(count - 1);
        }
    }

    public static int GetRoleSpawnMode(CustomRoles role)
    {
        var mode = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
        return mode switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            100 => 1,
            _ => 1,
        };
    }
    public static int GetRoleCount(CustomRoles role)
    {
        var mode = GetRoleSpawnMode(role);
        return mode is 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
    }
    public static float GetRoleChance(CustomRoles role)
    {
        return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetValue()/* / 10f */ : roleSpawnChances[role];
    }
    public static void Load()
    {
        if (IsLoaded) return;
        // 预设
        _ = PresetOptionItem.Create(0, TabGroup.SystemSettings)
            .SetColor(new Color32(255, 235, 4, byte.MaxValue))
            .SetHeader(true);

        // 游戏模式
        GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.GameSettings, false)
            .SetHeader(true);

        #region 职业详细设置
        CustomRoleCounts = new();
        CustomRoleSpawnChances = new();
        CustomAdtRoleSpawnRate = new();
        // 各职业的总体设定
        ImpKnowAlliesRole = BooleanOptionItem.Create(900045, "ImpKnowAlliesRole", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetHeader(true);
        ImpKnowWhosMadmate = BooleanOptionItem.Create(900046, "ImpKnowWhosMadmate", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        ImpCanKillMadmate = BooleanOptionItem.Create(900049, "ImpCanKillMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        MadmateKnowWhosMadmate = BooleanOptionItem.Create(900048, "MadmateKnowWhosMadmate", false, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        MadmateKnowWhosImp = BooleanOptionItem.Create(900047, "MadmateKnowWhosImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateCanKillImp = BooleanOptionItem.Create(900050, "MadmateCanKillImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateHasImpostorVision = BooleanOptionItem.Create(900054, "MadmateHasImpostorVision", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        //MadmateCanFixSabotage = BooleanOptionItem.Create(900058, "MadmateCanFixSabotage", false, TabGroup.ImpostorRoles, false)
            //.SetGameMode(CustomGameMode.Standard);

        DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds);
        DeadImpCantSabotage = BooleanOptionItem.Create(900051, "DeadImpCantSabotage", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        NonNeutralKillingRolesMinPlayer = IntegerOptionItem.Create(505007, "NonNeutralKillingRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        NonNeutralKillingRolesMaxPlayer = IntegerOptionItem.Create(505009, "NonNeutralKillingRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);

        NeutralKillingRolesMinPlayer = IntegerOptionItem.Create(505011, "NeutralKillingRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        NeutralKillingRolesMaxPlayer = IntegerOptionItem.Create(505013, "NeutralKillingRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);

        //     CovenRolesMinPlayer = IntegerOptionItem.Create(505011, "CovenRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
        //         .SetGameMode(CustomGameMode.Standard)
        //         .SetHeader(true)
        //        .SetValueFormat(OptionFormat.Players);
        //    CovenRolesMaxPlayer = IntegerOptionItem.Create(505013, "CovenRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
        //        .SetGameMode(CustomGameMode.Standard)
        //        .SetValueFormat(OptionFormat.Players);

        NeutralRoleWinTogether = BooleanOptionItem.Create(505015, "NeutralRoleWinTogether", false, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetHeader(true);
        NeutralWinTogether = BooleanOptionItem.Create(505016, "NeutralWinTogether", false, TabGroup.NeutralRoles, false)
        .SetParent(NeutralRoleWinTogether)
            .SetGameMode(CustomGameMode.Standard);

        NameDisplayAddons = BooleanOptionItem.Create(6050248, "NameDisplayAddons", true, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
        NoLimitAddonsNumMax = IntegerOptionItem.Create(6050252, "NoLimitAddonsNumMax", new(0, 15, 1), 0, TabGroup.Addons, false);

        // GM
        EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.GameSettings, false)
            .SetColor(Utils.GetRoleColor(CustomRoles.GM))
            .SetHeader(true);
//==================================================================================================================================//

        // Impostor
        TextOptionItem.Create(909090_1, "RoleType.VanillaRoles", TabGroup.ImpostorRoles) // Vanilla
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));
        SetupRoleOptions(180000, TabGroup.ImpostorRoles, CustomRoles.ImpostorTOHE);
        SetupRoleOptions(120000, TabGroup.ImpostorRoles, CustomRoles.ShapeshifterTOHE);
        ShapeshiftCD = FloatOptionItem.Create(120003, "ShapeshiftCooldown", new(1f, 999f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(120004, "ShapeshiftDuration", new(1f, 999f, 1f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ShapeshifterTOHE])
            .SetValueFormat(OptionFormat.Seconds);

        TextOptionItem.Create(909090_2, "RoleType.ImpKilling", TabGroup.ImpostorRoles) // KILLING
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));// KILLING
        EvilTracker.SetupCustomOption();
        Sans.SetupCustomOption();//Arrogance
        Assassin.SetupCustomOption();
        BountyHunter.SetupCustomOption();
        SetupRoleOptions(3200, TabGroup.ImpostorRoles, CustomRoles.CursedWolf); //TOH_Y
        GuardSpellTimes = IntegerOptionItem.Create(3210, "GuardSpellTimes", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf])
        .SetValueFormat(OptionFormat.Times);
        SetupRoleOptions(901065, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
        EGCanGuessTime = IntegerOptionItem.Create(901067, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetValueFormat(OptionFormat.Times);
        EGCanGuessImp = BooleanOptionItem.Create(901069, "EGCanGuessImp", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessAdt = BooleanOptionItem.Create(901073, "EGCanGuessAdt", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessTaskDoneSnitch = BooleanOptionItem.Create(901075, "EGCanGuessTaskDoneSnitch", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGTryHideMsg = BooleanOptionItem.Create(901071, "GuesserTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser])
        .SetColor(Color.green);
        Greedier.SetupCustomOption(); //TOH_Y
        Hangman.SetupCustomOption();
        SetupRoleOptions(6050750, TabGroup.ImpostorRoles, CustomRoles.Inhibitor);
        InhibitorCD = FloatOptionItem.Create(6050752, "KillCooldown", new(0f, 999f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Inhibitor])
            .SetValueFormat(OptionFormat.Seconds);
        Mare.SetupCustomOption();
        SerialKiller.SetupCustomOption();
        QuickShooter.SetupCustomOption();
        Sniper.SetupCustomOption();
        Witch.SetupCustomOption(); //spellcaster
        TextOptionItem.Create(909090_3, "RoleType.ImpSupport", TabGroup.ImpostorRoles)// SUPPORT
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));// SUPPORT
        Hacker.SetupCustomOption(); //anonymous
        AntiAdminer.SetupCustomOption();
        SetupRoleOptions(902135, TabGroup.ImpostorRoles, CustomRoles.Bomber);
        BomberRadius = FloatOptionItem.Create(902137, "BomberRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bomber])
            .SetValueFormat(OptionFormat.Multiplier);
        Camouflager.SetupCustomOption();
        SetupRoleOptions(902233, TabGroup.ImpostorRoles, CustomRoles.Cleaner);
        CleanerKillCooldown = FloatOptionItem.Create(902237, "KillCooldown", new(5f, 999f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Cleaner])
            .SetValueFormat(OptionFormat.Seconds);
        FireWorks.SetupCustomOption();
        Gangster.SetupCustomOption();
        SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
        MafiaCanKillNum = IntegerOptionItem.Create(901615, "MafiaCanKillNum", new(0, 15, 1), 1, TabGroup.ImpostorRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Mafia])
            .SetValueFormat(OptionFormat.Players);
        LegacyMafia = BooleanOptionItem.Create(901616, "LegacyMafia", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mafia]);
        MafiaShapeshiftCD = FloatOptionItem.Create(901617, "ShapeshiftCooldown", new(1f, 999f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(LegacyMafia)
            .SetValueFormat(OptionFormat.Seconds);
        MafiaShapeshiftDur = FloatOptionItem.Create(901618, "ShapeshiftDuration", new(1f, 999f, 1f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(LegacyMafia)
            .SetValueFormat(OptionFormat.Seconds);
        TimeThief.SetupCustomOption();
        SetupRoleOptions(150005, TabGroup.ImpostorRoles, CustomRoles.Vindicator);
        VindicatorAdditionalVote = IntegerOptionItem.Create(150010, "MayorAdditionalVote", new(1, 99, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vindicator])
            .SetValueFormat(OptionFormat.Votes);
        VindicatorHideVote = BooleanOptionItem.Create(150015, "MayorHideVote", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vindicator]);
        TextOptionItem.Create(909090_4, "RoleType.ImpConcealing", TabGroup.ImpostorRoles) //CONCEALING
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));//CONCEALING
        SetupRoleOptions(901595, TabGroup.ImpostorRoles, CustomRoles.Escapee);
        SetupRoleOptions(902422, TabGroup.ImpostorRoles, CustomRoles.ImperiusCurse);
        ShapeImperiusCurseShapeshiftDuration = FloatOptionItem.Create(902433, "ShapeshiftDuration", new(2.5f, 999f, 2.5f), 300, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ImperiusCurse])
            .SetValueFormat(OptionFormat.Seconds);
        ImperiusCurseShapeshiftCooldown = FloatOptionItem.Create(902435, "ShapeshiftCooldown", new(1f, 999f, 1f), 15f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ImperiusCurse])
            .SetValueFormat(OptionFormat.Seconds);
        SetupRoleOptions(901590, TabGroup.ImpostorRoles, CustomRoles.Miner);
        SetupRoleOptions(2000, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
        SetupRoleOptions(905520, TabGroup.ImpostorRoles, CustomRoles.Scavenger);
        ScavengerKillCooldown = FloatOptionItem.Create(905522, "KillCooldown", new(5f, 999f, 2.5f), 40f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Scavenger])
            .SetValueFormat(OptionFormat.Seconds);
        SetupRoleOptions(1200, TabGroup.ImpostorRoles, CustomRoles.ShapeMaster);
        ShapeMasterShapeshiftDuration = FloatOptionItem.Create(1210, "ShapeshiftDuration", new(1, 999, 1), 10, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ShapeMaster])
            .SetValueFormat(OptionFormat.Seconds);
        Swooper.SetupCustomOption();
        SetupRoleOptions(150000, TabGroup.ImpostorRoles, CustomRoles.Trickster);
        Vampire.SetupCustomOption();
        SetupRoleOptions(1400, TabGroup.ImpostorRoles, CustomRoles.Warlock);
        WarlockCanKillAllies = BooleanOptionItem.Create(901406, "CanKillAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Warlock]);
        WarlockCanKillSelf = BooleanOptionItem.Create(901408, "CanKillSelf", false, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Warlock]);
        Wildling.SetupCustomOption();
        TextOptionItem.Create(909090_5, "RoleType.Madmate", TabGroup.ImpostorRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));
        SetupRoleOptions(907090, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        CrewpostorCanKillAllies = BooleanOptionItem.Create(907092, "CanKillAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorKnowsAllies = BooleanOptionItem.Create(907093, "CrewpostorKnowsAllies", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        AlliesKnowCrewpostor = BooleanOptionItem.Create(907094, "AlliesKnowCrewpostor", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Crewpostor]);
        CrewpostorTasks = OverrideTasksData.Create(9079094, TabGroup.ImpostorRoles, CustomRoles.Crewpostor);
        SetupSingleRoleOptions(6050720, TabGroup.ImpostorRoles, CustomRoles.Parasite, 1, zeroOne: false);
        ParasiteCD = FloatOptionItem.Create(6050725, "KillCooldown", new(0f, 999f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Parasite])
            .SetValueFormat(OptionFormat.Seconds);
//==================================================================================================================================//

        // Crewmate
        TextOptionItem.Create(909092_12, "RoleType.VanillaRoles", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        SetupRoleOptions(120005, TabGroup.CrewmateRoles, CustomRoles.EngineerTOHE);
        SetupRoleOptions(120010, TabGroup.CrewmateRoles, CustomRoles.ScientistTOHE);
        ScientistCD = FloatOptionItem.Create(120012, "VitalsCooldown", new(1f, 250f, 1f), 3f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ScientistTOHE])
            .SetValueFormat(OptionFormat.Seconds);
        ScientistDur = FloatOptionItem.Create(120013, "VitalsDuration", new(1f, 250f, 1f), 15f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.ScientistTOHE])
            .SetValueFormat(OptionFormat.Seconds);

        TextOptionItem.Create(909092_4, "RoleType.CrewSupport", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        SetupRoleOptions(8020176, TabGroup.CrewmateRoles, CustomRoles.CyberStar);
        ImpKnowCyberStarDead = BooleanOptionItem.Create(8020178, "ImpKnowCyberStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CyberStar]);
        NeutralKnowCyberStarDead = BooleanOptionItem.Create(8020180, "NeutralKnowCyberStarDead", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CyberStar]);
        Bloodhound.SetupCustomOption();
        SetupRoleOptions(8021015, TabGroup.CrewmateRoles, CustomRoles.Detective);
        DetectiveCanknowKiller = BooleanOptionItem.Create(8021017, "DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);
        SetupRoleOptions(20700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
        DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(20710, "DoctorTaskCompletedBatteryCharge", new(0f, 250f, 1f), 50f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
            .SetValueFormat(OptionFormat.Seconds);
        Divinator.SetupCustomOption();
        SetupRoleOptions(8021615, TabGroup.CrewmateRoles, CustomRoles.Grenadier);
        GrenadierSkillCooldown = FloatOptionItem.Create(8021625, "GrenadierSkillCooldown", new(1f, 180f, 1f), 25f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierSkillDuration = FloatOptionItem.Create(8021627, "GrenadierSkillDuration", new(1f, 999f, 1f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Seconds);
        GrenadierCauseVision = FloatOptionItem.Create(8021637, "GrenadierCauseVision", new(0f, 5f, 0.05f), 0.3f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier])
            .SetValueFormat(OptionFormat.Multiplier);
        GrenadierCanAffectNeutral = BooleanOptionItem.Create(8021647, "GrenadierCanAffectNeutral", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Grenadier]);
        SetupRoleOptions(1020095, TabGroup.CrewmateRoles, CustomRoles.Needy);
        SetupRoleOptions(1020195, TabGroup.CrewmateRoles, CustomRoles.Luckey);
        LuckeyProbability = IntegerOptionItem.Create(1020197, "LuckeyProbability", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Luckey])
            .SetValueFormat(OptionFormat.Percent);
        SabotageMaster.SetupCustomOption(); //Mechanic
        Medicaler.SetupCustomOption();
        Mediumshiper.SetupCustomOption();
        Merchant.SetupCustomOption();
        Mortician.SetupCustomOption();
        SetupRoleOptions(8021618, TabGroup.CrewmateRoles, CustomRoles.Observer);
        SetupRoleOptions(8948971, TabGroup.CrewmateRoles, CustomRoles.DovesOfNeace);
        DovesOfNeaceCooldown = FloatOptionItem.Create(165647, "DovesOfNeaceCooldown", new(1f, 180f, 1f), 30f, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.DovesOfNeace])
            .SetValueFormat(OptionFormat.Seconds);
        DovesOfNeaceMaxOfUseage = IntegerOptionItem.Create(151574, "DovesOfNeaceMaxOfUseage", new(1, 999, 1), 3, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.DovesOfNeace])
            .SetValueFormat(OptionFormat.Times);
        SetupRoleOptions(8020490, TabGroup.CrewmateRoles, CustomRoles.Paranoia);
        ParanoiaNumOfUseButton = IntegerOptionItem.Create(8020493, "ParanoiaNumOfUseButton", new(1, 99, 1), 3, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia])
            .SetValueFormat(OptionFormat.Times);
        ParanoiaVentCooldown = FloatOptionItem.Create(8020495, "ParanoiaVentCooldown", new(0, 990, 1), 10, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia])
            .SetValueFormat(OptionFormat.Seconds);
        Psychic.SetupCustomOption();
        Snitch.SetupCustomOption();
        SetupRoleOptions(8020165, TabGroup.CrewmateRoles, CustomRoles.SuperStar);
        EveryOneKnowSuperStar = BooleanOptionItem.Create(8020168, "EveryOneKnowSuperStar", true, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
        TimeManager.SetupCustomOption();
        Tracker.SetupCustomOption();
        SetupRoleOptions(8021115, TabGroup.CrewmateRoles, CustomRoles.Transporter);
        TransporterTeleportMax = IntegerOptionItem.Create(8021117, "TransporterTeleportMax", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
            .SetValueFormat(OptionFormat.Times);
        TransporterTasks = OverrideTasksData.Create(8021119, TabGroup.CrewmateRoles, CustomRoles.Transporter);

        TextOptionItem.Create(909092_5, "RoleType.CrewKilling", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        SetupRoleOptions(8021515, TabGroup.CrewmateRoles, CustomRoles.Bodyguard);
        BodyguardProtectRadius = FloatOptionItem.Create(8021525, "BodyguardProtectRadius", new(0.5f, 5f, 0.5f), 1.5f, TabGroup.CrewmateRoles, false)
        .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bodyguard])
            .SetValueFormat(OptionFormat.Multiplier);
        Counterfeiter.SetupCustomOption();
        SetupRoleOptions(102255, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        GGCanGuessTime = IntegerOptionItem.Create(102257, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetValueFormat(OptionFormat.Times);
        GGCanGuessCrew = BooleanOptionItem.Create(102259, "GGCanGuessCrew", true, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGCanGuessAdt = BooleanOptionItem.Create(102263, "GGCanGuessAdt", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
        GGTryHideMsg = BooleanOptionItem.Create(102261, "GuesserTryHideMsg", true, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
            .SetColor(Color.green);
        SetupRoleOptions(8027315, TabGroup.CrewmateRoles, CustomRoles.Retributionist);
        RetributionistCanKillNum = IntegerOptionItem.Create(8027317, "RetributionistCanKillNum", new(1, 15, 1), 1, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Retributionist])
            .SetValueFormat(OptionFormat.Players);
        Sheriff.SetupCustomOption();
        SetupRoleOptions(8021315, TabGroup.CrewmateRoles, CustomRoles.Veteran);
        VeteranSkillCooldown = FloatOptionItem.Create(8021325, "VeteranSkillCooldown", new(1f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillDuration = FloatOptionItem.Create(8021327, "VeteranSkillDuration", new(1f, 999f, 1f), 20f, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Seconds);
        VeteranSkillMaxOfUseage = IntegerOptionItem.Create(8021328, "VeteranSkillMaxOfUseage", new(1, 999, 1), 10, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Veteran])
            .SetValueFormat(OptionFormat.Times);
        
        SwordsMan.SetupCustomOption();
        TextOptionItem.Create(909092_1, "RoleType.CrewPower", TabGroup.CrewmateRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        SetupRoleOptions(20900, TabGroup.CrewmateRoles, CustomRoles.Dictator);
        Judge.SetupCustomOption();
        Marshall.SetupCustomOption();
        SetupRoleOptions(20200, TabGroup.CrewmateRoles, CustomRoles.Mayor);
        MayorAdditionalVote = IntegerOptionItem.Create(20210, "MayorAdditionalVote", new(1, 99, 1), 3, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
            .SetValueFormat(OptionFormat.Votes);
        MayorHasPortableButton = BooleanOptionItem.Create(20211, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorNumOfUseButton = IntegerOptionItem.Create(20212, "MayorNumOfUseButton", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false)
        .SetParent(MayorHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
        MayorHideVote = BooleanOptionItem.Create(20213, "MayorHideVote", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        Monarch.SetupCustomOption();
        Farseer.SetupCustomOption();

        // Neutral
        TextOptionItem.Create(909094_0, "RoleType.NeutralBenign", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        Totocalcio.SetupCustomOption();
        Lawyer.SetupCustomOption();
        SetupRoleOptions(50100, TabGroup.NeutralRoles, CustomRoles.Opportunist);
        SetupRoleOptions(6050740, TabGroup.NeutralRoles, CustomRoles.Phantom);
        PhantomCanVent = BooleanOptionItem.Create(6050742, "CanVent", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomSnatchesWin = BooleanOptionItem.Create(6050748, "SnatchesWin", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomCanGuess = BooleanOptionItem.Create(6050747, "CanGuess", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
        PhantomTasks = OverrideTasksData.Create(6050743, TabGroup.NeutralRoles, CustomRoles.Phantom);
        Pursuer.SetupCustomOption();
        SetupSingleRoleOptions(6050530, TabGroup.NeutralRoles, CustomRoles.NWitch, 1, zeroOne: false);
        ControlCooldown = FloatOptionItem.Create(6050532, "ControlCooldown", new(0f, 999f, 2.5f), 30f, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.NWitch])
            .SetValueFormat(OptionFormat.Seconds);

        TextOptionItem.Create(909094_1, "RoleType.NeutralEvil", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        SetupRoleOptions(50500, TabGroup.NeutralRoles, CustomRoles.Arsonist);
        ArsonistDouseTime = FloatOptionItem.Create(50510, "ArsonistDouseTime", new(0f, 10f, 1f), 3f, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        ArsonistCooldown = FloatOptionItem.Create(50511, "Cooldown", new(0f, 990f, 1f), 10f, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
            .SetValueFormat(OptionFormat.Seconds);
        Gamer.SetupCustomOption();
        Executioner.SetupCustomOption();
        SetupRoleOptions(5050233, TabGroup.NeutralRoles, CustomRoles.Innocent);
        InnocentCanWinByImp = BooleanOptionItem.Create(5050266, "InnocentCanWinByImp", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Innocent]);
        SetupRoleOptions(50000, TabGroup.NeutralRoles, CustomRoles.Jester);
        JesterCanUseButton = BooleanOptionItem.Create(6050007, "JesterCanUseButton", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterCanVent = BooleanOptionItem.Create(6050009, "CanVent", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        Pelican.SetupCustomOption();


        TextOptionItem.Create(909094_2, "RoleType.NeutralChaos", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        Collector.SetupCustomOption();
        Succubus.SetupCustomOption();
        SetupRoleOptions(5050850, TabGroup.NeutralRoles, CustomRoles.FFF);
        SetupRoleOptions(50200, TabGroup.NeutralRoles, CustomRoles.Terrorist);
        CanTerroristSuicideWin = BooleanOptionItem.Create(50210, "CanTerroristSuicideWin", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist]);
        TerroristCanGuess = BooleanOptionItem.Create(55210, "CanGuess", true, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist]);
        //50220~50223を使用
        TerroristTasks = OverrideTasksData.Create(50220, TabGroup.NeutralRoles, CustomRoles.Terrorist);
        SetupRoleOptions(60100, TabGroup.NeutralRoles, CustomRoles.Workaholic); //TOH_Y
        WorkaholicCannotWinAtDeath = BooleanOptionItem.Create(60113, "WorkaholicCannotWinAtDeath", false, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicVentCooldown = FloatOptionItem.Create(60112, "VentCooldown", new(0f, 180f, 2.5f), 0f, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic])
            .SetValueFormat(OptionFormat.Seconds);
        WorkaholicVisibleToEveryone = BooleanOptionItem.Create(60114, "WorkaholicVisibleToEveryone", true, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);
        WorkaholicGiveAdviceAlive = BooleanOptionItem.Create(60115, "WorkaholicGiveAdviceAlive", true, TabGroup.NeutralRoles, false)
        .SetParent(WorkaholicVisibleToEveryone);
        WorkaholicTasks = OverrideTasksData.Create(60116, TabGroup.NeutralRoles, CustomRoles.Workaholic);
        WorkaholicCanGuess = BooleanOptionItem.Create(69114, "CanGuess", true, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Workaholic]);

        TextOptionItem.Create(909094_3, "RoleType.NeutralKilling", TabGroup.NeutralRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        BloodKnight.SetupCustomOption();
        HexMaster.SetupCustomOption();
        Infectious.SetupCustomOption();
        Jackal.SetupCustomOption();
        Juggernaut.SetupCustomOption();
        Poisoner.SetupCustomOption();
        NSerialKiller.SetupCustomOption();
        DarkHide.SetupCustomOption(); //TOH_Y
        Virus.SetupCustomOption();
        Wraith.SetupCustomOption();
        


        // Add-Ons
        AddBracketsToAddons = BooleanOptionItem.Create(6050512, "BracketAddons", false, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);
 
        TextOptionItem.Create(909096_1, "RoleType.Helpful", TabGroup.Addons) // HELPFUL
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));
        SetupAdtRoleOptions(20000, CustomRoles.Bait, canSetNum: true);
        ImpCanBeBait = BooleanOptionItem.Create(20003, "ImpCanBeBait", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        CrewCanBeBait = BooleanOptionItem.Create(20004, "CrewCanBeBait", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        NeutralCanBeBait = BooleanOptionItem.Create(20005, "NeutralCanBeBait", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitDelayMin = FloatOptionItem.Create(20006, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        BaitDelayMax = FloatOptionItem.Create(20007, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        BaitDelayNotify = BooleanOptionItem.Create(20008, "BaitDelayNotify", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        BaitNotification = BooleanOptionItem.Create(20009, "BaitNotification", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);
        SetupAdtRoleOptions(20800, CustomRoles.Trapper, canSetNum: true);
        ImpCanBeTrapper = BooleanOptionItem.Create(20803, "ImpCanBeTrapper", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper]);
        CrewCanBeTrapper = BooleanOptionItem.Create(20804, "CrewCanBeTrapper", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper]);
        NeutralCanBeTrapper = BooleanOptionItem.Create(20805, "NeutralCanBeTrapper", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper]);
        TrapperBlockMoveTime = FloatOptionItem.Create(20810, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Trapper])
            .SetValueFormat(OptionFormat.Seconds);

        SetupAdtRoleOptions(6050350, CustomRoles.Seer, canSetNum: true);
        ImpCanBeSeer = BooleanOptionItem.Create(6050353, "ImpCanBeSeer", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Seer]);
        CrewCanBeSeer = BooleanOptionItem.Create(6050354, "CrewCanBeSeer", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Seer]);
        NeutralCanBeSeer = BooleanOptionItem.Create(6050355, "NeutralCanBeSeer", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Seer]);
        SetupAdtRoleOptions(6050340, CustomRoles.Lighter, canSetNum: true);
        LighterVision = FloatOptionItem.Create(6050345, "LighterVision", new(0.5f, 5f, 0.25f), 1.25f, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
            .SetValueFormat(OptionFormat.Multiplier);
        SetupAdtRoleOptions(6050500, CustomRoles.Necroview, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeNecroview = BooleanOptionItem.Create(6050503, "ImpCanBeNecroview", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        CrewCanBeNecroview = BooleanOptionItem.Create(6050504, "CrewCanBeNecroview", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        NeutralCanBeNecroview = BooleanOptionItem.Create(6050513, "NeutralCanBeNecroview", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Necroview]);
        SetupAdtRoleOptions(6050605, CustomRoles.Onbound, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeOnbound = BooleanOptionItem.Create(6050616, "ImpCanBeOnbound", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        CrewCanBeOnbound = BooleanOptionItem.Create(6050608, "CrewCanBeOnbound", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        NeutralCanBeOnbound = BooleanOptionItem.Create(6050609, "NeutralCanBeOnbound", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Onbound]);
        SetupAdtRoleOptions(6052490, CustomRoles.Reach, canSetNum: true);
        SetupAdtRoleOptions(6052333, CustomRoles.DualPersonality, canSetNum: true);
        ImpCanBeDualPersonality = BooleanOptionItem.Create(6052338, "ImpCanBeDualPersonality", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.DualPersonality]);
        CrewCanBeDualPersonality = BooleanOptionItem.Create(6052340, "CrewCanBeDualPersonality", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.DualPersonality]);
        SetupAdtRoleOptions(6050360, CustomRoles.Brakar, canSetNum: true);
        ImpCanBeTiebreaker = BooleanOptionItem.Create(6050363, "ImpCanBeTiebreaker", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Brakar]);
        CrewCanBeTiebreaker = BooleanOptionItem.Create(6050364, "CrewCanBeTiebreaker", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Brakar]);
        NeutralCanBeTiebreaker = BooleanOptionItem.Create(6050365, "NeutralCanBeTiebreaker", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Brakar]);
        SetupAdtRoleOptions(6050320, CustomRoles.Watcher, canSetNum: true);
        ImpCanBeWatcher = BooleanOptionItem.Create(6050323, "ImpCanBeWatcher", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        CrewCanBeWatcher = BooleanOptionItem.Create(6050324, "CrewCanBeWatcher", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        NeutralCanBeWatcher = BooleanOptionItem.Create(6050325, "NeutralCanBeWatcher", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Watcher]);
        SetupAdtRoleOptions(6051320, CustomRoles.Lucky, canSetNum: true);
        LuckyProbability = IntegerOptionItem.Create(6051330, "LuckyProbability", new(0, 100, 5), 50, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky])
            .SetValueFormat(OptionFormat.Percent);
        ImpCanBeLucky = BooleanOptionItem.Create(6051340, "ImpCanBeLucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
        CrewCanBeLucky = BooleanOptionItem.Create(6051350, "CrewCanBeLucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
        NeutralCanBeLucky = BooleanOptionItem.Create(6051360, "NeutralCanBeLucky", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);

        TextOptionItem.Create(909096_2, "RoleType.Harmful", TabGroup.Addons) // HARMFUL
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));
        SetupAdtRoleOptions(6050450, CustomRoles.Avanger, canSetNum: true);
        ImpCanBeAvanger = BooleanOptionItem.Create(6050455, "ImpCanBeAvanger", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        SetupAdtRoleOptions(6050380, CustomRoles.Bewilder, canSetNum: true);
        BewilderVision = FloatOptionItem.Create(6050383, "BewilderVision", new(0f, 5f, 0.05f), 0.6f, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder])
            .SetValueFormat(OptionFormat.Multiplier);
        SetupAdtRoleOptions(6050610, CustomRoles.Unreportable, canSetNum: true);
        ImpCanBeUnreportable = BooleanOptionItem.Create(6050615, "ImpCanBeUnreportable", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        CrewCanBeUnreportable = BooleanOptionItem.Create(6050614, "CrewCanBeUnreportable", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        NeutralCanBeUnreportable = BooleanOptionItem.Create(6050613, "NeutralCanBeUnreportable", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Unreportable]);
        SetupAdtRoleOptions(6050370, CustomRoles.Oblivious, canSetNum: true);
        ImpCanBeOblivious = BooleanOptionItem.Create(6050373, "ImpCanBeOblivious", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        CrewCanBeOblivious = BooleanOptionItem.Create(6050374, "CrewCanBeOblivious", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        NeutralCanBeOblivious = BooleanOptionItem.Create(6050375, "NeutralCanBeOblivious", true, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        ObliviousBaitImmune = BooleanOptionItem.Create(6050376, "ObliviousBaitImmune", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Oblivious]);
        Workhorse.SetupCustomOption();
        TextOptionItem.Create(909096_3, "RoleType.Impostor", TabGroup.Addons) // IMPOSTOR
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 25, 25, byte.MaxValue));
        SetupAdtRoleOptions(6050390, CustomRoles.Madmate, canSetNum: true, canSetChance: false);
        MadmateSpawnMode = StringOptionItem.Create(6060444, "MadmateSpawnMode", madmateSpawnMode, 0, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadmateCountMode = StringOptionItem.Create(6060445, "MadmateCountMode", madmateCountMode, 0, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SheriffCanBeMadmate = BooleanOptionItem.Create(6050395, "SheriffCanBeMadmate", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MayorCanBeMadmate = BooleanOptionItem.Create(6050396, "MayorCanBeMadmate", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(6050397, "NGuesserCanBeMadmate", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        SnitchCanBeMadmate = BooleanOptionItem.Create(6050398, "SnitchCanBeMadmate", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        MadSnitchTasks = IntegerOptionItem.Create(6050399, "MadSnitchTasks", new(1, 99, 1), 3, TabGroup.Addons, false)
        .SetParent(SnitchCanBeMadmate)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create(6050405, "JudgeCanBeMadmate", false, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Madmate]);
        LastImpostor.SetupCustomOption();
        SetupAdtRoleOptions(6051677, CustomRoles.Mimic, canSetNum: true, tab: TabGroup.Addons);        
        SetupAdtRoleOptions(6051660, CustomRoles.TicketsStealer, canSetNum: true, tab: TabGroup.Addons);
        TicketsPerKill = FloatOptionItem.Create(6051666, "TicketsPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.Addons, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.TicketsStealer]);

 
        TextOptionItem.Create(909096_4, "RoleType.Neut", TabGroup.Addons) // NEUTRAL
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        SetupLoversRoleOptionsToggle(50300);
        SetupAdtRoleOptions(6050900, CustomRoles.Rogue, canSetNum: true);





        // 乐子职业

        // 内鬼
        TextOptionItem.Create(909090, "OtherRoles.ImpostorRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(247, 70, 49, byte.MaxValue));
        SetupRoleOptions(901635, TabGroup.OtherRoles, CustomRoles.Minimalism);
        MNKillCooldown = FloatOptionItem.Create(901638, "KillCooldown", new(2.5f, 999f, 2.5f), 10f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Minimalism])
            .SetValueFormat(OptionFormat.Seconds);
        SetupRoleOptions(901790, TabGroup.OtherRoles, CustomRoles.Zombie);
        ZombieKillCooldown = FloatOptionItem.Create(901792, "KillCooldown", new(0f, 999f, 2.5f), 5f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Seconds);
        ZombieSpeedReduce = FloatOptionItem.Create(901794, "ZombieSpeedReduce", new(0.0f, 1.0f, 0.1f), 0.1f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Multiplier);
        SetupRoleOptions(902265, TabGroup.OtherRoles, CustomRoles.BoobyTrap);
        BTKillCooldown = FloatOptionItem.Create(902267, "KillCooldown", new(2.5f, 999f, 2.5f), 20f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.BoobyTrap])
            .SetValueFormat(OptionFormat.Seconds);
        TrapOnlyWorksOnTheBodyBoobyTrap = BooleanOptionItem.Create(902268, "TrapOnlyWorksOnTheBodyBoobyTrap", true, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.BoobyTrap]);
        SetupRoleOptions(902555, TabGroup.OtherRoles, CustomRoles.Capitalism);
        CapitalismSkillCooldown = FloatOptionItem.Create(902558, "CapitalismSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.OtherRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Capitalism])
            .SetValueFormat(OptionFormat.Seconds);
        BallLightning.SetupCustomOption();
        Eraser.SetupCustomOption();
        SetupRoleOptions(902622, TabGroup.OtherRoles, CustomRoles.OverKiller);
        Disperser.SetupCustomOption();

        // 船员
        TextOptionItem.Create(909092_3, "OtherRoles.CrewmateRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        SetupRoleOptions(20600, TabGroup.OtherRoles, CustomRoles.SpeedBooster);
        SpeedBoosterUpSpeed = FloatOptionItem.Create(20610, "SpeedBoosterUpSpeed", new(0.1f, 1.0f, 0.1f), 0.2f, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedBoosterTimes = IntegerOptionItem.Create(20611, "SpeedBoosterTimes", new(1, 99, 1), 5, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
            .SetValueFormat(OptionFormat.Times);
        SetupRoleOptions(8023487, TabGroup.OtherRoles, CustomRoles.Glitch);
        GlitchCanVote = BooleanOptionItem.Create(8023489, "GlitchCanVote", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Glitch]);
        // 中立
        TextOptionItem.Create(909094_4, "OtherRoles.NeutralRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(127, 140, 141, byte.MaxValue));
        SetupRoleOptions(5050965, TabGroup.OtherRoles, CustomRoles.God);
        NotifyGodAlive = BooleanOptionItem.Create(5050967, "NotifyGodAlive", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.God]);
        GodCanGuess = BooleanOptionItem.Create(5050968, "CanGuess", false, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.God]);
        SetupRoleOptions(5050110, TabGroup.OtherRoles, CustomRoles.Mario);
        MarioVentNumWin = IntegerOptionItem.Create(5050112, "MarioVentNumWin", new(5, 900, 5), 55, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Mario])
            .SetValueFormat(OptionFormat.Times);
        SetupRoleOptions(5050600, TabGroup.OtherRoles, CustomRoles.Revolutionist);
        RevolutionistDrawTime = FloatOptionItem.Create(5050610, "RevolutionistDrawTime", new(0f, 10f, 1f), 3f, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        RevolutionistCooldown = FloatOptionItem.Create(5050615, "RevolutionistCooldown", new(5f, 100f, 1f), 10f, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        RevolutionistDrawCount = IntegerOptionItem.Create(5050617, "RevolutionistDrawCount", new(1, 14, 1), 6, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Players);
        RevolutionistKillProbability = IntegerOptionItem.Create(5050619, "RevolutionistKillProbability", new(0, 100, 5), 15, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Percent);
        RevolutionistVentCountDown = FloatOptionItem.Create(5050621, "RevolutionistVentCountDown", new(1f, 180f, 1f), 15f, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Revolutionist])
            .SetValueFormat(OptionFormat.Seconds);
        SetupRoleOptions(5051412, TabGroup.OtherRoles, CustomRoles.Provocateur);
        Amor.SetupCustomOption();

        // 副职
        TextOptionItem.Create(909096_0, "OtherRoles.Addons", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));
        SetupAdtRoleOptions(6050310, CustomRoles.Ntr, tab: TabGroup.OtherRoles);
        SetupAdtRoleOptions(6050330, CustomRoles.Flashman, canSetNum: true, tab: TabGroup.OtherRoles);
        FlashmanSpeed = FloatOptionItem.Create(6050335, "FlashmanSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Flashman])
            .SetValueFormat(OptionFormat.Multiplier);
        SetupAdtRoleOptions(6050480, CustomRoles.Youtuber, canSetNum: true, tab: TabGroup.OtherRoles);
        SetupAdtRoleOptions(6050490, CustomRoles.Egoist, canSetNum: true, tab: TabGroup.OtherRoles);
        CrewCanBeEgoist = BooleanOptionItem.Create(6050497, "CrewCanBeEgoist", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpCanBeEgoist = BooleanOptionItem.Create(6050495, "ImpCanBeEgoist", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
        ImpEgoistVisibalToAllies = BooleanOptionItem.Create(6050496, "ImpEgoistVisibalToAllies", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
    /*    SetupAdtRoleOptions(6050505, CustomRoles.Sidekick, canSetNum: true, tab: TabGroup.OtherRoles);
        SidekickCountMode = StringOptionItem.Create(6050595, "SidekickCountMode", sidekickCountMode, 0, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
        CrewmateCanBeSidekick = BooleanOptionItem.Create(6050510, "CrewmatesCanBeSidekick", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
        NeutralCanBeSidekick = BooleanOptionItem.Create(6050515, "NeutralsCanBeSidekick", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
        ImpostorCanBeSidekick = BooleanOptionItem.Create(6050540, "ImpostorsCanBeSidekick", false, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
        SidekickCanKillJackal = BooleanOptionItem.Create(6050520, "SidekickCanKillJackal", false, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
     //   JackalWinWithSidekick = BooleanOptionItem.Create(6050580, "JackalWinWithSidekick", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
        SidekickKnowOtherSidekick = BooleanOptionItem.Create(6050585, "SidekickKnowOtherSidekick", false, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Sidekick]);
        SidekickKnowOtherSidekickRole = BooleanOptionItem.Create(6050590, "SidekickKnowOtherSidekickRole", false, TabGroup.OtherRoles, false)
        .SetParent(SidekickKnowOtherSidekick);
        SidekickCanKillSidekick = BooleanOptionItem.Create(6050600, "SidekickCanKillSidekick", false, TabGroup.OtherRoles, false)
        .SetParent(SidekickKnowOtherSidekick); */
        SetupAdtRoleOptions(6050550, CustomRoles.Guesser, canSetNum: true, tab: TabGroup.OtherRoles);
        ImpCanBeGuesser = BooleanOptionItem.Create(6060000, "ImpCanBeGuesser", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CrewCanBeGuesser = BooleanOptionItem.Create(6060005, "CrewCanBeGuesser", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        NeutralCanBeGuesser = BooleanOptionItem.Create(6060010, "NeutralCanBeGuesser", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
     //   GCanGuessImp = BooleanOptionItem.Create(6050555, "GCanGuessImp", false, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
     //   GCanGuessCrew = BooleanOptionItem.Create(6050560, "GCanGuessCrew", false, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessAdt = BooleanOptionItem.Create(6050565, "GCanGuessAdt", false, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GCanGuessTaskDoneSnitch = BooleanOptionItem.Create(6050570, "GCanGuessTaskDoneSnitch", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        GTryHideMsg = BooleanOptionItem.Create(6050575, "GuesserTryHideMsg", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Guesser])
            .SetColor(Color.green);
        SetupAdtRoleOptions(6050440, CustomRoles.Fool, canSetNum: true, tab: TabGroup.OtherRoles);
        ImpCanBeFool = BooleanOptionItem.Create(6050443, "ImpCanBeFool", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        CrewCanBeFool = BooleanOptionItem.Create(6050444, "CrewCanBeFool", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        NeutralCanBeFool = BooleanOptionItem.Create(6050445, "NeutralCanBeFool", true, TabGroup.OtherRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);

          

        #endregion

        #region 系统设置

        KickLowLevelPlayer = IntegerOptionItem.Create(6090074, "KickLowLevelPlayer", new(0, 100, 1), 0, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Level)
            .SetHeader(true);
        KickAndroidPlayer = BooleanOptionItem.Create(6090071, "KickAndroidPlayer", false, TabGroup.SystemSettings, false);
        KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.SystemSettings, true);
        ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.SystemSettings, true);
        ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.SystemSettings, true);
        ApplyModeratorList = BooleanOptionItem.Create(6090072, "ApplyModeratorList", false, TabGroup.SystemSettings, false);
        AutoKickStart = BooleanOptionItem.Create(1_000_010, "AutoKickStart", false, TabGroup.SystemSettings, false);
        AutoKickStartTimes = IntegerOptionItem.Create(1_000_024, "AutoKickStartTimes", new(0, 99, 1), 1, TabGroup.SystemSettings, false)
        .SetParent(AutoKickStart)
            .SetValueFormat(OptionFormat.Times);
        AutoKickStartAsBan = BooleanOptionItem.Create(1_000_026, "AutoKickStartAsBan", false, TabGroup.SystemSettings, false)
        .SetParent(AutoKickStart);
        AutoKickStopWords = BooleanOptionItem.Create(1_000_011, "AutoKickStopWords", false, TabGroup.SystemSettings, false);
        AutoKickStopWordsTimes = IntegerOptionItem.Create(1_000_022, "AutoKickStopWordsTimes", new(0, 99, 1), 3, TabGroup.SystemSettings, false)
        .SetParent(AutoKickStopWords)
            .SetValueFormat(OptionFormat.Times);
        AutoKickStopWordsAsBan = BooleanOptionItem.Create(1_000_028, "AutoKickStopWordsAsBan", false, TabGroup.SystemSettings, false)
        .SetParent(AutoKickStopWords);
        AutoWarnStopWords = BooleanOptionItem.Create(1_000_012, "AutoWarnStopWords", false, TabGroup.SystemSettings, false);

    //    ShareLobby = BooleanOptionItem.Create(6090065, "ShareLobby", true, TabGroup.SystemSettings, false)
   //         .SetHeader(true)
   //         .SetColor(Color.cyan);
   //     ShareLobbyMinPlayer = IntegerOptionItem.Create(6090067, "ShareLobbyMinPlayer", new(3, 12, 1), 5, TabGroup.SystemSettings, false).SetParent(ShareLobby)
   //         .SetValueFormat(OptionFormat.Players);

        LowLoadMode = BooleanOptionItem.Create(6080069, "LowLoadMode", false, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(Color.red);

        EndWhenPlayerBug = BooleanOptionItem.Create(1_000_025, "EndWhenPlayerBug", true, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(Color.blue);

        CheatResponses = StringOptionItem.Create(6090121, "CheatResponses", CheatResponsesName, 0, TabGroup.SystemSettings, false)
            .SetHeader(true);

        //HighLevelAntiCheat = StringOptionItem.Create(6090123, "HighLevelAntiCheat", CheatResponsesName, 0, TabGroup.SystemSettings, false)
        //.SetHeader(true);

        AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.SystemSettings, false)
            .SetHeader(true);
        AutoDisplayLastRoles = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastRoles", true, TabGroup.SystemSettings, false);
        AutoDisplayLastResult = BooleanOptionItem.Create(1_000_007, "AutoDisplayLastResult", true, TabGroup.SystemSettings, false);

        SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.SystemSettings, true)
            .SetHeader(true);
        HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.SystemSettings, false);
        DIYGameSettings = BooleanOptionItem.Create(1_000_013, "DIYGameSettings", false, TabGroup.SystemSettings, false);
        PlayerCanSetColor = BooleanOptionItem.Create(1_000_014, "PlayerCanSetColor", false, TabGroup.SystemSettings, false);
        FormatNameMode = StringOptionItem.Create(1_000_003, "FormatNameMode", formatNameModes, 0, TabGroup.SystemSettings, false);
        DisableEmojiName = BooleanOptionItem.Create(1_000_016, "DisableEmojiName", true, TabGroup.SystemSettings, false);
        ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", false, TabGroup.SystemSettings, false);
        SendRoleDescriptionFirstMeeting = BooleanOptionItem.Create(1_000_0016, "SendRoleDescriptionFirstMeeting", false, TabGroup.SystemSettings, false);
        NoGameEnd = BooleanOptionItem.Create(900_002, "NoGameEnd", false, TabGroup.SystemSettings, false)
            .SetColor(Color.red);
        AllowConsole = BooleanOptionItem.Create(900_005, "AllowConsole", false, TabGroup.SystemSettings, false)
            .SetColor(Color.red);
        RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", roleAssigningAlgorithms, 4, TabGroup.SystemSettings, true)
           .RegisterUpdateValueEvent(
                (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
            );
        KPDCamouflageMode = BooleanOptionItem.Create(1_000_015, "KPDCamouflageMode", false, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(255, 192, 203, byte.MaxValue));

        //DebugModeManager.SetupCustomOption();

        EnableUpMode = BooleanOptionItem.Create(6090665, "EnableYTPlan", false, TabGroup.SystemSettings, false)
            .SetColor(Color.cyan)
            .SetHeader(true);

        #endregion 

        #region 游戏设置

        //SoloKombat
        SoloKombatManager.SetupCustomOption();

        TextOptionItem.Create(66_123_119, "MenuTitle.Guessers", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);
        GuesserMode = BooleanOptionItem.Create(6050521, "GuesserMode", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(Color.yellow)
            .SetHeader(true);
        CrewmatesCanGuess = BooleanOptionItem.Create(6050820, "CrewmatesCanGuess", false, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        ImpostorsCanGuess = BooleanOptionItem.Create(6050825, "ImpostorsCanGuess", false, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        NeutralKillersCanGuess = BooleanOptionItem.Create(6050830, "NeutralKillersCanGuess", false, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        PassiveNeutralsCanGuess = BooleanOptionItem.Create(6050835, "PassiveNeutralsCanGuess", false, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        CanGuessAddons = BooleanOptionItem.Create(6050845, "CanGuessAddons", true, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        CrewCanGuessCrew = BooleanOptionItem.Create(6050850, "CrewCanGuessCrew", true, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        ImpCanGuessImp = BooleanOptionItem.Create(6050855, "ImpCanGuessImp", true, TabGroup.GameSettings, false)
        .SetParent(GuesserMode);
        HideGuesserCommands = BooleanOptionItem.Create(6050840, "GuesserTryHideMsg", true, TabGroup.GameSettings, false)
        .SetParent(GuesserMode)
            .SetColor(Color.green);

        //驱逐相关设定
        TextOptionItem.Create(66_123_126, "MenuTitle.Ejections", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));

        CEMode = StringOptionItem.Create(6091223, "ConfirmEjectionsMode", ConfirmEjectionsMode, 2, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowImpRemainOnEject = BooleanOptionItem.Create(6090115, "ShowImpRemainOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowNKRemainOnEject = BooleanOptionItem.Create(6090119, "ShowNKRemainOnEject", true, TabGroup.GameSettings, false)
        .SetParent(ShowImpRemainOnEject)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowTeamNextToRoleNameOnEject = BooleanOptionItem.Create(6090125, "ShowTeamNextToRoleNameOnEject", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
                    ConfirmEgoistOnEject = BooleanOptionItem.Create(6090122, "ConfirmEgoistOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue))
            .SetHeader(true);
                    ConfirmSidekickOnEject = BooleanOptionItem.Create(6090124, "ConfirmSidekickOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
                    ConfirmLoversOnEject = BooleanOptionItem.Create(6090126, "ConfirmLoversOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
                    ConfirmRogueOnEject = BooleanOptionItem.Create(6090128, "ConfirmRogueOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));

        //Maps Settings
        TextOptionItem.Create(66_123_127, "MenuTitle.MapsSettings", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));

        // Random Maps Mode
        RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.GameSettings, false)
        .SetParent(RandomMapsMode);
        AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.GameSettings, false)
        .SetParent(RandomMapsMode);
        AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.GameSettings, false)
        .SetParent(RandomMapsMode);
        AddedTheAirship = BooleanOptionItem.Create(100404, "AddedTheAirship", false, TabGroup.GameSettings, false)
        .SetParent(RandomMapsMode);
        // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode);

        // Random Spawn
        RandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        AirshipAdditionalSpawn = BooleanOptionItem.Create(101301, "AirshipAdditionalSpawn", false, TabGroup.GameSettings, false)
        .SetParent(RandomSpawn)
            .SetGameMode(CustomGameMode.Standard);

        // Airship Variable Electrical
        AirshipVariableElectrical = BooleanOptionItem.Create(101600, "AirshipVariableElectrical", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));
        //Disable Airship Moving Platform
        DisableAirshipMovingPlatform = BooleanOptionItem.Create(101700, "DisableAirshipMovingPlatform", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(19, 188, 233, byte.MaxValue));


        // Sabotage
        TextOptionItem.Create(66_123_121, "MenuTitle.Sabotage", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetHeader(true);

        // CommsCamouflage
        CommsCamouflage = BooleanOptionItem.Create(900_013, "CommsCamouflage", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));
        DisableReportWhenCC = BooleanOptionItem.Create(900_015, "DisableReportWhenCC", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(243, 96, 96, byte.MaxValue));

        // SabotageTimeControl
        SabotageTimeControl = BooleanOptionItem.Create(100800, "SabotageTimeControl", false, TabGroup.GameSettings, false)
           .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.GameSettings, false)
        .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.GameSettings, false)
        .SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);

        // LightsOutSpecialSettings
        LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.GameSettings, false)
          .SetColor(new Color32(243, 96, 96, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.GameSettings, false)
        .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.GameSettings, false)
        .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.GameSettings, false)
        .SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);


        //禁用相关设定
        TextOptionItem.Create(66_123_120, "MenuTitle.Disable", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        DisableVanillaRoles = BooleanOptionItem.Create(6090069, "DisableVanillaRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableHiddenRoles = BooleanOptionItem.Create(6090070, "DisableHiddenRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableSunnyboy = BooleanOptionItem.Create(6090070_1, "DisableSunnyboy", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableHiddenRoles)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableBard = BooleanOptionItem.Create(6090070_2, "DisableBard", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableHiddenRoles)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableSaboteur = BooleanOptionItem.Create(6090070_3, "DisableSaboteur", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableHiddenRoles)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableTaskWin = BooleanOptionItem.Create(66_900_001, "DisableTaskWin", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        // 禁用任务

        DisableMeeting = BooleanOptionItem.Create(66_900_002, "DisableMeeting", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableSabotage = BooleanOptionItem.Create(66_900_004, "DisableSabotage", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableCloseDoor = BooleanOptionItem.Create(66_900_003, "DisableCloseDoor", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetParent(DisableSabotage)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        //禁用设备
        DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.GameSettings, false)
        .SetParent(DisableSkeldDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.GameSettings, false)
        .SetParent(DisableSkeldDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.GameSettings, false)
        .SetParent(DisableMiraHQDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.GameSettings, false)
        .SetParent(DisableMiraHQDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.GameSettings, false)
        .SetParent(DisablePolusDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.GameSettings, false)
        .SetParent(DisablePolusDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.GameSettings, false)
        .SetParent(DisablePolusDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.GameSettings, false)
        .SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.GameSettings, false)
        .SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.GameSettings, false)
        .SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.GameSettings, false)
        .SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.GameSettings, false)
        .SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);

        //Disable Short Tasks
        DisableShortTasks = BooleanOptionItem.Create(101000, "DisableShortTasks", false, TabGroup.TaskSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableCleanVent = BooleanOptionItem.Create(101001, "DisableCleanVent", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableCalibrateDistributor = BooleanOptionItem.Create(101002, "DisableCalibrateDistributor", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableChartCourse = BooleanOptionItem.Create(101003, "DisableChartCourse", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableStabilizeSteering = BooleanOptionItem.Create(101004, "DisableStabilizeSteering", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableCleanO2Filter = BooleanOptionItem.Create(101005, "DisableCleanO2Filter", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableUnlockManifolds = BooleanOptionItem.Create(101006, "DisableUnlockManifolds", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisablePrimeShields = BooleanOptionItem.Create(101007, "DisablePrimeShields", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableMeasureWeather = BooleanOptionItem.Create(101008, "DisableMeasureWeather", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableBuyBeverage = BooleanOptionItem.Create(101009, "DisableBuyBeverage", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableAssembleArtifact = BooleanOptionItem.Create(101010, "DisableAssembleArtifact", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableSortSamples = BooleanOptionItem.Create(101011, "DisableSortSamples", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableProcessData = BooleanOptionItem.Create(101012, "DisableProcessData", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableRunDiagnostics = BooleanOptionItem.Create(101013, "DisableRunDiagnostics", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableRepairDrill = BooleanOptionItem.Create(101014, "DisableRepairDrill", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableAlignTelescope = BooleanOptionItem.Create(101015, "DisableAlignTelescope", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableRecordTemperature = BooleanOptionItem.Create(101016, "DisableRecordTemperature", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableFillCanisters = BooleanOptionItem.Create(101017, "DisableFillCanisters", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableMonitorTree = BooleanOptionItem.Create(101018, "DisableMonitorTree", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableStoreArtifacts = BooleanOptionItem.Create(101019, "DisableStoreArtifacts", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisablePutAwayPistols = BooleanOptionItem.Create(101020, "DisablePutAwayPistols", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisablePutAwayRifles = BooleanOptionItem.Create(101021, "DisablePutAwayRifles", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableMakeBurger = BooleanOptionItem.Create(101022, "DisableMakeBurger", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableCleanToilet = BooleanOptionItem.Create(101023, "DisableCleanToilet", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableDecontaminate = BooleanOptionItem.Create(101024, "DisableDecontaminate", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableSortRecords = BooleanOptionItem.Create(101025, "DisableSortRecords", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableFixShower = BooleanOptionItem.Create(101026, "DisableFixShower", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisablePickUpTowels = BooleanOptionItem.Create(101027, "DisablePickUpTowels", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolishRuby = BooleanOptionItem.Create(101028, "DisablePolishRuby", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableDressMannequin = BooleanOptionItem.Create(101029, "DisableDressMannequin", false, TabGroup.TaskSettings, false)
        .SetParent(DisableShortTasks)
            .SetGameMode(CustomGameMode.Standard);

        //Disable Common Tasks
        DisableCommonTasks = BooleanOptionItem.Create(102000, "DisableCommonTasks", false, TabGroup.TaskSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableSwipeCard = BooleanOptionItem.Create(102301, "DisableSwipeCardTask", false, TabGroup.TaskSettings, false)
        .SetParent(DisableCommonTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableFixWiring = BooleanOptionItem.Create(102001, "DisableFixWiring", false, TabGroup.TaskSettings, false)
        .SetParent(DisableCommonTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableEnterIdCode = BooleanOptionItem.Create(102002, "DisableEnterIdCode", false, TabGroup.TaskSettings, false)
        .SetParent(DisableCommonTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableInsertKeys = BooleanOptionItem.Create(102003, "DisableInsertKeys", false, TabGroup.TaskSettings, false)
        .SetParent(DisableCommonTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableScanBoardingPass = BooleanOptionItem.Create(102004, "DisableScanBoardingPass", false, TabGroup.TaskSettings, false)
        .SetParent(DisableCommonTasks)
            .SetGameMode(CustomGameMode.Standard);

        //Disable Long Tasks
        DisableLongTasks = BooleanOptionItem.Create(103000, "DisableLongTasks", false, TabGroup.TaskSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableSubmitScan = BooleanOptionItem.Create(103302, "DisableSubmitScanTask", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableUnlockSafe = BooleanOptionItem.Create(103303, "DisableUnlockSafeTask", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableStartReactor = BooleanOptionItem.Create(103305, "DisableStartReactorTask", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableResetBreaker = BooleanOptionItem.Create(103306, "DisableResetBreakerTask", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableAlignEngineOutput = BooleanOptionItem.Create(103001, "DisableAlignEngineOutput", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableInspectSample = BooleanOptionItem.Create(103002, "DisableInspectSample", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableEmptyChute = BooleanOptionItem.Create(103003, "DisableEmptyChute", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableClearAsteroids = BooleanOptionItem.Create(103004, "DisableClearAsteroids", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableWaterPlants = BooleanOptionItem.Create(103005, "DisableWaterPlants", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableOpenWaterways = BooleanOptionItem.Create(103006, "DisableOpenWaterways", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableReplaceWaterJug = BooleanOptionItem.Create(103007, "DisableReplaceWaterJug", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableRebootWifi = BooleanOptionItem.Create(103008, "DisableRebootWifi", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevelopPhotos = BooleanOptionItem.Create(103009, "DisableDevelopPhotos", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableRewindTapes = BooleanOptionItem.Create(103010, "DisableRewindTapes", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableStartFans = BooleanOptionItem.Create(103011, "DisableStartFans", false, TabGroup.TaskSettings, false)
        .SetParent(DisableLongTasks)
            .SetGameMode(CustomGameMode.Standard);

        //Disable Divert Power, Weather Nodes and etc. situational Tasks
        DisableOtherTasks = BooleanOptionItem.Create(104000, "DisableOtherTasks", false, TabGroup.TaskSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(239, 89, 175, byte.MaxValue));
        DisableUploadData = BooleanOptionItem.Create(104304, "DisableUploadDataTask", false, TabGroup.TaskSettings, false)
        .SetParent(DisableOtherTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableEmptyGarbage = BooleanOptionItem.Create(104001, "DisableEmptyGarbage", false, TabGroup.TaskSettings, false)
        .SetParent(DisableOtherTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableFuelEngines = BooleanOptionItem.Create(104002, "DisableFuelEngines", false, TabGroup.TaskSettings, false)
        .SetParent(DisableOtherTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableDivertPower = BooleanOptionItem.Create(104003, "DisableDivertPower", false, TabGroup.TaskSettings, false)
        .SetParent(DisableOtherTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableActivateWeatherNodes = BooleanOptionItem.Create(104004, "DisableActivateWeatherNodes", false, TabGroup.TaskSettings, false)
        .SetParent(DisableOtherTasks)
            .SetGameMode(CustomGameMode.Standard);



        //会议相关设定
        TextOptionItem.Create(66_123_122, "MenuTitle.Meeting", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));

        // 会议限制次数
        SyncButtonMode = BooleanOptionItem.Create(100200, "SyncButtonMode", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        SyncedButtonCount = IntegerOptionItem.Create(100201, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.GameSettings, false)
        .SetParent(SyncButtonMode)
            .SetValueFormat(OptionFormat.Times)
            .SetGameMode(CustomGameMode.Standard);

        // 全员存活时的会议时间
        AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.GameSettings, false)
        .SetParent(AllAliveMeeting)
            .SetValueFormat(OptionFormat.Seconds);

        // 附加紧急会议
        AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.GameSettings, false)
        .SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);
        AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.GameSettings, false)
        .SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Seconds);

        // 投票相关设定
        VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.GameSettings, false)
        .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.GameSettings, false)
        .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.GameSettings, false)
        .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.GameSettings, false)
        .SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.GameSettings, false)
        .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.GameSettings, false)
        .SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);


        // 其它设定
        TextOptionItem.Create(66_123_123, "MenuTitle.Other", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 梯子摔死
        LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.GameSettings, false)
        .SetParent(LadderDeath)
            .SetGameMode(CustomGameMode.Standard);

        // 修正首刀时间
        FixFirstKillCooldown = BooleanOptionItem.Create(50_900_667, "FixFirstKillCooldown", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 首刀保护
        ShieldPersonDiedFirst = BooleanOptionItem.Create(50_900_676, "ShieldPersonDiedFirst", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 杀戮闪烁持续
        KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.GameSettings, false)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);

        // 幽灵相关设定
        TextOptionItem.Create(66_123_124, "MenuTitle.Ghost", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));

        // 幽灵设置
        GhostIgnoreTasks = BooleanOptionItem.Create(900_012, "GhostIgnoreTasks", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherRoles = BooleanOptionItem.Create(900_010, "GhostCanSeeOtherRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherVotes = BooleanOptionItem.Create(900_011, "GhostCanSeeOtherVotes", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
             .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeDeathReason = BooleanOptionItem.Create(900_014, "GhostCanSeeDeathReason", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(217, 218, 255, byte.MaxValue));

        #endregion 

        IsLoaded = true;
    }

    public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool zeroOne = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), zeroOne ? ratesZeroOne : ratesToggle, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var role = CustomRoles.Lovers;
        var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        LoverSpawnChances = IntegerOptionItem.Create(id + 2, "LoverSpawnChances", new(0, 100, 5), 50, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(customGameMode);

        LoverKnowRoles = BooleanOptionItem.Create(id + 4, "LoverKnowRoles", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        LoverSuicide = BooleanOptionItem.Create(id + 3, "LoverSuicide", true, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false)
        .SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    private static void SetupAdtRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool canSetNum = false, TabGroup tab = TabGroup.Addons, bool canSetChance = true)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, canSetNum ? 10 : 1, 1), 1, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetHidden(!canSetNum)
            .SetGameMode(customGameMode);

        var spawnRateOption = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), canSetChance ? 65 : 100, tab, false)
        .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetHidden(!canSetChance)
            .SetGameMode(customGameMode) as IntegerOptionItem;

        CustomAdtRoleSpawnRate.Add(role, spawnRateOption);
        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }

    public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard, bool zeroOne = false)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), zeroOne ? ratesZeroOne : ratesToggle, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;
        // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false)
        .SetParent(spawnOption)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public class OverrideTasksData
    {
        public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem doOverride;
        public OptionItem assignCommonTasks;
        public OptionItem numLongTasks;
        public OptionItem numShortTasks;

        public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
        {
            IdStart = idStart;
            Role = role;
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)) } };
            doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false)
        .SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.None);
            doOverride.ReplacementDictionary = replacementDic;
            assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false)
        .SetParent(doOverride)
                .SetValueFormat(OptionFormat.None);
            assignCommonTasks.ReplacementDictionary = replacementDic;
            numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false)
        .SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numLongTasks.ReplacementDictionary = replacementDic;
            numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false)
        .SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numShortTasks.ReplacementDictionary = replacementDic;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
        }
        public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new OverrideTasksData(idStart, tab, role);
        }
    }
}