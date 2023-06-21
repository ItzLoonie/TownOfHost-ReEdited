using System.Collections.Generic;
using System.Linq;
using Hazel;
using MS.Internal.Xml.XPath;
using UnityEngine;
using static Il2CppSystem.Globalization.CultureInfo;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    public static class Amor
    {
        private static readonly int Id = 7772269;
        private static List<byte> playerIdList = new();
        private static int MatchmakeLimit = new();
        public static List<PlayerControl> Lovers = new();

        private static OptionItem MatchmakeCooldown;
        private static OptionItem MatchmakeMax;
        public static OptionItem KnowTargetRole;
        public static OptionItem LoversSuicide;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Amor, 1, zeroOne: false);
            MatchmakeCooldown = FloatOptionItem.Create(Id + 10, "AmorMatchmakeCooldown", new(0f, 990f, 2.5f), 30f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amor])
                .SetValueFormat(OptionFormat.Seconds);
            MatchmakeMax = IntegerOptionItem.Create(Id + 11, "AmorMatchmakeMax", new(2, 4, 1), 2, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amor])
                .SetValueFormat(OptionFormat.Times);
            KnowTargetRole = BooleanOptionItem.Create(Id + 12, "AmorKnowLoverRole", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amor]);
            LoversSuicide = BooleanOptionItem.Create(Id + 13, "AmorLoversSuicide", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amor]);
        }

        public static void Init()
        {
            playerIdList = new();
            Lovers = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            MatchmakeLimit = MatchmakeMax.GetInt();
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static byte PlayerId => playerIdList.First();

        public static void SetCooldown(byte id) => Main.AllPlayerKillCooldown[id] = MatchmakeCooldown.GetFloat();

        private static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAmorMatchmakeLimit, SendOption.Reliable, -1);
            writer.Write(MatchmakeLimit);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            MatchmakeLimit = reader.ReadInt32();
        }

        public static void OnCheckMurder(PlayerControl player, PlayerControl target)
        {
            if (Lovers.Count == MatchmakeMax.GetInt())
            {
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amor), GetString("AmorLoverPairExists")));
                return;
            }

            if (target.Is(CustomRoles.Lovers))
            {
                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amor), GetString("AmorPlayerAlreadyInLove")));
                return;
            }

            if (Lovers.Any(a => a.PlayerId == target.PlayerId))
            {
                return;
            }

            player.SetKillCooldown();

            Lovers.Add(target);
            Lovers = Lovers.Where(x => x.IsAlive()).ToList();

            MatchmakeLimit = MatchmakeMax.GetInt() - Lovers.Count;
            SendRPC();

            if (Lovers.Count == MatchmakeMax.GetInt())
            {
                foreach (var lover in Lovers)
                {
                    lover.RpcSetCustomRole(CustomRoles.Lovers);
                    lover.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amor), GetString("AmorPlayerFellInLove")));
                }

                player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amor), GetString("AmorMadePlayerFallInLove")));
                Utils.NotifyRoles();
            }
        }

        public static bool KnowRole(PlayerControl player, PlayerControl target)
        {
            if (Lovers.Any(a => a.PlayerId == player.PlayerId) && target.Is(CustomRoles.Amor)) return true;
            if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Amor) && Lovers.Any(a => a.PlayerId == target.PlayerId)) return true;
            return false;
        }

        public static string GetMatchmakeLimit() => Utils.ColorString(MatchmakeLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Amor) : Color.gray, $"({MatchmakeLimit})");

        public static string GetLoversMark(PlayerControl seer, PlayerControl target) => Lovers.Any(a => a.PlayerId == target.PlayerId) ? Utils.ColorString(seer.GetRoleColor(), "♡") : "";

        public static void CheckLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (!IsEnable || !LoversSuicide.GetBool() || Lovers.Count < 2)
            {
                return;
            }

            var player = Lovers.FirstOrDefault(a => a.PlayerId == deathId);
            if (player == null || (!player.Data.IsDead && !isExiled))
            {
                return;
            }

            foreach (var lover in Lovers.Where(a => a.PlayerId != deathId))
            {
                if (!lover.Data.IsDead)
                {
                    Main.PlayerStates[lover.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                    if (isExiled)
                        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, lover.PlayerId);
                    else
                        lover.RpcMurderPlayerV3(lover);
                }
            }

            Lovers.Clear();
            MatchmakeLimit = MatchmakeMax.GetInt();
            SendRPC();
        }

        public static bool CheckAmorLoverLeave(PlayerControl leaver)
        {
            if (Lovers.Contains(leaver))
            {
                Lovers.Remove(leaver);
                if (Lovers.Count < 1)
                {
                    Main.PlayerStates[Amor.Lovers[0].PlayerId].RemoveSubRole(CustomRoles.Lovers);
                }

                return true;
            }

            return false;
        }

        public static bool IsLoverPair(PlayerControl seer, PlayerControl target)
        {
            return Amor.AreAmorLovers(seer, target) || Amor.AreNaturalLovers(seer, target);
        }

        public static bool AreAmorLovers(PlayerControl seer, PlayerControl target)
        {
            return
                Lovers.Any(a => a.PlayerId == seer.PlayerId) &&
                Lovers.Any(a => a.PlayerId == target.PlayerId);
        }

        public static bool AreNaturalLovers(PlayerControl seer, PlayerControl target)
        {
            return
                Main.LoversPlayers.Any(a => a.PlayerId == seer.PlayerId) &&
                Main.LoversPlayers.Any(a => a.PlayerId == target.PlayerId);
        }
    }
}
