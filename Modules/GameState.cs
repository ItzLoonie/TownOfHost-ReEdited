using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE;

public class PlayerState
{
    byte PlayerId;
    public CustomRoles MainRole;
    public List<CustomRoles> SubRoles;
    public CountTypes countTypes;
    public bool IsDead { get; set; }
    public DeathReason deathReason { get; set; }
    public TaskState taskState;
    public bool IsBlackOut { get; set; }
    public (DateTime, byte) RealKiller;
    public PlainShipRoom LastRoom;
    public Dictionary<byte, string> TargetColorData;
    public PlayerState(byte playerId)
    {
        MainRole = CustomRoles.NotAssigned;
        SubRoles = new();
        countTypes = CountTypes.OutOfGame;
        PlayerId = playerId;
        IsDead = false;
        deathReason = DeathReason.etc;
        taskState = new();
        IsBlackOut = false;
        RealKiller = (DateTime.MinValue, byte.MaxValue);
        LastRoom = null;
        TargetColorData = new();
    }
    public CustomRoles GetCustomRole()
    {
        var RoleInfo = Utils.GetPlayerInfoById(PlayerId);
        return RoleInfo.Role == null
            ? MainRole
            : RoleInfo.Role.Role switch
            {
                RoleTypes.Crewmate => CustomRoles.Crewmate,
                RoleTypes.Engineer => CustomRoles.Engineer,
                RoleTypes.Scientist => CustomRoles.Scientist,
                RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
                RoleTypes.Impostor => CustomRoles.Impostor,
                RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
                _ => CustomRoles.Crewmate,
            };
    }
    public void SetMainRole(CustomRoles role)
    {
        MainRole = role;
        countTypes = role.GetCountTypes();
    }
    public void SetSubRole(CustomRoles role, bool AllReplace = false)
    {
        if (AllReplace)
            SubRoles.ToArray().Do(role => SubRoles.Remove(role));

        if (!SubRoles.Contains(role))
            SubRoles.Add(role);

        if (role == CustomRoles.Madmate)
        {
            countTypes = Options.MadmateCountMode.GetInt() switch
            {
                0 => CountTypes.OutOfGame,
                1 => CountTypes.Impostor,
                2 => CountTypes.Crew,
                _ => throw new NotImplementedException()
            };
            SubRoles.Remove(CustomRoles.Charmed);
            SubRoles.Remove(CustomRoles.Sidekick);
            SubRoles.Remove(CustomRoles.Infected);
        }
        if (role == CustomRoles.Charmed)
        {
            countTypes = Succubus.CharmedCountMode.GetInt() switch
            {
                0 => CountTypes.OutOfGame,
                1 => CountTypes.Succubus,
                2 => countTypes,
                _ => throw new NotImplementedException()
            };
            SubRoles.Remove(CustomRoles.Madmate);
            SubRoles.Remove(CustomRoles.Sidekick);
            SubRoles.Remove(CustomRoles.Infected);
        }
        if (role == CustomRoles.Sidekick)
        {
            countTypes = Options.SidekickCountMode.GetInt() switch
            {
                0 => CountTypes.Jackal,
                1 => CountTypes.OutOfGame,
                2 => countTypes,
                _ => throw new NotImplementedException()
            };
            SubRoles.Remove(CustomRoles.Madmate);
            SubRoles.Remove(CustomRoles.Charmed);
            SubRoles.Remove(CustomRoles.Infected);
        }
        if (role == CustomRoles.Infected)
        {
            countTypes = CountTypes.Infectious;
            SubRoles.Remove(CustomRoles.Madmate);
            SubRoles.Remove(CustomRoles.Sidekick);
            SubRoles.Remove(CustomRoles.Charmed);
        }

    }
    public void RemoveSubRole(CustomRoles role)
    {
        if (SubRoles.Contains(role))
            SubRoles.Remove(role);
    }

    public void SetDead()
    {
        IsDead = true;
        if (AmongUsClient.Instance.AmHost)
        {
            RPC.SendDeathReason(PlayerId, deathReason);
        }
    }
    public bool IsSuicide() { return deathReason == DeathReason.Suicide; }
    public TaskState GetTaskState() { return taskState; }
    public void InitTask(PlayerControl player)
    {
        taskState.Init(player);
    }
    public void UpdateTask(PlayerControl player)
    {
        taskState.Update(player);
    }
    public enum DeathReason
    {
        Kill,
        Vote,
        Suicide,
        Spell,
        Hex,
        FollowingSuicide,
        Bite,
        Poison,
        Bombed,
        Misfire,
        Torched,
        Sniped,
        Revenge,
        Execution,
        Disconnected,
        Fall,

        // TOHE
        Gambled,
        Eaten,
        Sacrifice,
        Quantization,
        Overtired,
        Ashamed,
        PissedOff,
        Dismembered,
        LossOfHead,
        Trialed,

        etc = -1
    }
    public byte GetRealKiller()
        => IsDead && RealKiller.Item1 != DateTime.MinValue ? RealKiller.Item2 : byte.MaxValue;
    public int GetKillCount(bool ExcludeSelfKill = false)
    {
        int count = 0;
        foreach (var state in Main.PlayerStates.Values)
            if (!(ExcludeSelfKill && state.PlayerId == PlayerId) && state.GetRealKiller() == PlayerId)
                count++;
        return count;
    }
}
public class TaskState
{
    public static int InitialTotalTasks;
    public int AllTasksCount;
    public int CompletedTasksCount;
    public bool hasTasks;
    public int RemainingTasksCount => AllTasksCount - CompletedTasksCount;
    public bool DoExpose => RemainingTasksCount <= Options.SnitchExposeTaskLeft && hasTasks;
    public bool IsTaskFinished => RemainingTasksCount <= 0 && hasTasks;
    public TaskState()
    {
        this.AllTasksCount = -1;
        this.CompletedTasksCount = 0;
        this.hasTasks = false;
    }

    public void Init(PlayerControl player)
    {
        Logger.Info($"{player.GetNameWithRole()}: InitTask", "TaskState.Init");
        if (player == null || player.Data == null || player.Data.Tasks == null) return;
        if (!Utils.HasTasks(player.Data, false))
        {
            AllTasksCount = 0;
            return;
        }
        hasTasks = true;
        AllTasksCount = player.Data.Tasks.Count;
        Logger.Info($"{player.GetNameWithRole()}: TaskCounts = {CompletedTasksCount}/{AllTasksCount}", "TaskState.Init");
    }
    public void Update(PlayerControl player)
    {
        Logger.Info($"{player.GetNameWithRole()}: UpdateTask", "TaskState.Update");
        GameData.Instance.RecomputeTaskCounts();
        Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");

        //初期化出来ていなかったら初期化
        if (AllTasksCount == -1) Init(player);

        if (!hasTasks) return;

        if (AmongUsClient.Instance.AmHost)
        {
            //FIXME:SpeedBooster class transplant
            if (player.IsAlive()
            && player.Is(CustomRoles.SpeedBooster)
            && ((CompletedTasksCount + 1) <= Options.SpeedBoosterTimes.GetInt()))
            {
                Logger.Info("增速者触发加速:" + player.GetNameWithRole(), "SpeedBooster");
                Main.AllPlayerSpeed[player.PlayerId] += Options.SpeedBoosterUpSpeed.GetFloat();
                if (Main.AllPlayerSpeed[player.PlayerId] > 3) player.Notify(Translator.GetString("SpeedBoosterSpeedLimit"));
                else player.Notify(string.Format(Translator.GetString("SpeedBoosterTaskDone"), Main.AllPlayerSpeed[player.PlayerId].ToString("0.0#####")));
            }

            /*
            //叛徒修理搞破坏
            if (player.IsAlive()
            && player.Is(CustomRoles.SabotageMaster)
            && player.Is(CustomRoles.Madmate))
            {
                List<SystemTypes> SysList = new();
                foreach (SystemTypes sys in Enum.GetValues(typeof(SystemTypes)))
                    if (Utils.IsActive(sys)) SysList.Add(sys);

                if (SysList.Count > 0)
                {
                    var SbSys = SysList[IRandom.Instance.Next(0, SysList.Count)];

                    MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, player.GetClientId());
                    SabotageFixWriter.Write((byte)SbSys);
                    MessageExtensions.WriteNetObject(SabotageFixWriter, player);
                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

                    foreach (var target in Main.AllPlayerControls)
                    {
                        if (target == player || target.Data.Disconnected) continue;
                        SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
                        SabotageFixWriter.Write((byte)SbSys);
                        MessageExtensions.WriteNetObject(SabotageFixWriter, target);
                        AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                    }
                    Logger.Info("叛徒修理工造成破坏:" + player.cosmetics.nameText.text, "SabotageMaster");
                }
            }
            */

            //传送师完成任务
            if (player.IsAlive()
            && player.Is(CustomRoles.Transporter)
            && ((CompletedTasksCount + 1) <= Options.TransporterTeleportMax.GetInt()))
            {
                Logger.Info("传送师触发传送:" + player.GetNameWithRole(), "Transporter");
                var rd = IRandom.Instance;
                List<PlayerControl> AllAlivePlayer = new();
                foreach (var pc in Main.AllAlivePlayerControls.Where(x => !Pelican.IsEaten(x.PlayerId) && !x.inVent && !x.onLadder)) AllAlivePlayer.Add(pc);
                if (AllAlivePlayer.Count >= 2)
                {
                    var tar1 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
                    AllAlivePlayer.Remove(tar1);
                    var tar2 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
                    var pos = tar1.GetTruePosition();
                    Utils.TP(tar1.NetTransform, tar2.GetTruePosition());
                    Utils.TP(tar2.NetTransform, pos);
                    tar1.RPCPlayCustomSound("Teleport");
                    tar2.RPCPlayCustomSound("Teleport");
                    tar1.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), tar2.GetRealName())));
                    tar2.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), tar1.GetRealName())));
                }
                else if (player.Is(CustomRoles.Transporter))
                {
                    player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), string.Format(Translator.GetString("ErrorTeleport"), player.GetRealName())));
                }
            }

            //工作狂做完了
            if (player.Is(CustomRoles.Workaholic) && (CompletedTasksCount + 1) >= AllTasksCount
                    && !(Options.WorkaholicCannotWinAtDeath.GetBool() && !player.IsAlive()))
            {
                Logger.Info("工作狂任务做完了", "Workaholic");
                RPC.PlaySoundRPC(player.PlayerId, Sounds.KillSound);
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.PlayerId != player.PlayerId)
                    {
                        Main.PlayerStates[pc.PlayerId].deathReason = pc.PlayerId == player.PlayerId ?
                            PlayerState.DeathReason.Overtired : PlayerState.DeathReason.Ashamed;
                        pc.RpcMurderPlayerV3(pc);
                        Main.PlayerStates[pc.PlayerId].SetDead();
                        pc.SetRealKiller(player);
                    }
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Workaholic); //爆破で勝利した人も勝利させる
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
            }

            //船鬼要抽奖啦
      /*      if (player.Is(CustomRoles.Crewpostor))
            {

                List<PlayerControl> list = Main.AllAlivePlayerControls.Where(x => x.PlayerId != player.PlayerId && (Options.CrewpostorCanKillAllies.GetBool() || !x.GetCustomRole().IsImpostorTeam())).ToList();
                if (list.Count < 1)
                {
                    Logger.Info($"船鬼没有可击杀目标", "Crewpostor");
                }
                else
                {
                    list = list.OrderBy(x => Vector2.Distance(player.GetTruePosition(), x.GetTruePosition())).ToList();
                    var target = list[0];
                    target.SetRealKiller(player);
                    target.RpcCheckAndMurder(target);
                    player.RpcGuardAndKill();
                    Logger.Info($"船鬼完成任务击杀：{player.GetNameWithRole()} => {target.GetNameWithRole()}", "Crewpostor");
                }
            } */

        }

        //クリアしてたらカウントしない
        if (CompletedTasksCount >= AllTasksCount) return;

        CompletedTasksCount++;

        //調整後のタスク量までしか表示しない
        CompletedTasksCount = Math.Min(AllTasksCount, CompletedTasksCount);
        Logger.Info($"{player.GetNameWithRole()}: TaskCounts = {CompletedTasksCount}/{AllTasksCount}", "TaskState.Update");

    }
}
public class PlayerVersion
{
    public readonly Version version;
    public readonly string tag;
    public readonly string forkId;
    [Obsolete] public PlayerVersion(string ver, string tag_str) : this(Version.Parse(ver), tag_str, "") { }
    [Obsolete] public PlayerVersion(Version ver, string tag_str) : this(ver, tag_str, "") { }
    public PlayerVersion(string ver, string tag_str, string forkId) : this(Version.Parse(ver), tag_str, forkId) { }
    public PlayerVersion(Version ver, string tag_str, string forkId)
    {
        version = ver;
        tag = tag_str;
        this.forkId = forkId;
    }
    public bool IsEqual(PlayerVersion pv)
    {
        return pv.version == version && pv.tag == tag;
    }
}
public static class GameStates
{
    public static bool InGame = false;
    public static bool AlreadyDied = false;
    public static bool IsModHost => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x.PlayerId == 0 && x.IsModClient());
    public static bool IsLobby => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Joined;
    public static bool IsInGame => InGame;
    public static bool IsEnded => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Ended;
    public static bool IsNotJoined => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.NotJoined;
    public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
    public static bool IsLocalGame => AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
    public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool IsInTask => InGame && !MeetingHud.Instance;
    public static bool IsMeeting => InGame && MeetingHud.Instance;
    public static bool IsVoting => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted;
    public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    /**********TOP ZOOM.cs***********/
    public static bool IsShip => ShipStatus.Instance != null;
    public static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
    public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;
}
public static class MeetingStates
{
    public static DeadBody[] DeadBodies = null;
    public static GameData.PlayerInfo ReportTarget = null;
    public static bool IsEmergencyMeeting => ReportTarget == null;
    public static bool IsExistDeadBody => DeadBodies.Length > 0;
    public static bool MeetingCalled = false;
    public static bool FirstMeeting = true;
}