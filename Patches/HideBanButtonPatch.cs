/*
 //TODO:: Fix this its important
 using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Toggle))]
class CancelBanMenuStuckPatch
{
    public static void Prefix(ChatController __instance)
    {
        if (__instance.IsOpen && !__instance.animating) // (IsOpen==true) == 今から閉じないといけない
        {
            // BanButtonを非表示にする
            __instance.BanButton.SetVisible(false);
        }
    }
}
*/