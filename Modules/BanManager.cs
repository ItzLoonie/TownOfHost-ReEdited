using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static TOHE.Translator;

namespace TOHE;

public static class BanManager
{
    private static readonly string DENY_NAME_LIST_PATH = @"./TOHE_DATA/DenyName.txt";
    private static readonly string BAN_LIST_PATH = @"./TOHE_DATA/BanList.txt";
    private static readonly string HARD_BAN_LIST_PATH = @"./TOHE_DATA/HardBanList.txt";
    private static readonly string MODERATOR_LIST_PATH = @"./TOHE_DATA/Moderators.txt";
    private static readonly string AllowlistFilePath = @"./TOHE_DATA/Allowlist.txt";
    private static List<string> EACList = new();
    public static void Init()
    {
        try
        {
            Directory.CreateDirectory("TOHE_DATA");

            if (!File.Exists(BAN_LIST_PATH))
            {
                Logger.Warn("创建新的 BanList.txt 文件", "BanManager");
                File.Create(BAN_LIST_PATH).Close();
            }
            if (!File.Exists(DENY_NAME_LIST_PATH))
            {
                Logger.Warn("创建新的 DenyName.txt 文件", "BanManager");
                File.Create(DENY_NAME_LIST_PATH).Close();
                File.WriteAllText(DENY_NAME_LIST_PATH, GetResourcesTxt("TOHE.Resources.Config.DenyName.txt"));
            }
            if (!File.Exists(MODERATOR_LIST_PATH))
                            {
                Logger.Warn("Creating a new Moderators.txt file", "BanManager");
                File.Create(MODERATOR_LIST_PATH).Close();
                File.WriteAllText(MODERATOR_LIST_PATH, GetResourcesTxt("TOHE.Resources.Config.Moderators.txt"));
                            }
            if (!File.Exists(AllowlistFilePath))
            {
                Logger.Warn("Creating a new Allowlist.txt file", "BanManager");
                File.Create(AllowlistFilePath).Close();
                File.WriteAllText(AllowlistFilePath, GetResourcesTxt("TOHE.Resources.Config.Allowlist.txt"));
            }
            if (!File.Exists(AllowlistFilePath))
            {
                Logger.Warn("Creating a new HardBanList.txt file", "BanManager");
                Directory.CreateDirectory(Path.GetDirectoryName(HARD_BAN_LIST_PATH));
                File.Create(HARD_BAN_LIST_PATH).Close();
                File.WriteAllText(HARD_BAN_LIST_PATH, GetResourcesTxt("TOHE.Resources.Config.HardBanList.txt"));
            }

            //读取EAC名单
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TOHE.Resources.Config.EACList.txt");
            stream.Position = 0;
            using StreamReader sr = new(stream, Encoding.UTF8);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "" || line.StartsWith("#")) continue;
                EACList.Add(line);
            }

        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "BanManager");
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
  
    public static void AddBanPlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        string something = Something.GetSomethingByClient(player);
		if (!CheckBanList(something) && !string.IsNullOrWhiteSpace(something))
        {
            File.AppendAllText(HARD_BAN_LIST_PATH, $"{something},{player.PlayerName}\n");
            Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
        }
    }
    public static void CheckDenyNamePlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.ApplyDenyNameList.GetBool()) return;
        try
        {
            Directory.CreateDirectory("TOHE_DATA");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            using StreamReader sr = new(DENY_NAME_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (Regex.IsMatch(player.PlayerName, line))
                {
                    AmongUsClient.Instance.KickPlayer(player.Id, false);
                    Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                    Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckDenyNamePlayer");
        }
    }
    public static void CheckBanPlayer(InnerNet.ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.ApplyBanList.GetBool()) return;
		string something = Something.GetSomethingByClient(player);

        bool wasBannedBySomething = CheckBanList(something, true);
        bool wasBannedByFriendCode = CheckBanList(player.FriendCode);
        
		if (wasBannedBySomething || wasBannedByFriendCode)
        {
            // Migrate your account because Dinnerbone said so.
            if (wasBannedByFriendCode && !wasBannedBySomething)
                AddBanPlayer(player);

            AmongUsClient.Instance.KickPlayer(player.Id, true);
            Logger.SendInGame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
            return;
        }
        if (CheckEACList(player?.FriendCode))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            Logger.SendInGame(string.Format(GetString("Message.BanedByEACList"), player.PlayerName));
            Logger.Info($"{player.PlayerName}存在于EAC封禁名单", "BAN");
            return;
        }
    }
    public static bool CheckBanList(string code, bool bySomething = false)
    {
        if (code == "") return false;
        string banListPath = bySomething ? HARD_BAN_LIST_PATH : BAN_LIST_PATH;

        try
        {
            Directory.CreateDirectory("TOHE_DATA");
            if (!File.Exists(banListPath)) File.Create(banListPath).Close();
            using StreamReader sr = new(banListPath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (line.Contains(code)) return true;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "CheckBanList");
        }
        return false;
    }
    public static bool CheckEACList(string code)
    {
        if (code == "") return false;
        return EACList.Any(x => x.Contains(code));
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        InnerNet.ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (!BanManager.CheckBanList(Something.GetSomethingByClient(recentClient), true) && !BanManager.CheckBanList(recentClient?.FriendCode)) __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
    }
}