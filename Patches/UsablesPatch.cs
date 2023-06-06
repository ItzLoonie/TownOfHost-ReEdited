using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
class CanUsePatch
{
    public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
    {
        canUse = couldUse = false;
        //こいつをfalseでreturnしても、タスク(サボ含む)以外の使用可能な物は使えるまま(ボタンなど)
        return __instance.AllowImpostor || Utils.HasTasks(PlayerControl.LocalPlayer.Data, false);
    }
}
[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
class EmergencyMinigamePatch
{
    public static void Postfix(EmergencyMinigame __instance)
    {
        if (Options.DisableMeeting.GetBool() || Options.CurrentGameMode == CustomGameMode.SoloKombat)
            __instance.Close();
        return;
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
class CanUseVentPatch
{
    public static bool Prefix(Vent __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc,
        [HarmonyArgument(1)] ref bool canUse,
        [HarmonyArgument(2)] ref bool couldUse,
        ref float __result)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //#######################################
        //     ==ベント処理==
        //#######################################
        //参考:https://github.com/Eisbison/TheOtherRoles/blob/main/TheOtherRoles/Patches/UsablesPatch.cs

        bool VentForTrigger = false;
        float num = float.MaxValue;

        var usableDistance = __instance.UsableDistance;

        if (pc.IsDead) return false; //死んでる人は強制的にfalseに。

        canUse = couldUse = pc.Object.CanUseImpostorVentButton();
        switch (pc.GetCustomRole())
        {
            case CustomRoles.Arsonist:
                if (pc.Object.IsDouseDone())
                    VentForTrigger = true;
                break;
            case CustomRoles.Revolutionist://跳管解锁
                if (pc.Object.IsDrawDone())
                    VentForTrigger = true;
                break;
            default:
                if (pc.Role.Role == RoleTypes.Engineer) // インポスター陣営ベースの役職とエンジニアベースの役職は常にtrue
                    canUse = couldUse = true;
                break;
        }
        if (!canUse) return false;

        canUse = couldUse = (pc.Object.inVent || canUse) && (pc.Object.CanMove || pc.Object.inVent);

        if (VentForTrigger && pc.Object.inVent)
        {
            canUse = couldUse = false;
            return false;
        }
        if (canUse)
        {
            Vector2 truePosition = pc.Object.GetTruePosition();
            Vector3 position = __instance.transform.position;
            num = Vector2.Distance(truePosition, position);
            canUse &= num <= usableDistance && !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShipOnlyMask, false);
        }
        __result = num;
        return false;
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class PlayerPurchasesDataPatch
{
    public static bool Prefix(ref bool __result)
    {
        if (RunLoginPatch.ClickCount < 20) return true;
        __result = true;
        return false;
    }
}