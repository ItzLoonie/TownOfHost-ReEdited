using System;
using System.Linq;
using System.Collections;
using System.Threading;
using Discord;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using InnerNet;
using static TOHE.GameStates;
using static TOHE.Translator;
using Epic.OnlineServices.Inventory;
using Sentry.Internal;
using Cpp2IL.Core.Extensions;

namespace TOHE;

/// <summary>
/// Properly working Discord Rich Presence [RP] changer for this mod
/// </summary>
public static partial class DiscordRP
{
	private static Discord.Discord DiscordInterface = null;

	public static void Update(bool immediately = false)
		=> new System.Threading.Tasks.Task(() => _UpdateReal(immediately)).Start();

	private static void _UpdateReal(bool immediately)
	{
		// Discord is not initialized, give up
		if (DiscordInterface == null)
			return;

		// Make the thread run in parallel instead of freezing something
		Thread.CurrentThread.IsBackground = true;

		// Let things have time to update if this is not an instant request
		if (!immediately)
			Thread.Sleep(1000);
			
		string gameCodeText = GameCode.IntToGameName(AmongUsClient.Instance.GameId);

		// Get images and text to use
		string LargeImage = GetString("LargeImageKey");
		string LargeText = GetString("LargeImageText");

		string SmallImage = GetString("SmallImageKey");
		string SmallText = GetString("SmallImageText");

		// Initialize all of the variables of a Discord activity
		long a_ApplicationId = 0L;
		string a_Assets_LargeImage = LargeImage;
		string a_Assets_LargeText = LargeText;
		string a_Assets_SmallImage = SmallImage;
		string a_Assets_SmallText = SmallText;
		string a_Details = null;
		bool a_Instance = false;
		string a_Name = null;
		string a_Party_Id = null;
		ActivityPartyPrivacy a_Party_Privacy = ActivityPartyPrivacy.Private;
		int a_Party_Size_CurrentSize = 0;
		int a_Party_Size_MaxSize = 0;
		uint a_SupportedPlatforms = 7U;
		string a_Secrets_Join = null;
		string a_Secrets_Match = null;
		string a_Secrets_Spectate = null;
		string a_State = null;
		long a_Timestamps_Start = 0L;
		long a_Timestamps_End = 0L;
		ActivityType a_Type = ActivityType.Playing;

		/*
			* Presence format:
			*		- Details
			*		- State (timestamps)
			*/

		// -TODO- Add strings used for Details, State, etc. to `string.csv'

		// Find what we need to do according to Innersloth's GameState
		switch (AmongUsClient.Instance.GameState)
		{
			// We are in the main menu
			case InnerNetClient.GameStates.NotJoined:
				a_Details = "Creating a game";
				a_State = "In Menus";
				break;

			// We are in a lobby or in game
			case InnerNetClient.GameStates.Joined:
			case InnerNetClient.GameStates.Started:
				// Do things according to ToHE's state

				if (GameStates.IsInTask)
					// In game, not in meeting
					a_State = "Playing";

				if (GameStates.IsCountDown)
					// Counting down to game start
					a_State = "Starting in: " + GameStartManager.Instance.countDownTimer + "s";

				if (a_Details == null)
					// Nothing smarter to say
					switch (AmongUsClient.Instance.NetworkMode)
					{
						case NetworkModes.LocalGame:
							a_Details = "Playing a local game";
							break;
						case NetworkModes.OnlineGame:
							break;
						case NetworkModes.FreePlay:
							// Make sure to be rude to noobs because why not
							a_Details = "Being a Noob";
							break;

						default:
							// Unknown network mode
							a_Details ??= "ERR";
							break;
					}
				break;

			// Game has ended
			case InnerNetClient.GameStates.Ended:
				a_State = "Game ended";
				break;

			default:
				// Unknown game state
				a_State ??= "ERR";
				break;
		}

		// Additional things to do
		if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.NotJoined)
		{
			if (GameStates.IsLobby)
				a_State = "In lobby";

			if (GameStates.IsMeeting)
			{
				if (a_Party_Size_CurrentSize == 0 && a_Party_Size_MaxSize == 0)
				{
					a_Party_Size_CurrentSize = Utils.AllAlivePlayersCount;
					a_Party_Size_MaxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;
				}
				a_State = "In a meeting";
			}

			if (a_Party_Size_CurrentSize == 0 && a_Party_Size_MaxSize == 0)
			{
				a_Party_Size_CurrentSize = GameStates.IsLobby ? GameStartManager.Instance.LastPlayerCount : Utils.AllPlayersCount;
				a_Party_Size_MaxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;
			}

			// Make sure the payload is valid, because 50/10 is allowed and 0/10 is not. Makes sense, Discord!
			if (a_Party_Size_CurrentSize <= 0 && a_Party_Size_MaxSize > 0)
				a_Party_Size_CurrentSize = 1;

			if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
			{
				a_Details ??= "Code is: " + gameCodeText;
				a_Secrets_Join ??= "join" + gameCodeText;
				a_Secrets_Match ??= "match" + gameCodeText;
				a_Party_Id ??= gameCodeText;
				a_Party_Privacy = ActivityPartyPrivacy.Public;
			}
		}
		
		// Send the rich presence to the Discord client
		DiscordInterface.GetActivityManager().UpdateActivity(new Activity()
		{
			ApplicationId = a_ApplicationId,
			Assets = new ActivityAssets()
			{
				LargeImage = a_Assets_LargeImage,
				LargeText = a_Assets_LargeText,
				SmallImage = a_Assets_SmallImage,
				SmallText = a_Assets_SmallText
			},
			Details = a_Details,
			Instance = a_Instance,
			Name = a_Name,
			Party = new ActivityParty()
			{
				Id = a_Party_Id,
				Privacy = a_Party_Privacy,
				Size = new PartySize()
				{
					CurrentSize = a_Party_Size_CurrentSize,
					MaxSize = a_Party_Size_MaxSize
				}
			},
			SupportedPlatforms = a_SupportedPlatforms,
			Secrets = new ActivitySecrets()
			{
				Join = a_Secrets_Join,
				Match = a_Secrets_Match,
				Spectate = a_Secrets_Spectate
			},
			State = a_State,
			Timestamps = new ActivityTimestamps()
			{
				Start = a_Timestamps_Start,
				End = a_Timestamps_End
			},
			Type = a_Type
		}, (Action<Result>)((Result res) =>
		{
			Logger.Info($"Received response from Discord client: {res}", "DiscordRP");
		}));
	}
}

[HarmonyPatch]
public static partial class DiscordRP
{
	[HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.FixedUpdate))]
	[HarmonyPrefix]
	public static bool FixedUpdatePrefix()
	{
		DiscordInterface.RunCallbacks();
		return false;
	}

	[HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.Start))]
	[HarmonyPrefix]
	public static bool CustomDiscordRPCPrefix(DiscordManager __instance)
	{
		try
		{
			/*string discordAppIdStr = GetString("DiscordAppID")"sus, this shouldn't parse";

			long discordAppId;
			if (!long.TryParse(discordAppIdStr, out discordAppId))
				discordAppId = 1111023738197119020; // 1111023738197119020

			*/
			DiscordInterface = new Discord.Discord(1111023738197119020L, (ulong)CreateFlags.NoRequireDiscord);
			DiscordInterface.SetLogHook(LogLevel.Debug, (Action<LogLevel, string>)((LogLevel l, string s) =>
			{
				Logger.Error($" [{l}] {s}", "Discord Log - CustomDiscordManager");
			}));
			__instance.presence = null;

			ActivityManager activityManager = DiscordInterface.GetActivityManager();
			activityManager.RegisterSteam(945360U); // Among Us' AppID on steam
				
			activityManager.OnActivityJoin = (Action<string>)((string joinSecret) =>
			{
				if (!AmongUsClient.Instance)
				{
					Logger.Warn("Missing AmongUsClient", "CustomDiscordManager");
					return;
				}

				if (!joinSecret.StartsWith("join"))
				{
					Logger.Warn($"Invalid join secret: {joinSecret}", "CustomDiscordManager");
					return;
				}

				string targetGameStr = joinSecret.Substring(4);
				int targetGameCode = GameCode.GameNameToInt(targetGameStr);
				if (AmongUsClient.Instance.GameId == targetGameCode)
				{
					// Since the anticheat became more agressive, we must ignore this request if we're already in the lobby + (Fuck your anticheat Innersloth)
					Logger.Warn($"Got request to join lobby code \"${targetGameStr}\", but we are already in that lobby", "CustomDiscordManager");
					return;
				}

				// Start a coroutine to join the game
				AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoJoinOnlineGameFromCode(targetGameCode));
			});

#region Implementation-Idea-1
			//Thanksforthehelpwithcode,Innersloth,youwereusefulforonce
			/*
			SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)(Action<Scene, LoadSceneMode>)((Scene scene, LoadSceneMode mode) =>
			{
				string name = scene.name;
				if (name == null)
				{
					Logger.Warn("Switched to a null scene?!", "CustomDiscordManager");
					return;
				}

				if (name == "MainMenu" || name == "MatchMaking" || name == "MMOnline" || name == "FindAGame")
					// We are in a menu scene, update
					Update(true);
			});
				*/
#endregion Implementation-Idea-1


			// Cause an update now
			Update(true);
		}
		catch
		{
			// Log an error for this
			Logger.Error("Discord messed up!", "CustomDiscordManager");
		}

		return false;
	}

	// Layer 2 of blocking Among Us' activities
	[HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
	[HarmonyPrefix]
	public static bool Nullifier([HarmonyArgument(0)] ref Activity activity)
	{
		if (activity.Assets.LargeImage == "icon")
		{
			// In the edge case where Among Us bypasses the attempt of disabling their activity, log and block it
			Logger.Warn("Among Us bypassed the blocking attempt...", "CustomDiscordManager");
			return false;
		}
		
		return true;
	}

	[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
	[HarmonyPostfix]
	public static void FixAccountTabPatch(HudManager __instance)
	{
		DestroyableSingleton<AccountTab>.Instance.gameObject.SetActive(false);
	}
}