using UnityEngine;
using System.Linq;
using AmongUs.GameOptions;
using System.Collections.Generic;

using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class CovenLeader
{
    private static readonly int Id = 10350;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> CovenLeaderList = new();

    private static OptionItem ControlCooldown;
    public static OptionItem CanVent;

    public static void SetupCustomOption()
    {
        //CovenLeaderは1人固定
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.CovenLeader, 1, zeroOne: false);
        ControlCooldown = FloatOptionItem.Create(Id + 12, "ControlCooldown", new(0f, 180f, 1f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader]);
    }
    public static void Init()
    {
        playerIdList = new();
        CovenLeaderList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        CovenLeaderList[target.PlayerId] = killer.PlayerId;
        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer);

        return false;
    }

    public static void OnFixedUpdate(PlayerControl leader)
    {
        if (!IsEnable) return;
        if (!GameStates.IsInTask) return;
        if (!CovenLeaderList.ContainsKey(leader.PlayerId)) return;

        if (!leader.IsAlive() || Pelican.IsEaten(leader.PlayerId))
        {
            CovenLeaderList.Remove(leader.PlayerId);
        }
        else
        {
            var covenleaderPos = leader.transform.position;
            Dictionary<byte, float> targetDistance = new();
            float dis;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != leader.PlayerId && !(target.Is(CustomRoleTypes.Impostor) || target.Is(CustomRoles.Glitch) || target.Is(CustomRoles.Pestilence)))
                {
                    dis = Vector2.Distance(covenleaderPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }

            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];

                if (min.Value <= KillRange && leader.CanMove && target.CanMove)
                {
                    if (leader.RpcCheckAndMurder(target, true))
                    {
                        var puppeteerId = CovenLeaderList[leader.PlayerId];
                        RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                        leader.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        CovenLeaderList.Remove(leader.PlayerId);
                        Utils.NotifyRoles(SpecifySeer: leader);
                    }
                }
            }
        }
    }

    public static void OnReportDeadBody()
    {
        CovenLeaderList.Clear();
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (CovenLeaderList.ContainsValue(seer.PlayerId) && CovenLeaderList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.CovenLeader), "◆") : "";
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ControlCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(true);
    public static void CanUseVent(PlayerControl player)
    {
        bool CovenLeader_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(CovenLeader_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = CovenLeader_canUse;
    }
}
