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

        private static OptionItem OptionSellHarmfulToEvil;
        private static OptionItem OptionSellHelpfulToCrew;

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
            OptionSellHarmfulToEvil = BooleanOptionItem.Create(Id + 17, "MerchantSellHarmfulToEvil", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);
            OptionSellHelpfulToCrew = BooleanOptionItem.Create(Id + 18, "MerchantSellHelpfulToCrew", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Merchant]);

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
            List<PlayerControl> AllAlivePlayer =
                Main.AllAlivePlayerControls.Where(x => x.PlayerId != player.PlayerId && !Pelican.IsEaten(x.PlayerId)).ToList();
            if (AllAlivePlayer.Count >= 1)
            {
                PlayerControl target = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
                CustomRoles role = target.GetCustomRole();

                bool isCrewmate = CustomRolesHelper.IsCrewmate(role);
                bool isImpostor = CustomRolesHelper.IsImpostor(role);
                bool isNeutral = CustomRolesHelper.IsNeutral(role) || CustomRolesHelper.IsNeutralKilling(role);

                if ((!OptionCanTargetCrew.GetBool() && isCrewmate) ||
                    (!OptionCanTargetImpostor.GetBool() && isImpostor) ||
                    (!OptionCanTargetNeutral.GetBool() && isNeutral))
                {
                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSellFail")));
                    return;
                }

                CustomRoles addon = addons[rd.Next(0, addons.Count)];
                if (target.Is(addon) ||
                    (OptionSellHarmfulToEvil.GetBool() && (isImpostor || isNeutral) && !IsHarmful(addon)) ||
                    (OptionSellHelpfulToCrew.GetBool() && isCrewmate && IsHarmful(addon)) ||
                    CustomRolesHelper.CheckAddonConfilct(addon, target))
                {
                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSellFail")));
                    return;
                }

                target.RpcSetCustomRole(addon);
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonSell")));
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Merchant), GetString("MerchantAddonDelivered")));

                Utils.NotifyRoles();

                addonsSold[player.PlayerId] += 1;
            }
        }

        private static bool IsHarmful(CustomRoles addon)
        {
            return harmfulAddons.Contains(addon);
        }
    }
}
