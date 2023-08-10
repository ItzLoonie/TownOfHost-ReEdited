using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor
{
    public static class Pitfall
    {
        private static readonly int Id = 8050;

        public static List<byte> playerIdList = new();

        private static List<PitfallTrap> Traps = new();
        private static Dictionary<byte, long> PlayersTrapped = new();

        private static OptionItem ShapeshiftCooldown;
        public static OptionItem TrapMaxPlayerCount;
        public static OptionItem TrapDuration;
        private static OptionItem TrapRadius;
        private static OptionItem TrapFreezeTime;
        private static OptionItem TrapCauseVision;
        private static OptionItem TrapCauseVisionTime;

        private static float DefaultSpeed = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Pitfall);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 10, "PitfallTrapCooldown", new(1f, 999f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
            TrapMaxPlayerCount = FloatOptionItem.Create(Id + 11, "PitfallTrapMaxPlayerCount", new(1f, 15f, 1f), 3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Times);
            TrapDuration = FloatOptionItem.Create(Id + 12, "PitfallTrapDuration", new(5f, 999f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
            TrapRadius = FloatOptionItem.Create(Id + 13, "PitfallTrapRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Multiplier);
            TrapFreezeTime = FloatOptionItem.Create(Id + 14, "PitfallTrapFreezeTime", new(0f, 30f, 1f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
            TrapCauseVision = FloatOptionItem.Create(Id + 15, "PitfallTrapCauseVision", new(0f, 5f, 0.05f), 0.2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Multiplier);
            TrapCauseVisionTime = FloatOptionItem.Create(Id + 16, "PitfallTrapCauseVisionTime", new(0f, 45f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pitfall])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = 1f;
        }

        public static void Init()
        {
            playerIdList = new();
            Traps = new();
            PlayersTrapped = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            DefaultSpeed = Main.AllPlayerSpeed[playerId];
        }
        public static bool IsEnable => playerIdList.Any();

        public static void OnShapeshift(PlayerControl shapeshifter)
        {
            var position = shapeshifter.GetTruePosition();

            var trap = Traps.FirstOrDefault(a => a.PitfallPlayerId == shapeshifter.PlayerId);
            if (trap == null)
            {
                Traps.Add(new PitfallTrap
                {
                    PitfallPlayerId = shapeshifter.PlayerId,
                    Location = position,
                    PlayersTrappedCount = 0,
                    Timer = 0
                });
            }
            else
            {
                trap.Location = position;
                trap.PlayersTrappedCount = 0;
                trap.Timer = 0;
            }
        }

        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!GameStates.IsInTask || !IsEnable || Pelican.IsEaten(player.PlayerId) || player.Data.IsDead) return;

            if (player.GetCustomRole().IsImpostor())
            {
                var trap = Traps.FirstOrDefault(a => a.PitfallPlayerId == player.PlayerId && a.IsActive);
                if (trap != null)
                {
                    trap.Timer += Time.fixedDeltaTime;
                }

                return;
            }
            else if (PlayersTrapped.ContainsKey(player.PlayerId) && PlayersTrapped[player.PlayerId] < Utils.GetTimeStamp())
            {
                PlayersTrapped.Remove(player.PlayerId);
                player.MarkDirtySettings();
            }

            var position = player.GetTruePosition();

            foreach (var trap in Traps.Where(a => a.IsActive))
            {
                var dis = Vector2.Distance(trap.Location, position);
                if (dis > TrapRadius.GetFloat()) continue;

                if (TrapFreezeTime.GetFloat() > 0)
                {
                    TrapPlayer(player);
                }

                if (TrapCauseVisionTime.GetFloat() > 0)
                {
                    long time = Utils.GetTimeStamp() + (long)TrapCauseVisionTime.GetFloat();
                    PlayersTrapped.Add(player.PlayerId, time);
                }
                    
                trap.PlayersTrappedCount += 1;

                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pitfall), GetString("PitfallTrap")));
            }
        }

        public static void ReduceVision(IGameOptions opt, PlayerControl target)
        {
            if (PlayersTrapped.ContainsKey(target.PlayerId))
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, TrapCauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, TrapCauseVision.GetFloat());
            }
        }

        private static void TrapPlayer(PlayerControl player)
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
            player.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
                ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                player.MarkDirtySettings();
            }, TrapFreezeTime.GetFloat(), "PitfallTrapPlayer");
        }
    }

    public class PitfallTrap
    {
        public int PitfallPlayerId;
        public Vector2 Location;
        public float Timer;
        public int PlayersTrappedCount;
        public bool IsActive
        {
            get
            {
                return Timer <= Pitfall.TrapDuration.GetFloat() && PlayersTrappedCount < Pitfall.TrapMaxPlayerCount.GetInt();
            }
        }
    }
}
