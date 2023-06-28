using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using static Il2CppMono.Security.X509.X520;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Neutral
{
    public static class Spiritcaller
    {
        private static readonly int Id = 6123470;
        private static List<byte> playerIdList = new();
        private static int InfectLimit = new();
        public static List<byte> GhostPlayer = new();

        private static OptionItem KillCooldown;
        private static OptionItem InfectMax;
        public static OptionItem CanVent;
        public static OptionItem ImpostorVision;
        public static OptionItem KnowTargetRole;
        public static OptionItem TargetKnowOtherTarget;
        public static OptionItem KillInfectedPlayerAfterMeeting;
        public static OptionItem ContagiousCountMode;

        public static void SetupCustomOption()
        {
            // todo: options & win condition

            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Spiritcaller, 1, zeroOne: false);
            KillCooldown = FloatOptionItem.Create(Id + 10, "VirusKillCooldown", new(0f, 990f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Seconds);
            CanVent = BooleanOptionItem.Create(Id + 11, "VirusCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
            ImpostorVision = BooleanOptionItem.Create(Id + 16, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
            InfectMax = IntegerOptionItem.Create(Id + 12, "VirusInfectMax", new(1, 15, 1), 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Times);
            KnowTargetRole = BooleanOptionItem.Create(Id + 13, "VirusKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
            TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "VirusTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
            KillInfectedPlayerAfterMeeting = BooleanOptionItem.Create(Id + 15, "VirusKillInfectedPlayerAfterMeeting", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
        }

        public static void Init()
        {
            playerIdList = new();
            InfectLimit = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            InfectLimit = InfectMax.GetInt();

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

        public static bool IsGhostPlayer(byte playerId) => GhostPlayer.Contains(playerId);

        private static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVirusInfectLimit, SendOption.Reliable, -1);
            writer.Write(InfectLimit);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        private static void SendRPCInfectKill(byte virusId, byte target = 255)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
            writer.Write(virusId);
            writer.Write(target);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            InfectLimit = reader.ReadInt32();
        }

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            //if (InfectLimit < 1) return;
            GhostPlayer.Add(target.PlayerId);
            target.RpcSetCustomRole(CustomRoles.EvilSpirit);

            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(target.GetClientId());
            writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(GetString("VirusNoticeTitle"))
                .EndRpc();
            writer.StartRpc(target.NetId, (byte)RpcCalls.SendChat)
                .Write(GetString("VirusNoticeMessage"))
                .EndRpc();
            writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(target.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }

        public static void OnKilledBodyReport(PlayerControl target)
        {
            if (!CanBeInfected(target)) return;

            InfectLimit--;
            SendRPC();

            if (KillInfectedPlayerAfterMeeting.GetBool())
            {
                Main.VirusNotify.Add(target.PlayerId, GetString("VirusNoticeMessage2"));
            }
            else
            {
                target.RpcSetCustomRole(CustomRoles.Contagious);

                Utils.NotifyRoles();

                Main.VirusNotify.Add(target.PlayerId, GetString("VirusNoticeMessage"));
            }

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Contagious.ToString(), "Assign " + CustomRoles.Contagious.ToString());
        }

        public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
        {
            if (!KillInfectedPlayerAfterMeeting.GetBool()) return;

            PlayerControl virus =
                Main.AllAlivePlayerControls.FirstOrDefault(a => a.GetCustomRole() == CustomRoles.Virus);
            if (virus == null || deathReason != PlayerState.DeathReason.Vote) return;

            if (exileIds.Contains(virus.PlayerId))
            {
                return;
            }

            var infectedIdList = new List<byte>();
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (virus.IsAlive())
                {
                    if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                    {
                        pc.SetRealKiller(virus);
                        infectedIdList.Add(pc.PlayerId);
                    }
                }
                else
                {
                    Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
                }
            }

            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Infected, infectedIdList.ToArray());
        }

        public static bool KnowRole(PlayerControl player, PlayerControl target)
        {
            if (player.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Virus)) return true;
            if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Virus) && target.Is(CustomRoles.Contagious)) return true;
            if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Contagious) && target.Is(CustomRoles.Contagious)) return true;
            return false;
        }
        public static string GetInfectLimit() => Utils.ColorString(InfectLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Virus) : Color.gray, $"({InfectLimit})");

        public static bool CanBeInfected(this PlayerControl pc)
        {
            return true && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Contagious);
        }

        public static void FreezePlayer(PlayerControl player, PlayerControl target)
        {
            var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                target.MarkDirtySettings();
                RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
            }, 5); // Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove"

            // todo test reset protect cooldown
            player.RpcResetAbilityCooldown();
        }
    }
}