using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class PizzaDelivery
{
    private static readonly int Id = 6831;
    private static List<byte> playerIdList = new();

    public static OptionItem PizzaCooldown;
    public static OptionItem PizzaMax;

    

    private static int PizzaLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.PizzaDelivery);
        PizzaCooldown = FloatOptionItem.Create(Id + 10, "PizzaCooldown", new(0f, 990f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PizzaDelivery])
            .SetValueFormat(OptionFormat.Seconds);
        PizzaMax = IntegerOptionItem.Create(Id + 12, "PizzaMax", new(1, 99, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PizzaDelivery])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        PizzaLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        PizzaLimit = PizzaMax.GetInt();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPizzaLimit, SendOption.Reliable, -1);
        writer.Write(PizzaLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        PizzaLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PizzaCooldown.GetFloat();
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && PizzaLimit >= 1;
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (PizzaLimit < 1) return false;
        if (CanBeHandcuffed(target))
        {
            PizzaLimit--;

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.PizzaDelivery), GetString("PizzaDeliveryPizza")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.PizzaDelivery), GetString("PizzaByPizzaDelivery")));
            Utils.NotifyRoles();


            if (PizzaLimit < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{PizzaLimit}次招募机会", "PizzaDelivery");
            return true;
        }
        
        if (PizzaLimit < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.PizzaDelivery), GetString("PizzaInvalid")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{PizzaLimit}次招募机会", "PizzaDelivery");
        return false;
    }
    public static string GetPizzaLimit() => Utils.ColorString(PizzaLimit >= 1 ? Utils.GetRoleColor(CustomRoles.PizzaDelivery) : Color.gray, $"({PizzaLimit})");
    public static bool CanBeHandcuffed(this PlayerControl pc)
    {
        return pc != null && !pc.Is(CustomRoles.PizzaDelivery)
        && !(
            false
            );
    }
}
