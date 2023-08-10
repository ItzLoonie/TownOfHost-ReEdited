using HarmonyLib;
using Discord;

// Comes from Town of Us Rewritten, by Det
namespace TOHE.Patches
{
    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    public class DiscordRPC
    {
        public static void Prefix([HarmonyArgument(0)] Activity activity)
        {
            activity.Details += $" Town of Host Edited";
        }
    }
}