using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    public static class SpeedRunner
    {
        private static readonly int Id = 96000;
        public static List<byte> playerIdList = new();

        public static OptionItem EveryOneKnowSpeedRunner;
        public static OptionItem SpeedRunnerCanVent;
        public static OptionItem SpeedRunnerCanGuess;
        public static OverrideTasksData SpeedRunnerTasks;
        public static OptionItem AddTasksPreKill;

        public static bool MurderCheck;
        public static float OriginalSpeed;
        public static int AddShortTasks;
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.SpeedRunner, 1);
            EveryOneKnowSpeedRunner = BooleanOptionItem.Create(Id + 10, "EveryOneKnowSpeedRunner", true, TabGroup.OtherRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.SpeedRunner]);
            SpeedRunnerCanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedRunner]);
            SpeedRunnerCanGuess = BooleanOptionItem.Create(Id + 12, "CanGuess", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedRunner]);
            AddTasksPreKill = IntegerOptionItem.Create(Id + 13, "AddTasksPreKill", new(0, 15, 1), 1, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.SpeedRunner]);
            SpeedRunnerTasks = OverrideTasksData.Create(Id + 14, TabGroup.OtherRoles, CustomRoles.SpeedRunner);
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
            AddShortTasks = 0;
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
            if (!player.Is(CustomRoles.SpeedRunner)) return;
            CheckTask(player);
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return false;
            if (!GameStates.IsMeeting && !MurderCheck)
            {
                CheckTask(target);
                MurderCheck = true;
                AddShortTasks = AddShortTasks + AddTasksPreKill.GetValue();
                var taskState = target.GetPlayerTaskState();
                taskState.AllTasksCount = taskState.AllTasksCount + AddTasksPreKill.GetValue();

                Utils.TP(target.NetTransform, GetBlackRoomPS());
                OriginalSpeed = Main.AllPlayerSpeed[target.PlayerId];
                Main.AllPlayerSpeed[target.PlayerId] = 0.01f; //I'm too lazy to do tp like pelican LOL
                ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
                NameNotifyManager.Notify(target, string.Format(GetString("SpeedRunnerMurdered"), killer.GetRealName()));
                target.RpcGuardAndKill();
                target.MarkDirtySettings();
                NameNotifyManager.Notify(killer, GetString("MurderSpeedRunner"));
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                killer.SetKillCooldown(time: Main.AllPlayerKillCooldown[killer.PlayerId], forceAnime: true);
            }
            return false;
        } //My idea is to encourage everyone to kill SpeedRunner and won't waste shoots on it, only resets cd.
        public static void AfterMeetingTasks()
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)))
            {
                if (pc == null || !pc.Is(CustomRoles.SpeedRunner)) continue;
                if (MurderCheck || Main.AllPlayerSpeed[pc.PlayerId] < 0.1f)
                {
                    Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - 0.01f + OriginalSpeed;
                    ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                    pc.MarkDirtySettings();
                }
                CheckTask(pc);
                MurderCheck = false;
                ResetTasks();
            }
        }
        public static void CheckTask(PlayerControl player)  //Check SpeedRunner win
        {
            if (player == null) return;
            if (!player.Is(CustomRoles.SpeedRunner)) return;
            var taskState = player.GetPlayerTaskState();
            if (!MurderCheck)
            {
                if (taskState.IsTaskFinished)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SpeedRunner);
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }
            }
            else ResetTasks();
        } //Because all the checkmurder is patched on speedrunner , we don't need to consider other winning condition.
        public static void ResetTasks()
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)))
            {
                if (pc == null || !pc.Is(CustomRoles.SpeedRunner)) continue;
                var taskState = pc.GetPlayerTaskState();
                GameData.Instance.RpcSetTasks(pc.PlayerId, new byte[0]);
                taskState.CompletedTasksCount = 0;                
                pc.RpcGuardAndKill();
                NameNotifyManager.Notify(pc, GetString("SpeedRunnerTasksReset"));
            }
        }
    }
}
