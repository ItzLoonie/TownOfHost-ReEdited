namespace TOHE.Roles.Crewmate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using static TOHE.Options;
    using static UnityEngine.GraphicsBuffer;
    using static TOHE.Translator;
    using UnityEngine;
    using LibCpp2IL;

    public static class Addict
    {
        private static readonly int Id = 4204204;
        private static List<byte> playerIdList = new();

        public static OptionItem VentCooldown;
        public static OptionItem TimeLimit;
        public static OptionItem ImmortalTimeAfterVent;
        public static OptionItem FreezeTimeAfterImmortal;

        private static Dictionary<byte, float> SuicideTimer = new();
        private static Dictionary<byte, float> ImmortalTimer = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Addict);
            VentCooldown = FloatOptionItem.Create(Id + 11, "VentCooldown", new(5f, 999f, 5f), 40f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
            TimeLimit = FloatOptionItem.Create(Id + 12, "SerialKillerLimit", new(5f, 999f, 5f), 45f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
            ImmortalTimeAfterVent = FloatOptionItem.Create(Id + 13, "AddictImmortalTimeAfterVent", new(5f, 999f, 5f), 10f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
            FreezeTimeAfterImmortal = FloatOptionItem.Create(Id + 14, "AddictFreezeTimeAfterImmortal", new(5f, 999f, 5f), 10f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Addict])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            SuicideTimer = new();
            ImmortalTimer = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            SuicideTimer.TryAdd(playerId, -10f);
            ImmortalTimer.TryAdd(playerId, 420f);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static bool IsImmortal(PlayerControl player) => player.Is(CustomRoles.Addict) && ImmortalTimer[player.PlayerId] <= ImmortalTimeAfterVent.GetFloat();

        public static void OnReportDeadBody()
        {
            foreach (var player in playerIdList)
            {
                SuicideTimer[player] = 0f;
                ImmortalTimer[player] = 420f;
            }
        }

        public static void FixedUpdate(PlayerControl player)
        {
            if (!GameStates.IsInTask || !IsEnable || !SuicideTimer.ContainsKey(player.PlayerId) || !player.IsAlive()) return;

            if (SuicideTimer[player.PlayerId] >= TimeLimit.GetFloat())
            {
                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                player.RpcMurderPlayerV3(player);
                SuicideTimer.Remove(player.PlayerId);
            }
            else
            { 
                SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;

                if (IsImmortal(player))
                {
                    ImmortalTimer[player.PlayerId] += Time.fixedDeltaTime;
                }
                else
                {
                    if (ImmortalTimer[player.PlayerId] != 420f && FreezeTimeAfterImmortal.GetFloat() > 0)
                    {
                        AddictGetDown(player);
                        ImmortalTimer[player.PlayerId] = 420f;
                    }
                }
            }
        }

        public static void OnEnterVent(PlayerControl pc, Vent vent) 
        {
            if (!pc.Is(CustomRoles.Addict)) return;

            SuicideTimer[pc.PlayerId] = 0f;
            ImmortalTimer[pc.PlayerId] = 0f;
        }

        private static void AddictGetDown(PlayerControl addict)
        {
            var tmpSpeed = Main.AllPlayerSpeed[addict.PlayerId];
            Main.AllPlayerSpeed[addict.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[addict.PlayerId] = false;
            addict.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[addict.PlayerId] = Main.AllPlayerSpeed[addict.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[addict.PlayerId] = true;
                addict.MarkDirtySettings();
            }, FreezeTimeAfterImmortal.GetFloat(), "AddictGetDown");
        }
    }
}
