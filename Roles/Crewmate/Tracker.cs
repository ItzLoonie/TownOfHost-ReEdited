namespace TOHE.Roles.Crewmate
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Hazel;
    using Il2CppSystem.Runtime.Remoting.Messaging;
    using MS.Internal.Xml.XPath;
    using UnityEngine;
    using static TOHE.Options;
    using static TOHE.Translator;
    using static UnityEngine.GraphicsBuffer;

    public static class Tracker
    {
        private static readonly int Id = 9041812;

        private static List<byte> playerIdList = new();

        private static OptionItem TrackLimitOpt;
        private static OptionItem CanSeeLastRoomInMeeting;
        public static OptionItem HideVote;

        private static Dictionary<byte, int> TrackLimit = new();
        public static Dictionary<byte, byte> TrackerTarget = new();

        public static Dictionary<byte, string> msgToSend = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Tracker);
            TrackLimitOpt = IntegerOptionItem.Create(Id + 10, "DivinatorSkillLimit", new(1, 990, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
                .SetValueFormat(OptionFormat.Times);
            CanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 11, "EvilTrackerCanSeeLastRoomInMeeting", true, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            HideVote = BooleanOptionItem.Create(Id + 12, "TrackerHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
        }
        public static void Init()
        {
            playerIdList = new();
            TrackLimit = new();
            TrackerTarget = new();
            msgToSend = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TrackLimit.TryAdd(playerId, TrackLimitOpt.GetInt());
            TrackerTarget.Add(playerId, byte.MaxValue);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static string GetTargetMark(PlayerControl seer, PlayerControl target) => TrackerTarget[seer.PlayerId] == target.PlayerId ? Utils.ColorString(seer.GetRoleColor(), "◀") : "";

        public static void OnReportDeadBody()
        {
            if (!CanSeeLastRoomInMeeting.GetBool()) return;

            foreach (var pc in playerIdList)
            {
                if (TrackerTarget[pc] == byte.MaxValue)
                {
                    continue;
                }

                string room = string.Empty;
                var targetRoom = Main.PlayerStates[TrackerTarget[pc]].LastRoom;
                if (targetRoom == null) room += GetString("FailToTrack");
                else room += GetString(targetRoom.RoomId.ToString());

                if (msgToSend.ContainsKey(pc))
                {
                    msgToSend[pc] = string.Format(Translator.GetString("TrackerLastRoomMessage"), room);
                }
                else
                {
                    msgToSend.Add(pc, string.Format(Translator.GetString("TrackerLastRoomMessage"), room));
                }

                
            }
        }

        public static void OnVote(PlayerControl player, PlayerControl target)
        {
            if (player == null || target == null) return;
            if (TrackLimit[player.PlayerId] < 1) return; 
            if (player.PlayerId == target.PlayerId) return;
            if (target.PlayerId == TrackerTarget[player.PlayerId]) return;

            TrackLimit[player.PlayerId]--;

            if (TrackerTarget[player.PlayerId] != byte.MaxValue)
            {
                TargetArrow.Remove(player.PlayerId, TrackerTarget[player.PlayerId]);
            }

            TrackerTarget[player.PlayerId] = target.PlayerId;
            TargetArrow.Add(player.PlayerId, target.PlayerId);
        }

        public static string GetTrackerArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (!seer.Is(CustomRoles.Tracker)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (GameStates.IsMeeting) return "";
            return Utils.ColorString(Color.white, TargetArrow.GetArrows(seer, TrackerTarget[seer.PlayerId]));
        }
    }
}
