using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    public static class Virus
    {
        private static readonly int Id = 6052269;
        private static List<byte> playerIdList = new();
        private static int InfectLimit = new();

        private static OptionItem KillCooldown;
        private static OptionItem InfectMax;
        public static OptionItem CanVent;
        public static OptionItem KnowTargetRole;
        public static OptionItem TargetKnowOtherTarget;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Virus, 1, zeroOne: false);
            KillCooldown = FloatOptionItem.Create(Id + 10, "VirusKillCooldown", new(0f, 990f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
                .SetValueFormat(OptionFormat.Seconds);
            CanVent = BooleanOptionItem.Create(Id + 11, "VirusCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
            InfectMax = IntegerOptionItem.Create(Id + 12, "VirusInfectMax", new(1, 15, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus])
                .SetValueFormat(OptionFormat.Times);
            KnowTargetRole = BooleanOptionItem.Create(Id + 13, "VirusKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
            TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "VirusTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Virus]);
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

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

        private static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVirusInfectLimit, SendOption.Reliable, -1);
            writer.Write(InfectLimit);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            InfectLimit = reader.ReadInt32();
        }

        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (InfectLimit < 1) return;

            Main.InfectedBodies.Add(target.PlayerId);
            
            return;
        }

        public static void OnKilledBodyReport(PlayerControl killer, PlayerControl target)
        {
            if (CanBeInfected(target))
            {
                InfectLimit--;
                SendRPC();
                target.RpcSetCustomRole(CustomRoles.Contagious);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("VirusInfectPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("InfectedByVirus")));
                Utils.NotifyRoles();

                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Contagious.ToString(), "Assign " + CustomRoles.Contagious.ToString());
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{InfectLimit}次魅惑机会", "Virus");
                return;
            }

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("SuccubusInvalidTarget")));
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{InfectLimit}次魅惑机会", "Virus");
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
            return true;
        }
    }
}
