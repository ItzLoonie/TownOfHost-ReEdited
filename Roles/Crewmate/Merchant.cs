using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Merchant
    {
        private static readonly int Id = 330500;
        private static readonly List<byte> playerIdList = new();

        private static Dictionary<byte, int> addonsSold = new();

        private static List<CustomRoles> addons = new();

        private static readonly List<CustomRoles> helpfulAddons = new List<CustomRoles>
        {
            CustomRoles.Watcher,
            CustomRoles.Lighter,
            CustomRoles.Seer,
            CustomRoles.Bait,
            CustomRoles.Trapper,
            CustomRoles.Brakar,
            CustomRoles.Guesser,
            CustomRoles.Knighted,
            CustomRoles.Necroview,
            CustomRoles.Onbound,
            CustomRoles.DualPersonality
        };

        private static readonly List<CustomRoles> harmfulAddons = new List<CustomRoles>
        {
            CustomRoles.Oblivious,
            CustomRoles.Bewilder,
            CustomRoles.Workhorse,
            CustomRoles.Fool,
            CustomRoles.Avanger,
            CustomRoles.Unreportable
        };

        private static readonly List<CustomRoles> neutralAddons = new List<CustomRoles>
        {
        };

        private static OptionItem OptionMaxSell;
        private static OptionItem OptionCanTargetCrew;
        private static OptionItem OptionCanTargetImpostor;
        private static OptionItem OptionCanTargetNeutral;
        private static OptionItem OptionCanSellHelpful;
        private static OptionItem OptionCanSellHarmful;
        private static OptionItem OptionCanSellNeutral;

        private static OptionItem OptionSellOnlyHarmfulToEvil;
        private static OptionItem OptionSellOnlyHelpfulToCrew;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Merchant);
            OptionMaxSell = IntegerOptionItem.Create(Id + 10, "MerchantMaxSell", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]).SetValueFormat(OptionFormat.Times);
            OptionCanTargetCrew = BooleanOptionItem.Create(Id + 11, "MerchantTargetCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanTargetImpostor = BooleanOptionItem.Create(Id + 12, "MerchantTargetImpostor", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanTargetNeutral = BooleanOptionItem.Create(Id + 13, "MerchantTargetNeutral", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanSellHelpful = BooleanOptionItem.Create(Id + 14, "MerchantSellHelpful", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanSellHarmful = BooleanOptionItem.Create(Id + 15, "MerchantSellHarmful", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionCanSellNeutral = BooleanOptionItem.Create(Id + 16, "MerchantSellNeutral", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionSellOnlyHarmfulToEvil = BooleanOptionItem.Create(Id + 17, "MerchantSellHarmfulToEvil", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionSellOnlyHelpfulToCrew = BooleanOptionItem.Create(Id + 18, "MerchantSellHelpfulToCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);

            OverrideTasksData.Create(Id + 11, TabGroup.CrewmateRoles, CustomRoles.Merchant);
        }
        public static void Init()
        {
            playerIdList.Clear();

            addons = new List<CustomRoles>();
            addonsSold = new Dictionary<byte, int>();

            if (OptionCanSellHelpful.GetBool())
            {
                addons.AddRange(helpfulAddons);
            }

            if (OptionCanSellHarmful.GetBool())
            {
                addons.AddRange(harmfulAddons);
            }

            if (OptionCanSellNeutral.GetBool())
            {
                addons.AddRange(neutralAddons);
            }
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            addonsSold.Add(playerId, 0);
        }

        public static void OnTaskFinished(PlayerControl player)
        {
            if (!player.IsAlive() || !player.Is(CustomRoles.Merchant) || (addonsSold[player.PlayerId] >= OptionMaxSell.GetInt()))
            {
                return;
            }

            var rd = IRandom.Instance;
            CustomRoles addon = addons[rd.Next(0, addons.Count)];

            List<PlayerControl> AllAlivePlayer =
                Main.AllAlivePlayerControls.Where(x =>
                    (x.PlayerId != player.PlayerId && !Pelican.IsEaten(x.PlayerId))
                    &&
                    !x.Is(addon)
                    &&
                    !CustomRolesHelper.CheckAddonConfilct(addon, x)
                    &&
                    (
                        (OptionCanTargetCrew.GetBool() && CustomRolesHelper.IsCrewmate(x.GetCustomRole())) 
                        ||
                        (OptionCanTargetImpostor.GetBool() && CustomRolesHelper.IsImpostor(x.GetCustomRole()))
                        ||
                        (OptionCanTargetNeutral.GetBool() && (CustomRolesHelper.IsNeutral(x.GetCustomRole()) || CustomRolesHelper.IsNeutralKilling(x.GetCustomRole())))
                    )
                ).ToList();

            if (AllAlivePlayer.Count >= 1)
            {
                bool helpfulAddon = IsHelpful(addon);
                bool harmfulAddon = !helpfulAddon;

                if (helpfulAddon && OptionSellOnlyHarmfulToEvil.GetBool())
                {
                    AllAlivePlayer = AllAlivePlayer.Where(a => CustomRolesHelper.IsCrewmate(a.GetCustomRole())).ToList();
                }

                if (harmfulAddon && OptionSellOnlyHelpfulToCrew.GetBool())
                {
                    AllAlivePlayer = AllAlivePlayer.Where(a =>
                        CustomRolesHelper.IsImpostor(a.GetCustomRole())
                        ||
                        CustomRolesHelper.IsNeutral(a.GetCustomRole())
                        ||
                        CustomRolesHelper.IsNeutralKilling(a.GetCustomRole())
                    ).ToList();
                }

                if (AllAlivePlayer.Count == 0)
                {
                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSellFail")));
                    return;
                }

                PlayerControl target = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];

                target.RpcSetCustomRole(addon);
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSell")));
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonDelivered")));

                Utils.NotifyRoles();

                addonsSold[player.PlayerId] += 1;
            }
        }

        private static bool IsHelpful(CustomRoles addon)
        {
            return helpfulAddons.Contains(addon);
        }
    }
}
