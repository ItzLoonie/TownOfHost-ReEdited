using HarmonyLib;

namespace TOHE.Patches;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
class AmogusClientAwakePatch
{
	public static void Postfix()
		=> DiscordRP.Update();
}
