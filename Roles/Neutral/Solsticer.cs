using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    public static class Solsticer
    {
        private static readonly int Id = 96000;
        public static List<byte> playerIdList = new();

        public static OptionItem EveryOneKnowSolsticer;
        public static OptionItem SolsticerCanVent;
        public static OptionItem SolsticerCanGuess;
        public static OverrideTasksData SolsticerTasks;

        public static bool MurderCheck;
        public static float OriginalSpeed;
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Solsticer, 1);
            EveryOneKnowSolsticer = BooleanOptionItem.Create(Id + 10, "EveryOneKnowSolsticer", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerCanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            SolsticerCanGuess = BooleanOptionItem.Create(Id + 12, "CanGuess", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Solsticer]);
            OverrideTasksData.Create(Id + 13, TabGroup.OtherRoles, CustomRoles.Solsticer);
        }
        public static void ApplyGameOptions()
        {
            AURoleOptions.EngineerCooldown = 0f;
            AURoleOptions.EngineerInVentMaxTime = 0f;
        }
        public static void Init()
        {
            playerIdList = new();
            MurderCheck = false;
            OriginalSpeed = Main.NormalOptions.PlayerSpeedMod;
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static Vector2 GetBlackRoomPS()
        {
            return Main.NormalOptions.MapId switch
            {
                0 => new(-27f, 3.3f), // The Skeld
                1 => new(-11.4f, 8.2f), // MIRA HQ
                2 => new(42.6f, -19.9f), // Polus
                4 => new(-16.8f, -6.2f), // Airship
                _ => throw new System.NotImplementedException(),
            };
        }
        public static void OnCompleteTask(PlayerControl player)
        {
            if (player == null) return;
            if (!player.Is(CustomRoles.Solsticer)) return;
            CheckTask(player);
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return false;
            if (!GameStates.IsMeeting && !MurderCheck)
            {
                CheckTask(target);
                MurderCheck = true;
                Utils.TP(target.NetTransform, GetBlackRoomPS());
                OriginalSpeed = Main.AllPlayerSpeed[target.PlayerId];
                Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
                ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
                NameNotifyManager.Notify(target, string.Format(GetString("SolsticerMurdered"), killer.GetRealName()));
                target.RpcGuardAndKill();
                target.MarkDirtySettings();
                NameNotifyManager.Notify(killer, GetString("MurderSolsticer"));
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                killer.SetKillCooldown(time: Main.AllPlayerKillCooldown[killer.PlayerId], forceAnime: true);
            }
            return false;
        } //My idea is to encourage everyone to kill solsticer and won't waste shoots on it, only resets cd.
        public static void AfterMeetingTasks()
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)))
            {
                if (pc == null || !pc.Is(CustomRoles.Solsticer)) continue;
                if (MurderCheck || Main.AllPlayerSpeed[pc.PlayerId] < 0.1f)
                {
                    Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - Main.MinSpeed + OriginalSpeed;
                    ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                    pc.MarkDirtySettings();
                }
                CheckTask(pc);
                MurderCheck = false;
                ResetTasks();
            }
        }
        public static void CheckTask(PlayerControl player)  //Check solsticer win
        {
            if (player == null) return;
            if (!player.Is(CustomRoles.Solsticer)) return;
            var taskState = player.GetPlayerTaskState();
            if (!MurderCheck)
            {
                if (taskState.IsTaskFinished)
                {
                    if (!player.Is(CustomRoles.Admired))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Solsticer);
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    }
                    if (player.Is(CustomRoles.Admired))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    }
                }
            }
            else ResetTasks();
        }
        public static void ResetTasks()
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)))
            {
                if (pc == null || !pc.Is(CustomRoles.Solsticer)) continue;
                var taskState = pc.GetPlayerTaskState();
                taskState.CompletedTasksCount = 0;
                GameData.Instance.RpcSetTasks(pc.PlayerId, new byte[0]);
                pc.RpcGuardAndKill();
                NameNotifyManager.Notify(pc, GetString("SolsticerTasksReset"));
            }
        }
    }
}
