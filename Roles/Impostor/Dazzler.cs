using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Impostor
{
    public static class Dazzler
    {
        private static readonly int Id = 914634;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, List<byte>> PlayersDazzled = new();

        private static OptionItem KillCooldown;
        private static OptionItem ShapeshiftCooldown;
        private static OptionItem ShapeshiftDuration;
        private static OptionItem CauseVision;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Dazzler);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "ShapeshiftCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Seconds);
            CauseVision = FloatOptionItem.Create(Id + 13, "DazzlerCauseVision", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Multiplier);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            PlayersDazzled.TryAdd(playerId, new List<byte>());
        }

        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

        //public static string GetDazzleLimit(byte playerId) => Utils.ColorString((HackLimit.TryGetValue(playerId, out var x) && x >= 1) ? Color.red : 
        //    Color.gray, HackLimit.TryGetValue(playerId, out var hackLimit) ? $"({hackLimit})" : "Invalid");

        public static void OnShapeshift(PlayerControl pc, PlayerControl target)
        {
            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

            if (!PlayersDazzled[pc.PlayerId].Contains(target.PlayerId))
            {
                PlayersDazzled[pc.PlayerId].Add(target.PlayerId);
                MarkEveryoneDirtySettings();
            }
        }

        public static void SetDazzled(PlayerControl player, IGameOptions opt)
        {
            if (PlayersDazzled.Any(a => a.Value.Contains(player.PlayerId)))
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, CauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, CauseVision.GetFloat());
            }
        }

        public static void OnDazzlerDied(byte dazzler, IGameOptions opt)
        {
            foreach (byte player in PlayersDazzled[dazzler])
            {
                if (PlayersDazzled.Any(a => a.Key != dazzler && a.Value.Contains(player)))
                {
                    continue;
                }

                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision); ;
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultCrewmateVision);
            }
        }
    }
}
