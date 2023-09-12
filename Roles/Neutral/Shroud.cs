using UnityEngine;
using System.Linq;
using AmongUs.GameOptions;
using System.Collections.Generic;

using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Shroud
{
    private static readonly int Id = 10320;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> ShroudList = new();

    private static OptionItem ShroudCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shroud, 1, zeroOne: false);
        ShroudCooldown = FloatOptionItem.Create(Id + 10, "ShroudCooldown", new(0f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
    }
    public static void Init()
    {
        playerIdList = new();
        ShroudList = new();
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
        ShroudList[target.PlayerId] = killer.PlayerId;
        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer, ForceLoop: false);

        return false;
    }

    public static void OnFixedUpdate(PlayerControl shroud)
    {
        if (!IsEnable) return;
        if (!GameStates.IsInTask) return;
        if (!ShroudList.ContainsKey(shroud.PlayerId)) return;

        if (!shroud.IsAlive() || Pelican.IsEaten(shroud.PlayerId))
        {
            ShroudList.Remove(shroud.PlayerId);
        }
        else
        {
            var shroudPos = shroud.transform.position;
            Dictionary<byte, float> targetDistance = new();
            float dis;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != shroud.PlayerId && !target.Is(CustomRoles.Shroud) && !target.Is(CustomRoles.Glitch) && !target.Is(CustomRoles.Pestilence))
                {
                    dis = Vector2.Distance(shroudPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }
            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && shroud.CanMove && target.CanMove)
                {
                    if (shroud.RpcCheckAndMurder(target, true))
                    {
                        var shroudId = ShroudList[shroud.PlayerId];
                        RPC.PlaySoundRPC(shroudId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(shroudId));
                        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Shrouded;
                        shroud.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        ShroudList.Remove(shroud.PlayerId);
                        Utils.NotifyRoles(SpecifySeer: shroud, ForceLoop: false);
                    }
                }
            }
        }
    }

    public static void OnReportDeadBody()
    {
        ShroudList.Clear();
    }

    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Shroud) && ShroudList.ContainsKey(seer.PlayerId) && ShroudList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈") : "";
    
    public static string GetShroudMark(byte target, bool isMeeting)
    {
        if (isMeeting && ShroudList.ContainsKey(target))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈");
        }
        return "";
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ShroudCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool Shroud_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Shroud_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = Shroud_canUse;
    }
}
