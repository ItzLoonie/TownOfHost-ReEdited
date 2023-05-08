using AmongUs.GameOptions;
using System.Linq;

namespace TOHE;

internal static class CustomRolesHelper
{
    public static CustomRoles GetVNRole(this CustomRoles role) // ��Ӧԭ��ְҵ
    {
        return role.IsVanilla()
            ? role
            : role switch
            {
                CustomRoles.Sniper => CustomRoles.Shapeshifter,
                CustomRoles.Jester => CustomRoles.Crewmate,
                CustomRoles.Mayor => Options.MayorHasPortableButton.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate,
                CustomRoles.Opportunist => CustomRoles.Crewmate,
                CustomRoles.Snitch => CustomRoles.Crewmate,
                CustomRoles.Marshall => CustomRoles.Crewmate,
                CustomRoles.SabotageMaster => CustomRoles.Engineer,
                CustomRoles.Mafia => CustomRoles.Impostor,
                CustomRoles.Terrorist => CustomRoles.Engineer,
                CustomRoles.Executioner => CustomRoles.Crewmate,
                CustomRoles.Vampire => CustomRoles.Impostor,
                CustomRoles.Poisoner => CustomRoles.Impostor,
                CustomRoles.BountyHunter => CustomRoles.Shapeshifter,
                CustomRoles.Witch => CustomRoles.Impostor,
                CustomRoles.ShapeMaster => CustomRoles.Shapeshifter,
                CustomRoles.ShapeshifterTOHE => CustomRoles.Shapeshifter,
                CustomRoles.Wildling => CustomRoles.Shapeshifter,
                CustomRoles.Warlock => CustomRoles.Shapeshifter,
                CustomRoles.SerialKiller => CustomRoles.Shapeshifter,
                CustomRoles.FireWorks => CustomRoles.Shapeshifter,
                CustomRoles.SpeedBooster => CustomRoles.Crewmate,
                CustomRoles.Dictator => CustomRoles.Crewmate,
                CustomRoles.Mare => CustomRoles.Impostor,
                CustomRoles.Doctor => CustomRoles.Scientist,
                CustomRoles.ScientistTOHE => CustomRoles.Scientist,
                CustomRoles.Puppeteer => CustomRoles.Impostor,
                CustomRoles.NWitch => CustomRoles.Impostor,
                CustomRoles.TimeThief => CustomRoles.Impostor,
                CustomRoles.EvilTracker => CustomRoles.Shapeshifter,
                CustomRoles.Paranoia => CustomRoles.Engineer,
                CustomRoles.EngineerTOHE => CustomRoles.Engineer,
                CustomRoles.Miner => CustomRoles.Shapeshifter,
                CustomRoles.Psychic => CustomRoles.Crewmate,
                CustomRoles.Needy => CustomRoles.Crewmate,
                CustomRoles.SuperStar => CustomRoles.Crewmate,
                CustomRoles.Hacker => CustomRoles.Shapeshifter,
                CustomRoles.Assassin => CustomRoles.Shapeshifter,
                CustomRoles.Luckey => CustomRoles.Crewmate,
                CustomRoles.CyberStar => CustomRoles.Crewmate,
                CustomRoles.Escapee => CustomRoles.Shapeshifter,
                CustomRoles.NiceGuesser => CustomRoles.Crewmate,
                CustomRoles.EvilGuesser => CustomRoles.Impostor,
                CustomRoles.Detective => CustomRoles.Crewmate,
                CustomRoles.Minimalism => CustomRoles.Impostor,
                CustomRoles.God => CustomRoles.Crewmate,
                CustomRoles.GuardianAngelTOHE => CustomRoles.GuardianAngel,
                CustomRoles.Zombie => CustomRoles.Impostor,
                CustomRoles.Mario => CustomRoles.Engineer,
                CustomRoles.AntiAdminer => CustomRoles.Impostor,
                CustomRoles.Sans => CustomRoles.Impostor,
                CustomRoles.Bomber => CustomRoles.Shapeshifter,
                CustomRoles.BoobyTrap => CustomRoles.Impostor,
                CustomRoles.Scavenger => CustomRoles.Impostor,
                CustomRoles.Transporter => CustomRoles.Crewmate,
                CustomRoles.Veteran => CustomRoles.Engineer,
                CustomRoles.Capitalism => CustomRoles.Impostor,
                CustomRoles.Bodyguard => CustomRoles.Crewmate,
                CustomRoles.Grenadier => CustomRoles.Engineer,
                CustomRoles.Gangster => CustomRoles.Impostor,
                CustomRoles.Cleaner => CustomRoles.Impostor,
                CustomRoles.Konan => CustomRoles.Crewmate,
                CustomRoles.Divinator => CustomRoles.Crewmate,
                CustomRoles.BallLightning => CustomRoles.Impostor,
                CustomRoles.Greedier => CustomRoles.Impostor,
                CustomRoles.Workaholic => CustomRoles.Engineer,
                CustomRoles.CursedWolf => CustomRoles.Impostor,
                CustomRoles.Collector => CustomRoles.Crewmate,
                CustomRoles.Glitch => CustomRoles.Crewmate,
                CustomRoles.ImperiusCurse => CustomRoles.Shapeshifter,
                CustomRoles.QuickShooter => CustomRoles.Shapeshifter,
                CustomRoles.Concealer => CustomRoles.Shapeshifter,
                CustomRoles.Eraser => CustomRoles.Impostor,
                CustomRoles.OverKiller => CustomRoles.Impostor,
                CustomRoles.Hangman => CustomRoles.Shapeshifter,
                CustomRoles.Sunnyboy => CustomRoles.Scientist,
                CustomRoles.Judge => CustomRoles.Crewmate,
                CustomRoles.Mortician => CustomRoles.Crewmate,
                CustomRoles.Mediumshiper => CustomRoles.Crewmate,
                CustomRoles.Bard => CustomRoles.Impostor,
                CustomRoles.Swooper => CustomRoles.Impostor,
                CustomRoles.Crewpostor => CustomRoles.Crewmate,
                CustomRoles.Observer => CustomRoles.Crewmate,
                _ => role.IsImpostor() ? CustomRoles.Impostor : CustomRoles.Crewmate,
            };
    }
    public static RoleTypes GetDYRole(this CustomRoles role) // ��Ӧԭ��ְҵ����ְҵ��
    {
        return role switch
        {
            //SoloKombat
            CustomRoles.KB_Normal => RoleTypes.Impostor,
            //Standard
            CustomRoles.Sheriff => RoleTypes.Impostor,
            CustomRoles.Arsonist => RoleTypes.Impostor,
            CustomRoles.Jackal => RoleTypes.Impostor,
       //     CustomRoles.Sidekick => RoleTypes.Impostor,
            CustomRoles.SwordsMan => RoleTypes.Impostor,
            CustomRoles.Innocent => RoleTypes.Impostor,
            CustomRoles.Pelican => RoleTypes.Impostor,
            CustomRoles.Counterfeiter => RoleTypes.Impostor,
            CustomRoles.Revolutionist => RoleTypes.Impostor,
            CustomRoles.FFF => RoleTypes.Impostor,
            CustomRoles.Medicaler => RoleTypes.Impostor,
            CustomRoles.Gamer => RoleTypes.Impostor,
            CustomRoles.DarkHide => RoleTypes.Impostor,
            CustomRoles.Provocateur => RoleTypes.Impostor,
            CustomRoles.BloodKnight => RoleTypes.Impostor,
            CustomRoles.Poisoner => RoleTypes.Impostor,
            CustomRoles.NWitch => RoleTypes.Impostor,
            CustomRoles.Totocalcio => RoleTypes.Impostor,
            CustomRoles.Succubus => RoleTypes.Impostor,
            _ => RoleTypes.GuardianAngel
        };
    }
    public static bool IsAdditionRole(this CustomRoles role) // �Ƿ񸽼�
    {
        return role is
            CustomRoles.Lovers or
            CustomRoles.LastImpostor or
            CustomRoles.Ntr or
            CustomRoles.Madmate or
            CustomRoles.Watcher or
            CustomRoles.Flashman or
            CustomRoles.Lighter or
            CustomRoles.Seer or
            CustomRoles.Bait or
            CustomRoles.Sidekick or
            CustomRoles.Trapper or
            CustomRoles.Brakar or
            CustomRoles.Oblivious or
            CustomRoles.Guesser or
            CustomRoles.Bewilder or
            CustomRoles.Workhorse or
            CustomRoles.Fool or
            CustomRoles.Necroview or
            CustomRoles.Avanger or
            CustomRoles.Youtuber or
            CustomRoles.Egoist or
            CustomRoles.TicketsStealer or
            CustomRoles.DualPersonality or
            CustomRoles.Mimic or
            CustomRoles.Reach or
            CustomRoles.Charmed or
            CustomRoles.Bait or
            CustomRoles.Trapper;
    }
    public static bool IsNK(this CustomRoles role) // �Ƿ��������
    {
        return role is
            CustomRoles.Jackal or
        //    CustomRoles.Sidekick or
            CustomRoles.Pelican or
            CustomRoles.NWitch or
            CustomRoles.FFF or
            CustomRoles.Gamer or
            CustomRoles.DarkHide or
            CustomRoles.Poisoner or
            CustomRoles.Provocateur or
            CustomRoles.BloodKnight;
    }
    public static bool IsNeutralKilling(this CustomRoles role) //�Ƿ�а������������򵥶�ʤ����������
    {
        return role is
            CustomRoles.Terrorist or
            CustomRoles.Arsonist or
            CustomRoles.Jackal or
       //     CustomRoles.Sidekick or
            CustomRoles.God or
            CustomRoles.Mario or
            CustomRoles.Innocent or
            CustomRoles.Pelican or
            CustomRoles.Egoist or
            CustomRoles.Gamer or
            CustomRoles.DarkHide or
            CustomRoles.Workaholic or
            CustomRoles.Collector or
            CustomRoles.Poisoner or
            CustomRoles.BloodKnight or
            CustomRoles.Succubus;
    }
    public static bool IsCK(this CustomRoles role) // �Ƿ������Ա
    {
        return role is
            CustomRoles.SwordsMan or
            CustomRoles.Sheriff;
    }
    public static bool IsImpostor(this CustomRoles role) // �Ƿ��ڹ�
    {
        return role is
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter or
            CustomRoles.ShapeshifterTOHE or
            CustomRoles.Wildling or
            CustomRoles.BountyHunter or
            CustomRoles.Vampire or
            CustomRoles.Witch or
            CustomRoles.ShapeMaster or
            CustomRoles.Zombie or
            CustomRoles.Warlock or
            CustomRoles.Assassin or
            CustomRoles.Hacker or
            CustomRoles.Miner or
            CustomRoles.Escapee or
            CustomRoles.SerialKiller or
            CustomRoles.Mare or
            CustomRoles.Puppeteer or
            CustomRoles.TimeThief or
            CustomRoles.Mafia or
            CustomRoles.Minimalism or
            CustomRoles.FireWorks or
            CustomRoles.Sniper or
            CustomRoles.EvilTracker or
            CustomRoles.EvilGuesser or
            CustomRoles.AntiAdminer or
            CustomRoles.Sans or
            CustomRoles.Bomber or
            CustomRoles.Scavenger or
            CustomRoles.BoobyTrap or
            CustomRoles.Capitalism or
            CustomRoles.Gangster or
            CustomRoles.Cleaner or
            CustomRoles.BallLightning or
            CustomRoles.Greedier or
            CustomRoles.CursedWolf or
            CustomRoles.ImperiusCurse or
            CustomRoles.QuickShooter or
            CustomRoles.Concealer or
            CustomRoles.Eraser or
            CustomRoles.OverKiller or
            CustomRoles.Hangman or
            CustomRoles.Bard or
            CustomRoles.Swooper or
            CustomRoles.Crewpostor;
    }
    public static bool IsNeutral(this CustomRoles role) // �Ƿ�����
    {
        return role is
            //SoloKombat
            CustomRoles.KB_Normal or
            //Standard
            CustomRoles.Jester or
            CustomRoles.Opportunist or
            CustomRoles.Mario or
            CustomRoles.NWitch or
            CustomRoles.Terrorist or
            CustomRoles.Executioner or
            CustomRoles.Arsonist or
            CustomRoles.Jackal or
            CustomRoles.God or
            CustomRoles.Innocent or
        //    CustomRoles.Sidekick or
            CustomRoles.Poisoner or
            CustomRoles.Pelican or
            CustomRoles.Revolutionist or
            CustomRoles.FFF or
            CustomRoles.Konan or
            CustomRoles.Gamer or
            CustomRoles.DarkHide or
            CustomRoles.Workaholic or
            CustomRoles.Collector or
            CustomRoles.Provocateur or
            CustomRoles.Sunnyboy or
            CustomRoles.BloodKnight or
            CustomRoles.Totocalcio or
            CustomRoles.Succubus;
    }
    public static bool CheckAddonConfilct(CustomRoles role, PlayerControl pc)
    {
        if (!role.IsAdditionRole()) return false;

        if (pc.Is(CustomRoles.GM) || (pc.HasSubRole() && !Options.NoLimitAddonsNum.GetBool()) || pc.Is(CustomRoles.Needy)) return false;
        if (role is CustomRoles.Lighter && (!pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Bewilder) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Bewilder && (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.Lighter) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Ntr && (pc.Is(CustomRoles.Lovers) || pc.Is(CustomRoles.FFF) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Guesser && (pc.Is(CustomRoles.EvilGuesser) || pc.Is(CustomRoles.NiceGuesser) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Madmate && !Utils.CanBeMadmate(pc)) return false;
        if (role is CustomRoles.Oblivious && (pc.Is(CustomRoles.Detective) || pc.Is(CustomRoles.Cleaner) || pc.Is(CustomRoles.Mortician) || pc.Is(CustomRoles.Mediumshiper) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Fool && (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.SabotageMaster) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Avanger && pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeAvanger.GetBool() || pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
        if (role is CustomRoles.Brakar && pc.Is(CustomRoles.Dictator) || pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
        if (role is CustomRoles.Youtuber && (!pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Madmate) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Egoist && (pc.GetCustomRole().IsNeutral() || pc.Is(CustomRoles.Madmate))) return false;
        if (role is CustomRoles.Egoist && pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeEgoist.GetBool()) return false;
        if (role is CustomRoles.Egoist && pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeEgoist.GetBool() || pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
        if (role is CustomRoles.TicketsStealer or CustomRoles.Mimic && !pc.GetCustomRole().IsImpostor()) return false;
        if (role is CustomRoles.TicketsStealer && (pc.Is(CustomRoles.Bomber) || pc.Is(CustomRoles.BoobyTrap))) return false;
        if (role is CustomRoles.Mimic && pc.Is(CustomRoles.Mafia)) return false;
        if (role is CustomRoles.Necroview && pc.Is(CustomRoles.Doctor) || pc.Is(CustomRoles.God) || pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
        if (role is CustomRoles.Bait && ((pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeBait.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeBait.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeBait.GetBool()))) return false;
        if (role is CustomRoles.Trapper && ((pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeTrapper.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeTrapper.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeTrapper.GetBool()))) return false;
        if (role is CustomRoles.DualPersonality && ((!pc.GetCustomRole().IsImpostor() && !pc.GetCustomRole().IsCrewmate()) || pc.Is(CustomRoles.Madmate))) return false;
        if (role is CustomRoles.DualPersonality && pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeDualPersonality.GetBool()) return false;
        if (role is CustomRoles.DualPersonality && pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeDualPersonality.GetBool() || pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
        if (role is CustomRoles.Seer && ((pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeSeer.GetBool()) || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeSeer.GetBool()) || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeSeer.GetBool()) || pc.Is(CustomRoles.GuardianAngelTOHE))) return false;
        if (role is CustomRoles.Sidekick && (pc.Is(CustomRoles.Madmate))) return false;
        if (role is CustomRoles.Sidekick && pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeSidekick.GetBool()) return false;
        if (role is CustomRoles.Sidekick && pc.GetCustomRole().IsCrewmate() && !Options.CrewmateCanBeSidekick.GetBool()) return false;
        if (role is CustomRoles.Sidekick && pc.GetCustomRole().IsImpostor() && !Options.ImpostorCanBeSidekick.GetBool()) return false;
        if (role is CustomRoles.Madmate && pc.Is(CustomRoles.Sidekick) || pc.Is(CustomRoles.Egoist)) return false;
        if (role is CustomRoles.Sidekick && pc.Is(CustomRoles.Madmate) || pc.Is(CustomRoles.Egoist)) return false;
        if (role is CustomRoles.Egoist && pc.Is(CustomRoles.Sidekick) || pc.Is(CustomRoles.Madmate)) return false;
        if (role is CustomRoles.Sidekick && pc.Is(CustomRoles.Jackal)) return false;
        if (role is CustomRoles.Bait && pc.Is(CustomRoles.GuardianAngelTOHE)) return false;
        if (role is CustomRoles.Trapper && pc.Is(CustomRoles.GuardianAngelTOHE)) return false;

        if (role is CustomRoles.Reach && !pc.CanUseKillButton()) return false;
        return true;
    }
    public static RoleTypes GetRoleTypes(this CustomRoles role)
        => GetVNRole(role) switch
        {
            CustomRoles.Impostor => RoleTypes.Impostor,
            CustomRoles.Scientist => RoleTypes.Scientist,
            CustomRoles.Engineer => RoleTypes.Engineer,
            CustomRoles.GuardianAngel => RoleTypes.GuardianAngel,
            CustomRoles.Shapeshifter => RoleTypes.Shapeshifter,
            CustomRoles.Crewmate => RoleTypes.Crewmate,
            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };
    public static bool IsDesyncRole(this CustomRoles role) => role.GetDYRole() != RoleTypes.GuardianAngel;
    public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostorTeam() && !role.IsNeutral();
    public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role == CustomRoles.Madmate;
    public static bool IsNNK(this CustomRoles role) => role.IsNeutral() && !role.IsNK(); // �Ƿ��޵�����
    public static bool IsVanilla(this CustomRoles role) // �Ƿ�ԭ��ְҵ
    {
        return role is
            CustomRoles.Crewmate or
            CustomRoles.Engineer or
            CustomRoles.Scientist or
            CustomRoles.GuardianAngel or
            CustomRoles.Impostor or
            CustomRoles.Shapeshifter;
    }
    public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
    {
        CustomRoleTypes type = CustomRoleTypes.Crewmate;
        if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
        if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
        if (role.IsAdditionRole()) type = CustomRoleTypes.Addon;
        return type;
    }
    public static bool RoleExist(this CustomRoles role, bool countDead = false) => Main.AllPlayerControls.Any(x => x.Is(role) && (x.IsAlive() || countDead));
    public static int GetCount(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            if (Options.DisableVanillaRoles.GetBool()) return 0;
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                _ => 0
            };
        }
        else
        {
            return Options.GetRoleCount(role);
        }
    }
    public static int GetMode(this CustomRoles role) => Options.GetRoleSpawnMode(role);
    public static float GetChance(this CustomRoles role)
    {
        if (role.IsVanilla())
        {
            var roleOpt = Main.NormalOptions.RoleOptions;
            return role switch
            {
                CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                _ => 0
            } / 100f;
        }
        else
        {
            return Options.GetRoleChance(role);
        }
    }
    public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
    public static CountTypes GetCountTypes(this CustomRoles role)
       => role switch
       {
           CustomRoles.GM => CountTypes.OutOfGame,
           CustomRoles.Jackal => CountTypes.Jackal,
           CustomRoles.Sidekick => CountTypes.Jackal,
           CustomRoles.Poisoner => CountTypes.Poisoner,
           CustomRoles.Pelican => CountTypes.Pelican,
           CustomRoles.Gamer => CountTypes.Gamer,
           CustomRoles.BloodKnight => CountTypes.BloodKnight,
           CustomRoles.Succubus => CountTypes.Succubus,
           _ => role.IsImpostorTeam() ? CountTypes.Impostor : CountTypes.Crew,
       };

    public static bool HasSubRole(this PlayerControl pc) => Main.PlayerStates[pc.PlayerId].SubRoles.Count > 0;
}
public enum CustomRoleTypes
{
    Crewmate,
    Impostor,
    Neutral,
    Addon
}
public enum CountTypes
{
    OutOfGame,
    None,
    Crew,
    Impostor,
    Jackal,
    Pelican,
    Gamer,
    BloodKnight,
    Poisoner,
    Charmed,
    Succubus,
}