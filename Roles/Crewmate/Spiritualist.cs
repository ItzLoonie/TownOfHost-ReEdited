using System.Collections.Generic;
using System.Linq;
using Hazel;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Spiritualist
    {
        private static readonly int Id = 9123412;

        private static List<byte> playerIdList = new();

        public static OptionItem ShowGhostArrowEverySeconds;
        public static OptionItem ShowGhostArrowForSeconds;

        private static Dictionary<byte, long> ShowGhostArrowUntil = new();
        private static Dictionary<byte, long> LastGhostArrowShowTime = new();

        public static byte SpiritualistTarget = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Spiritualist);
            ShowGhostArrowEverySeconds = FloatOptionItem.Create(Id + 10, "SpiritualistShowGhostArrowEverySeconds", new(1f, 60f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritualist])
                .SetValueFormat(OptionFormat.Seconds);
            ShowGhostArrowForSeconds = FloatOptionItem.Create(Id + 11, "SpiritualistShowGhostArrowForSeconds", new(1f, 60f, 1f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritualist])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            SpiritualistTarget = new();
            LastGhostArrowShowTime = new();
            ShowGhostArrowUntil = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            SpiritualistTarget = byte.MaxValue;
            LastGhostArrowShowTime.Add(playerId, 0);
            ShowGhostArrowUntil.Add(playerId, 0);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        private static bool ShowArrow(byte playerId)
        {
            long timestamp = Utils.GetTimeStamp();

            if (LastGhostArrowShowTime[playerId] == 0 || LastGhostArrowShowTime[playerId] + (long)ShowGhostArrowEverySeconds.GetFloat() <= timestamp)
            {
                LastGhostArrowShowTime[playerId] = timestamp;
                ShowGhostArrowUntil[playerId] = timestamp + (long)ShowGhostArrowForSeconds.GetFloat();
                return true;
            }
            else if (ShowGhostArrowUntil[playerId] >= timestamp)
            {
                return true;
            }

            return false;
        }

        public static void OnReportDeadBody(GameData.PlayerInfo target)
        {
            if (target == null)
            {
                return;
            }

            SpiritualistTarget = target.PlayerId;
        }

        public static void AfterMeetingTasks()
        {
            foreach (var spiritualist in playerIdList)
            {
                LastGhostArrowShowTime[spiritualist] = 0;
                ShowGhostArrowUntil[spiritualist] = 0;

                PlayerControl target = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == SpiritualistTarget);
                if (target == null)
                {
                    continue;
                }

                TargetArrow.Add(spiritualist, target.PlayerId);

                var writer = CustomRpcSender.Create("SpiritualistSendMessage", SendOption.None);
                writer.StartMessage(target.GetClientId());
                writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                    .Write(GetString("SpiritualistNoticeTitle"))
                    .EndRpc();
                writer.StartRpc(target.NetId, (byte)RpcCalls.SendChat)
                    .Write(GetString("SpiritualistNoticeMessage"))
                    .EndRpc();
                writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                    .Write(target.Data.PlayerName)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();
            }
        }

        public static string GetSpiritualistArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (!seer.Is(CustomRoles.Spiritualist) || !seer.IsAlive()) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (GameStates.IsMeeting) return "";
            if (SpiritualistTarget != byte.MaxValue && ShowArrow(seer.PlayerId))
            {
                return Utils.ColorString(seer.GetRoleColor(), TargetArrow.GetArrows(seer, SpiritualistTarget)); 
            }
            return "";
        }
    }
}