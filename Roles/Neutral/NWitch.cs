using UnityEngine;
using System.Linq;
using AmongUs.GameOptions;
using System.Collections.Generic;

using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class NWitch
{
    private static readonly int Id = 10300;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> TaglockedList = new();

    private static OptionItem ControlCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.NWitch, 1, zeroOne: false);
        ControlCooldown = FloatOptionItem.Create(Id + 10, "ControlCooldown", new(0f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NWitch])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NWitch]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NWitch]);
    }
    public static void Init()
    {
        playerIdList = new();
        TaglockedList = new();
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
        TaglockedList[target.PlayerId] = killer.PlayerId;
        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer, ForceLoop: false);

        return false;
    }

    public static void OnFixedUpdate(PlayerControl taglocked)
    {
        if (!IsEnable) return;
        if (!GameStates.IsInTask) return;
        if (!TaglockedList.ContainsKey(taglocked.PlayerId)) return;

        if (!taglocked.IsAlive() || Pelican.IsEaten(taglocked.PlayerId))
        {
            TaglockedList.Remove(taglocked.PlayerId);
        }
        else
        {
            var taglockedPos = taglocked.transform.position;
            Dictionary<byte, float> targetDistance = new();
            float dis;
            foreach (var target in Main.AllAlivePlayerControls)
            {

                {
                    if (target.PlayerId != taglocked.PlayerId && !target.Is(CustomRoles.NWitch) && !target.Is(CustomRoles.Glitch) && !target.Is(CustomRoles.Pestilence))
                    {
                        dis = Vector2.Distance(taglockedPos, target.transform.position);
                        targetDistance.Add(target.PlayerId, dis);
                    }
                }
            }
            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && taglocked.CanMove && target.CanMove)
                {
                    if (taglocked.RpcCheckAndMurder(target, true))
                    {
                        var taglockedId = TaglockedList[taglocked.PlayerId];
                        RPC.PlaySoundRPC(taglockedId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(taglockedId));
                        taglocked.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        TaglockedList.Remove(taglocked.PlayerId);
                        Utils.NotifyRoles(SpecifySeer: taglocked, ForceLoop: false);
                    }
                }
            }
        }
    }
    public static void OnReportDeadBody()
    {
        TaglockedList.Clear();
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (TaglockedList.ContainsValue(seer.PlayerId) && TaglockedList.ContainsValue(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.NWitch), "◆") : "";

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ControlCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool NWitch_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(NWitch_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = NWitch_canUse;
    }
}
