using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;
using TOHE.Modules;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch]
public static class Credentials
{
    public static SpriteRenderer ToheLogo { get; private set; }

    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerUpdatePatch
    {
        private static readonly StringBuilder sb = new();

        private static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TextAlignmentOptions.TopRight;

            sb.Clear();

            sb.Append(Main.credentialsText);

            var ping = AmongUsClient.Instance.Ping;
            string pingcolor = "#ff4500";
            if (ping < 30) pingcolor = "#44dfcc";
            else if (ping < 100) pingcolor = "#7bc690";
            else if (ping < 200) pingcolor = "#f3920e";
            else if (ping < 400) pingcolor = "#ff146e";
            sb.Append($"\r\n").Append($"<color={pingcolor}>Ping: {ping} ms</color>");

            if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));

            if (Main.ShowFPS.Value)
            {
                var FPSGame = 1.0f / Time.deltaTime;
                Color fpscolor = Color.green;

                if (FPSGame < 20f) fpscolor = Color.red;
                else if (FPSGame < 40f) fpscolor = Color.yellow;

                sb.Append("\r\n").Append(Utils.ColorString(fpscolor, Utils.ColorString(Color.cyan, GetString("FPSGame")) + ((int)FPSGame).ToString()));
            }

            if (Main.ShowTextOverlay.Value)
            {
                if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Overlay.NoGameEnd")));
                if (Options.AllowConsole.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Overlay.AllowConsole")));
                if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, GetString("Overlay.DebugMode")));
                if (Options.LowLoadMode.GetBool()) sb.Append("\r\n").Append(Utils.ColorString(Color.green, GetString("Overlay.LowLoadMode")));
                if (Options.GuesserMode.GetBool()) sb.Append("\r\n").Append(Utils.ColorString(Color.yellow, GetString("Overlay.GuesserMode")));
            }

            var offset_x = 1.2f; //右端からのオフセット
            if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
            if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
            __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

            __instance.text.text = sb.ToString();
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    class VersionShowerStartPatch
    {
        static TextMeshPro SpecialEventText;
        private static void Postfix(VersionShower __instance)
        {
            Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginDisplayVersion}";

#if RELEASE
            //  Main.credentialsText += $"\r\n<color=#a54aff>Modified by </color><color=#ff3b6f>Loonie</color>";
            Main.credentialsText += $"\r\n<color=#a54aff>By <color=#ffc0cb>KARPED1EM</color> & </color><color=#f34c50>Loonie</color>";
#endif

#if DEBUG
         /* string additionalCredentials = GetString("TextBelowVersionText");
            if (additionalCredentials != null && additionalCredentials != "*TextBelowVersionText")
            {
                Main.credentialsText += $"\n{additionalCredentials}";
            } */

            Main.credentialsText += $"\r\n<color=#a54aff>By <color=#ffc0cb>KARPED1EM</color> & </color><color=#f34c50>Loonie</color>";
#endif

            if (Main.IsAprilFools)
                Main.credentialsText = $"\r\n<color=#00bfff>Town Of Host</color> v11.45.14";

            var credentials = Object.Instantiate(__instance.text);
            credentials.text = Main.credentialsText;
            credentials.alignment = TextAlignmentOptions.Right;
            credentials.transform.position = new Vector3(1f, 2.79f, -2f);
            credentials.fontSize = credentials.fontSizeMax = credentials.fontSizeMin = 2f;

            ErrorText.Create(__instance.text);
            if (Main.hasArgumentException && ErrorText.Instance != null)
            {
                ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
            }

            VersionChecker.Check();

            if (SpecialEventText == null && ToheLogo != null)
            {
                SpecialEventText = Object.Instantiate(__instance.text, ToheLogo.transform);
                SpecialEventText.name = "SpecialEventText";
                SpecialEventText.text = "";
                SpecialEventText.color = Color.white;
                SpecialEventText.fontSizeMin = 3f;
                SpecialEventText.alignment = TextAlignmentOptions.Center;
                SpecialEventText.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            }
            if (SpecialEventText != null)
            {
                SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
            }
            if (Main.IsInitialRelease)
            {
                SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                if (ColorUtility.TryParseHtmlString(Main.ModColor, out var col))
                {
                    SpecialEventText.color = col;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class TitleLogoPatch
    {
        public static GameObject amongUsLogo;
        public static GameObject Ambience;
        public static GameObject LoadingHint;

        private static void Postfix(MainMenuManager __instance)
        {
            amongUsLogo = GameObject.Find("LOGO-AU");

            var rightpanel = __instance.gameModeButtons.transform.parent;
            var logoObject = new GameObject("titleLogo_TOHE");
            var logoTransform = logoObject.transform;
            ToheLogo = logoObject.AddComponent<SpriteRenderer>();
            logoTransform.parent = rightpanel;
            logoTransform.localPosition = new(-0.16f, 0f, 1f); //new(0f, 0.3f, 1f); new(0f, 0.15f, 1f);
            logoTransform.localScale *= 1.2f;

            if (!Options.IsLoaded)
            {
                LoadingHint = new GameObject("LoadingHint");
                LoadingHint.transform.position = Vector3.down;
                var LoadingHintText = LoadingHint.AddComponent<TextMeshPro>();
                LoadingHintText.text = GetString("Loading");
                LoadingHintText.alignment = TextAlignmentOptions.Center;
                LoadingHintText.fontSize = 2f;
                LoadingHintText.transform.position = amongUsLogo.transform.position;
                LoadingHintText.transform.position += new Vector3 (-0.25f, -0.9f, 0f);
                LoadingHintText.color = new Color32(17, 255, 1, byte.MaxValue);
                __instance.playButton.transform.gameObject.SetActive(false);
            }
            if ((Ambience = GameObject.Find("Ambience")) != null)
            {
                // Show playButton when mod is fully loaded
                if (Options.IsLoaded && LoadingHint != null) __instance.playButton.transform.gameObject.SetActive(true);

                Ambience.SetActive(false);
                //var CustomBG = new GameObject("CustomBG");
                //CustomBG.transform.position = new Vector3(2.095f, -0.25f, 520f);
                //var bgRenderer = CustomBG.AddComponent<SpriteRenderer>();
                //bgRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Background.TOH-Background-Old.jpg", 245f);
            }
        }
    }
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class ModManagerLateUpdatePatch
    {
        public static void Prefix(ModManager __instance)
        {
            __instance.ShowModStamp();

            LateTask.Update(Time.deltaTime);
            CheckMurderPatch.Update();
        }
        public static void Postfix(ModManager __instance)
        {
            var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
            __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
        }
    }
}