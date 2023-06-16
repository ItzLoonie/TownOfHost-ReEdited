﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using TOHE.Modules;
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
        private static OptionItem DazzleLimitOpt;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Dazzler);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 11, "ShapeshiftCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftDuration = FloatOptionItem.Create(Id + 12, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Seconds);
            CauseVision = FloatOptionItem.Create(Id + 13, "DazzlerCauseVision", new(0f, 5f, 0.05f), 0.65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Multiplier);
            DazzleLimitOpt = IntegerOptionItem.Create(Id + 14, "DazzlerDazzleLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Dazzler])
                .SetValueFormat(OptionFormat.Times);
        }

        public static void Init()
        {
            playerIdList = new();
            PlayersDazzled = new();
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

        public static void OnShapeshift(PlayerControl pc, PlayerControl target)
        {
            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

            if (!PlayersDazzled[pc.PlayerId].Contains(target.PlayerId) && PlayersDazzled[pc.PlayerId].Count < DazzleLimitOpt.GetInt())
            {
                PlayersDazzled[pc.PlayerId].Add(target.PlayerId);
                MarkEveryoneDirtySettings();
            }
        }

        public static void SetDazzled(PlayerControl player, IGameOptions opt)
        {
            if (PlayersDazzled.Any(a => a.Value.Contains(player.PlayerId) && Main.AllAlivePlayerControls.Any(b => b.PlayerId == a.Key)))
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, CauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, CauseVision.GetFloat());
            }
        }
    }
}
