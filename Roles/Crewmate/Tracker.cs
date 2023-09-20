using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate
{
    public static class Tracker
    {
        private static readonly int Id = 8300;
        private static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem TrackLimitOpt;
        private static OptionItem OptionCanSeeLastRoomInMeeting;
        public static OptionItem HideVote;
        public static OptionItem TrackerAbilityUseGainWithEachTaskCompleted;

        public static bool CanSeeLastRoomInMeeting;

        public static Dictionary<byte, float> TrackLimit = new();
        public static Dictionary<byte, List<byte>> TrackerTarget = new();

        public static Dictionary<byte, string> msgToSend = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Tracker);
            TrackLimitOpt = IntegerOptionItem.Create(Id + 10, "DivinatorSkillLimit", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
                .SetValueFormat(OptionFormat.Times);
            OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 11, "EvilTrackerCanSeeLastRoomInMeeting", false, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            HideVote = BooleanOptionItem.Create(Id + 12, "TrackerHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tracker]);
            TrackerAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Tracker])
            .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            TrackLimit = new();
            TrackerTarget = new();
            msgToSend = new();
            CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TrackLimit.Add(playerId, TrackLimitOpt.GetInt());
            TrackerTarget.Add(playerId, new List<byte>());
            IsEnable = true;
        }
        public static void SendRPC(byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTrackerTarget, SendOption.Reliable, -1);
            writer.Write(trackerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte trackerId = reader.ReadByte();
            byte targetId = reader.ReadByte();

            //if (TrackerTarget[trackerId].Contains(targetId))
            //{
            //    TargetArrow.Remove(trackerId, TrackerTarget[trackerId]);
            //}

            //TrackerTarget[trackerId] = targetId;
            //TargetArrow.Add(trackerId, targetId);

        }
        public static string GetTargetMark(PlayerControl seer, PlayerControl target) => !(seer == null || target == null) && TrackerTarget.ContainsKey(seer.PlayerId) && TrackerTarget[seer.PlayerId].Contains(target.PlayerId) ? Utils.ColorString(seer.GetRoleColor(), "◀") : "";

        public static void OnReportDeadBody(GameData.PlayerInfo target)
        {
            if (!OptionCanSeeLastRoomInMeeting.GetBool()) return;

            //foreach (var pc in playerIdList)
            //{
            //    string room = string.Empty;
            //    var targetRoom = string.Empty;

            //    foreach (var targetId in TrackerTarget.Values)
            //    {
            //        targetRoom = ;
            //    }
    
            //    if (targetRoom == null) room += GetString("FailToTrack");
            //    else room += GetString(targetRoom.RoomId.ToString());

            //    if (msgToSend.ContainsKey(pc))
            //    {
            //        msgToSend[pc] = string.Format(GetString("TrackerLastRoomMessage"), room);
            //    }
            //    else
            //    {
            //        msgToSend.Add(pc, string.Format(GetString("TrackerLastRoomMessage"), room));
            //    }
            //}
        }

        public static void OnVote(PlayerControl player, PlayerControl target)
        {
            if (player == null || target == null) return;
            if (TrackLimit[player.PlayerId] < 1) return;
            if (player.PlayerId == target.PlayerId) return;
            if (TrackerTarget[player.PlayerId].Contains(target.PlayerId)) return;

            TrackLimit[player.PlayerId]--;

            TrackerTarget[player.PlayerId].Add(target.PlayerId);
            //TrackerTarget[player.PlayerId] = target.PlayerId;
            TargetArrow.Add(player.PlayerId, target.PlayerId);

            SendRPC(player.PlayerId, target.PlayerId);
        }

        public static string GetTrackerArrow(PlayerControl seer, PlayerControl target)
        {
            if (seer == null || target == null) return "";
            if (!seer.Is(CustomRoles.Tracker)) return "";
            if (GameStates.IsMeeting) return "";
            if (!TrackerTarget.ContainsKey(seer.PlayerId)) return "";
            if (!TrackerTarget[seer.PlayerId].Contains(target.PlayerId)) return "";

            var arrows = string.Empty;

            //var targetData = Utils.GetPlayerById(trackTarget);

            var arrow = TargetArrow.GetArrows(seer, target.PlayerId);
            arrows += Utils.ColorString(Palette.PlayerColors[target.Data.DefaultOutfit.ColorId], arrow);
            
            //foreach (var targetList in TrackerTarget.Values)
            //{
            //    foreach (var trackTarget in targetList)
            //    {
            //        var targetData = Utils.GetPlayerById(trackTarget);

            //        var arrow = TargetArrow.GetArrows(seer, trackTarget);
            //        arrows += Utils.ColorString(Palette.PlayerColors[targetData.Data.DefaultOutfit.ColorId], arrow);
            //    }
            //}

            return arrows;

            //return Utils.ColorString(Color.white, TargetArrow.GetArrows(seer, TrackerTarget[seer.PlayerId])); //Palette.PlayerColors[TrackerTarget[seer.PlayerId]]
        }

        public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
            => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)
                && TrackerTarget[seer.PlayerId].Contains(target.PlayerId)
                && target.IsAlive();

        public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
        {
            string text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), TargetArrow.GetArrows(seer, target.PlayerId));
            var room = Main.PlayerStates[target.PlayerId].LastRoom;
            if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
            else text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Tracker), "@" + GetString(room.RoomId.ToString()));
            return text;
        }
    }
}