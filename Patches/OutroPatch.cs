using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
class EndGamePatch
{
    public static Dictionary<byte, string> SummaryText = new();
    public static string KillLog = "";
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        GameStates.InGame = false;

        Logger.Info("-----------游戏结束-----------", "Phase");
        if (!GameStates.IsModHost) return;
        SummaryText = new();
        foreach (var id in Main.PlayerStates.Keys)
            SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);

        var sb = new StringBuilder(GetString("KillLog") + ":");
        foreach (var kvp in Main.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
        {
            var date = kvp.Value.RealKiller.Item1;
            if (date == DateTime.MinValue) continue;
            var killerId = kvp.Value.GetRealKiller();
            var targetId = kvp.Key;
            sb.Append($"\n{date:T} {Main.AllPlayerNames[targetId]}({Utils.GetDisplayRoleName(targetId, true)}{Utils.GetSubRolesText(targetId, summary: true)}) [{Utils.GetVitalText(kvp.Key)}]");
            if (killerId != byte.MaxValue && killerId != targetId)
                sb.Append($"\n\t⇐ {Main.AllPlayerNames[killerId]}({Utils.GetDisplayRoleName(killerId, true)}{Utils.GetSubRolesText(killerId, summary: true)})");
        }
        KillLog = sb.ToString();
        if (!KillLog.Contains("\n")) KillLog = "";

        Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
        //winnerListリセット
        TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
        var winner = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winner.Add(pc);
        }
        foreach (var team in CustomWinnerHolder.WinnerRoles)
        {
            winner.AddRange(Main.AllPlayerControls.Where(p => p.Is(team) && !winner.Contains(p)));
        }

        Main.winnerNameList = new();
        Main.winnerList = new();
        foreach (var pc in winner)
        {
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

            TempData.winners.Add(new WinningPlayerData(pc.Data));
            Main.winnerList.Add(pc.PlayerId);
            Main.winnerNameList.Add(pc.GetRealName());
        }

        BountyHunter.ChangeTimer = new();
        Main.isDoused = new Dictionary<(byte, byte), bool>();
        Main.isDraw = new Dictionary<(byte, byte), bool>();

        Main.VisibleTasksCount = false;
        if (AmongUsClient.Instance.AmHost)
        {
            Main.RealOptionsData.Restore(GameOptionsManager.Instance.CurrentGameOptions);
            GameOptionsSender.AllSenders.Clear();
            GameOptionsSender.AllSenders.Add(new NormalGameOptionsSender());
            /* Send SyncSettings RPC */
        }
    }
}
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
class SetEverythingUpPatch
{
    public static string LastWinsText = "";
    public static string LastWinsReason = "";

    public static void Postfix(EndGameManager __instance)
    {
        if (!Main.playerVersion.ContainsKey(0)) return;
        //#######################################
        //          ==勝利陣営表示==
        //#######################################

        __instance.WinText.alignment = TMPro.TextAlignmentOptions.Right;
        var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x + 2.4f, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
        WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);
        var WinnerText = WinnerTextObject.GetComponent<TMPro.TextMeshPro>(); //WinTextと同じ型のコンポーネントを取得
        WinnerText.fontSizeMin = 3f;
        WinnerText.text = "";

        string CustomWinnerText = "";
        string AdditionalWinnerText = "";
        string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);

        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
            __instance.WinText.text = Main.AllPlayerNames[winnerId] + GetString("Win");
            __instance.WinText.fontSize -= 5f;
            __instance.WinText.color = Main.PlayerColors[winnerId];
            __instance.BackgroundBar.material.color = new Color32(245, 82, 82, 255);
            WinnerText.text = $"<color=#f55252>{GetString("ModeSoloKombat")}</color>";
            WinnerText.color = Color.red;
            goto EndOfText;
        }

        var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
        if (winnerRole >= 0)
        {
            CustomWinnerText = Utils.GetRoleName(winnerRole);
            CustomWinnerColor = Utils.GetRoleColorCode(winnerRole);
            if (winnerRole.IsNeutral())
            {
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
            }
        }
        if (AmongUsClient.Instance.AmHost && Main.PlayerStates[0].MainRole == CustomRoles.GM)
        {
            __instance.WinText.text = GetString("GameOver");
            __instance.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
            __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.GM);
        }
        switch (CustomWinnerHolder.WinnerTeam)
        {
            //通常勝利
            case CustomWinner.Crewmate:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                break;
            //特殊勝利
            case CustomWinner.Terrorist:
                __instance.Foreground.material.color = Color.red;
                break;
            case CustomWinner.Lovers:
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
                break;
            //引き分け処理
            case CustomWinner.Draw:
                __instance.WinText.text = GetString("ForceEnd");
                __instance.WinText.color = Color.white;
                __instance.BackgroundBar.material.color = Color.gray;
                WinnerText.text = GetString("ForceEndText");
                WinnerText.color = Color.gray;
                break;
            //全滅
            case CustomWinner.None:
                __instance.WinText.text = "";
                __instance.WinText.color = Color.black;
                __instance.BackgroundBar.material.color = Color.gray;
                WinnerText.text = GetString("EveryoneDied");
                WinnerText.color = Color.gray;
                break;
        }

        foreach (var additionalWinners in CustomWinnerHolder.AdditionalWinnerTeams)
        {
            var addWinnerRole = (CustomRoles)additionalWinners;
            AdditionalWinnerText += "＆" + Utils.ColorString(Utils.GetRoleColor(addWinnerRole), Utils.GetRoleName(addWinnerRole));
        }
        if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
        {
            WinnerText.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
        }

    EndOfText:

        LastWinsText = WinnerText.text.RemoveHtmlTags();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //#######################################
        //           ==最終結果表示==
        //#######################################

        var Pos = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
        var RoleSummaryObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        RoleSummaryObject.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, Pos.y - 0.1f, -15f);
        RoleSummaryObject.transform.localScale = new Vector3(1f, 1f, 1f);

        StringBuilder sb = new($"{GetString("RoleSummaryText")}");
        List<byte> cloneRoles = new(Main.PlayerStates.Keys);
        foreach (var id in Main.winnerList)
        {
            if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
            sb.Append($"\n<color={CustomWinnerColor}>★</color> ").Append(EndGamePatch.SummaryText[id]);
            cloneRoles.Remove(id);
        }
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            List<(int, byte)> list = new();
            foreach (var id in cloneRoles) list.Add((SoloKombatManager.GetRankOfScore(id), id));
            list.Sort();
            foreach (var id in list)
                sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id.Item2]);
        }
        else
        {
            foreach (var id in cloneRoles)
            {
                if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
                sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
            }
        }
        var RoleSummary = RoleSummaryObject.GetComponent<TMPro.TextMeshPro>();
        RoleSummary.alignment = TMPro.TextAlignmentOptions.TopLeft;
        RoleSummary.color = Color.white;
        RoleSummary.outlineWidth *= 1.2f;
        RoleSummary.fontSizeMin = RoleSummary.fontSizeMax = RoleSummary.fontSize = 1.25f;

        var RoleSummaryRectTransform = RoleSummary.GetComponent<RectTransform>();
        RoleSummaryRectTransform.anchoredPosition = new Vector2(Pos.x + 3.5f, Pos.y - 0.1f);
        RoleSummary.text = sb.ToString();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Utils.ApplySuffix();
    }
}