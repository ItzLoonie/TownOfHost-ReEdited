using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    public static class Spiritcaller
    {
        private static readonly int Id = 6123470;
        private static List<byte> playerIdList = new();
        private static int SpiritLimit = new();

        private static List<byte> GhostPlayer = new();

        private static OptionItem KillCooldown;
        public static OptionItem CanVent;
        public static OptionItem ImpostorVision;
        private static OptionItem SpiritMax;
        public static OptionItem SpiritAbilityCooldown;
        private static OptionItem SpiritFreezeTime;
        private static OptionItem SpiritProtectTime;
        private static OptionItem SpiritCauseVision;
        private static OptionItem SpiritCauseVisionTime;

        private static long ProtectTimeStamp = new();

        public static void SetupCustomOption()
        {
            // todo: Speak with evil spirits

            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Spiritcaller, 1, zeroOne: false);
            KillCooldown = FloatOptionItem.Create(Id + 10, "SpiritcallerKillCooldown", new(0f, 60f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Seconds);
            CanVent = BooleanOptionItem.Create(Id + 11, "SpiritcallerCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
            ImpostorVision = BooleanOptionItem.Create(Id + 12, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
            SpiritMax = IntegerOptionItem.Create(Id + 13, "SpiritcallerSpiritMax", new(1, 15, 1), 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Times);
            SpiritAbilityCooldown = FloatOptionItem.Create(Id + 14, "SpiritcallerSpiritAbilityCooldown", new(0f, 90f, 1f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Seconds);
            SpiritFreezeTime = FloatOptionItem.Create(Id + 15, "SpiritcallerFreezeTime", new(0f, 30f, 1f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Seconds);
            SpiritProtectTime = FloatOptionItem.Create(Id + 16, "SpiritcallerProtectTime", new(0f, 30f, 1f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Seconds);
            SpiritCauseVision = FloatOptionItem.Create(Id + 17, "SpiritCauseVision", new(0f, 5f, 0.05f), 0.3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Multiplier);
            SpiritCauseVisionTime = FloatOptionItem.Create(Id + 18, "SpiritcallerCauseVisionTime", new(0f, 45f, 1f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
                .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Init()
        {
            playerIdList = new();
            SpiritLimit = new();
            ProtectTimeStamp = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            SpiritLimit = SpiritMax.GetInt();
            ProtectTimeStamp = 0;

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

        public static bool IsGhostPlayer(byte playerId) => GhostPlayer.Contains(playerId);
        public static bool InProtect(PlayerControl player) => player.Is(CustomRoles.Spiritcaller) && ProtectTimeStamp > Utils.GetTimeStamp();

        private static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSpiritcallerSpiritLimit, SendOption.Reliable, -1);
            writer.Write(SpiritLimit);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            SpiritLimit = reader.ReadInt32();
        }

        public static void OnCheckMurder(PlayerControl target)
        {
            if (SpiritLimit < 1) return;

            SpiritLimit--;
            SendRPC();
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

        public static void OnFixedUpdate(PlayerControl pc)
        {
            if (!GameStates.IsInTask || !pc.Is(CustomRoles.Spiritcaller)) return;
            if (ProtectTimeStamp < Utils.GetTimeStamp() && ProtectTimeStamp != 0)
            {
                ProtectTimeStamp = 0;
            }
        }

        public static string GetSpiritLimit() => Utils.ColorString(SpiritLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Spiritcaller) : Color.gray, $"({SpiritLimit})");

        public static void FreezePlayer(PlayerControl player, PlayerControl target)
        {
            var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.MarkDirtySettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                target.MarkDirtySettings();
                RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
            }, SpiritFreezeTime.GetFloat()); 
        }

        public static void ReduceVision(PlayerControl player, PlayerControl target)
        {
        }

        public static void ProtectSpiritcaller()
        {
            ProtectTimeStamp = Utils.GetTimeStamp() + (long)SpiritProtectTime.GetFloat();
        }
    }
}