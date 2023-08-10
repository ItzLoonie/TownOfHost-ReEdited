using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Werewolf
{
    private static readonly int Id = 12850;
    public static List<byte> playerIdList = new();

    public static OptionItem KillCooldown;
    public static OptionItem KillCooldownAfterKilling;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        //Werewolfは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Werewolf, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownAfterKilling = FloatOptionItem.Create(Id + 12, "KillCooldownAfterKilling", new(0f, 180f, 2.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Werewolf]);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Any();
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool Werewolf_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Werewolf_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = Werewolf_canUse;
    }
}
