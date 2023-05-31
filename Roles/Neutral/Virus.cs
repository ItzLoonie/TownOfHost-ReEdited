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

        public static void SetupCustomOption()
        {
        }

        public static void Init()
        {
            playerIdList = new();
            InfectLimit = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            InfectLimit = 5;

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }

        private static void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSuccubusCharmLimit, SendOption.Reliable, -1);
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
            //if (CanBeInfected(target))
            //{
            //    InfectLimit--;
            //    SendRPC();
            //    target.RpcSetCustomRole(CustomRoles.Charmed);

            //    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("SuccubusCharmedPlayer")));
            //    target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CharmedBySuccubus")));
            //    Utils.NotifyRoles();

            //    killer.ResetKillCooldown();
            //    killer.SetKillCooldown();
            //    killer.RpcGuardAndKill(target);
            //    target.RpcGuardAndKill(killer);
            //    target.RpcGuardAndKill(target);

            //    Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Charmed.ToString(), "Assign " + CustomRoles.Charmed.ToString());
            //    Logger.Info($"{killer.GetNameWithRole()} : 剩余{InfectLimit}次魅惑机会", "Succubus");
            //    return;
            //}
            //killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("SuccubusInvalidTarget")));
            //Logger.Info($"{killer.GetNameWithRole()} : 剩余{InfectLimit}次魅惑机会", "Succubus");
        }

        public static bool KnowRole(PlayerControl player, PlayerControl target)
        {
            if (player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Succubus)) return true;
            //if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Charmed)) return true;
            //if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed)) return true;
            return false;
        }
        public static string GetCharmLimit() => Utils.ColorString(InfectLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Succubus) : Color.gray, $"({InfectLimit})");

        public static bool CanBeInfected(this PlayerControl pc)
        {
            return true;
        }
    }
}
