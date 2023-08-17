using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(MainMenuManager))]
public class MainMenuManagerPatch
{
    private static PassiveButton template;
    private static PassiveButton gitHubButton;
    private static PassiveButton discordButton;
    private static PassiveButton websiteButton;

    [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
    public static void StartPostfix(MainMenuManager __instance)
    {
        if (template == null) template = __instance.quitButton;
        if (template == null) return;


        // GitHub Button
        if (gitHubButton == null)
        {
            gitHubButton = CreateButton(
                "GitHubButton",
                new(-1.8f, -1.4f, 1f),
                new(153, 153, 153, byte.MaxValue),
                new(209, 209, 209, byte.MaxValue),
                () => Application.OpenURL(Main.GitHubInviteUrl),
                GetString("GitHub")); //"GitHub"
        }
        gitHubButton.gameObject.SetActive(Main.ShowGitHubButton);

        // Discord Button
        if (discordButton == null)
        {
            discordButton = CreateButton(
                "DiscordButton",
                new(-1.8f, -1.8f, 1f),
                new(88, 101, 242, byte.MaxValue),
                new(148, 161, byte.MaxValue, byte.MaxValue),
                () => Application.OpenURL(Main.DiscordInviteUrl),
                GetString("Discord")); //"Discord"
        }
        discordButton.gameObject.SetActive(Main.ShowDiscordButton);

        // Website Button
        if (websiteButton == null)
        {
            websiteButton = CreateButton(
                "WebsiteButton",
                new(-1.8f, -2.2f, 1f),
                new(251, 81, 44, byte.MaxValue),
                new(211, 77, 48, byte.MaxValue),
                () => Application.OpenURL(Main.WebsiteInviteUrl),
                GetString("Website")); //"Website"
        }
        websiteButton.gameObject.SetActive(Main.ShowWebsiteButton);


        var howToPlayButton = __instance.howToPlayButton;
        var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");
        
        if (freeplayButton != null) freeplayButton.gameObject.SetActive(false);

        howToPlayButton.transform.SetLocalX(0);


        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
    }

    private static PassiveButton CreateButton(string name, Vector3 localPosition, Color32 normalColor, Color32 hoverColor, Action action, string label, Vector2? scale = null)
    {
        var button = Object.Instantiate(template, Credentials.ToheLogo.transform);
        button.name = name;
        Object.Destroy(button.GetComponent<AspectPosition>());
        button.transform.localPosition = localPosition;

        button.OnClick = new();
        button.OnClick.AddListener(action);

        var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
        buttonText.DestroyTranslator();
        buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.5f;
        buttonText.enableWordWrapping = false;
        buttonText.text = label;
        var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
        var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
        normalSprite.color = normalColor;
        hoverSprite.color = hoverColor;

        var container = buttonText.transform.parent;
        Object.Destroy(container.GetComponent<AspectPosition>());
        Object.Destroy(buttonText.GetComponent<AspectPosition>());
        container.SetLocalX(0f);
        buttonText.transform.SetLocalX(0f);
        buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

        var buttonCollider = button.GetComponent<BoxCollider2D>();
        if (scale.HasValue)
        {
            normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
        }

        buttonCollider.offset = new(0f, 0f);

        return button;
    }

    [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
    [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
    [HarmonyPostfix]
    public static void OpenMenuPostfix()
    {
        if (Credentials.ToheLogo != null) Credentials.ToheLogo.gameObject.SetActive(false);
    }
    [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
    public static void ResetScreenPostfix()
    {
        if (Credentials.ToheLogo != null) Credentials.ToheLogo.gameObject.SetActive(true);
    }
}

// 来源：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/HorseModePatch.cs
[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldHorseAround))]
public static class HorseModePatch
{
    public static bool isHorseMode = false;
    public static bool Prefix(ref bool __result)
    {
        __result = isHorseMode;
        return false;
    }
}
[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldFlipSkeld))]
public static class DleksPatch
{
    public static bool isDleks = false;
    public static bool Prefix(ref bool __result)
    {
        __result = isDleks;
        return false;
    }
}