namespace TOHE.Roles.Crewmate
{
    using System.Collections.Generic;
    using Hazel;
    using UnityEngine;
    using static TOHE.Options;
    using static UnityEngine.GraphicsBuffer;

    public static class Bloodhound
    {
        private static readonly int Id = 9031150;
        private static List<byte> playerIdList = new();

        public static List<byte> UnreportablePlayers = new();
        public static Dictionary<byte, byte> BloodhoundTarget = new();

        public static OptionItem ArrowsPointingToDeadBody;
        public static OptionItem LeaveDeadBodyUnreportable;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Bloodhound);
            ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "BloodhoundArrowsPointingToDeadBody", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bloodhound]);
            LeaveDeadBodyUnreportable = BooleanOptionItem.Create(Id + 11, "BloodhoundLeaveDeadBodyUnreportable", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Bloodhound]);
        }
        public static void Init()
        {
            playerIdList = new();
            UnreportablePlayers = new List<byte>();
            BloodhoundTarget = new Dictionary<byte, byte>();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

        }
        public static bool IsEnable => playerIdList.Count > 0;

        private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBloodhoundArrow, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(add);
            if (add)
            {
                writer.Write(loc.x);
                writer.Write(loc.y);
                writer.Write(loc.z);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            bool add = reader.ReadBoolean();
            if (add)
                LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            else
                LocateArrow.RemoveAllTarget(playerId);
        }

        public static void Clear()
        {
            foreach (var item in BloodhoundTarget)
            {
                TargetArrow.Remove(item.Key, item.Value);
            }

            BloodhoundTarget.Clear();
        }

        public static void OnPlayerDead(PlayerControl target)
        {
            if (!ArrowsPointingToDeadBody.GetBool()) return; 

            var pos = target.GetTruePosition();
            float minDis = float.MaxValue;
            string minName = "";
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.PlayerId == target.PlayerId) continue;
                var dis = Vector2.Distance(pc.GetTruePosition(), pos);
                if (dis < minDis && dis < 1.5f)
                {
                    minDis = dis;
                    minName = pc.GetRealName();
                }
            }

            foreach (var pc in playerIdList)
            {
                var player = Utils.GetPlayerById(pc);
                if (player == null || !player.IsAlive()) continue;
                LocateArrow.Add(pc, target.transform.position);
                SendRPC(pc, true, target.transform.position);
            }
        }

        public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target, PlayerControl killer)
        {
            foreach (var apc in playerIdList)
            {
                LocateArrow.RemoveAllTarget(apc);
                SendRPC(apc, false);
            }

            // Only 1 Target at a time
            if (BloodhoundTarget.ContainsKey(pc.PlayerId))
            {
                return;
            }

            BloodhoundTarget.Add(pc.PlayerId, killer.PlayerId);
            TargetArrow.Add(pc.PlayerId, killer.PlayerId);

            if (LeaveDeadBodyUnreportable.GetBool())
            {
                UnreportablePlayers.Add(target.PlayerId);
            }
        }

        public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (!seer.Is(CustomRoles.Bloodhound)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (GameStates.IsMeeting) return "";
            if (BloodhoundTarget.ContainsKey(seer.PlayerId)) return TargetArrow.GetArrows(seer, BloodhoundTarget[seer.PlayerId]);
            return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
        }
    }
}
