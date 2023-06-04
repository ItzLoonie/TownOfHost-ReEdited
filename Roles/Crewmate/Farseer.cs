using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate
{
    public static class Farseer
    {
        private static readonly int Id = 7052269;

        public static OptionItem FarseerCooldown;
        public static OptionItem FarseerRevealTime;

        public static OptionItem Vision; 

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Farseer);
            FarseerCooldown = FloatOptionItem.Create(Id + 10, "FarseerRevealCooldown", new(0f, 990f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Seconds);
            FarseerRevealTime = FloatOptionItem.Create(Id + 11, "FarseerRevealTime", new(0f, 60f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Seconds);
            Vision = FloatOptionItem.Create(Id + 12, "FarseerVision", new(0f, 5f, 0.05f), 0.6f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Multiplier);

        }

        public static void SetCooldown(byte id) => Main.AllPlayerKillCooldown[id] = FarseerCooldown.GetFloat();
    }
}
