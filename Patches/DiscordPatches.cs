using System;
using System.Linq;
using Discord;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using InnerNet;
using static TOHE.GameStates;
using static TOHE.Translator;
using Epic.OnlineServices.Inventory;
using Sentry.Internal;

namespace TOHE.Patches
{
    [HarmonyPatch]
    public class DiscordPatches
    {
        [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.Start))]
        [HarmonyPrefix]
        public static bool CustomDiscordRPCPrefix(DiscordManager __instance)
        {
            if (DestroyableSingleton<DiscordManager>.Instance != __instance)
            {
                return false;
            }
            try
            {
           /*     string appidString = GetString("DiscordAppID");
                int appid;
                if (int.TryParse(appidString, out appid))
                {*/
                    __instance.presence = new Discord.Discord(1111023738197119020, 1UL);
           //     }
                ActivityManager activityManager = __instance.presence.GetActivityManager();
                activityManager.RegisterSteam(945360U);
                /*activityManager.OnActivityJoin = (Action<string>)delegate (string joinSecret)
                {
                    if (!joinSecret.StartsWith("join"))
                    {
                        Debug.LogWarning("DiscordManager: Invalid join secret: " + joinSecret);
                        return;
                    }
                    __instance.StopAllCoroutines();
                    __instance.StartCoroutine(__instance.CoJoinGame(joinSecret));
                };*/
                __instance.SetInMenus();
                SceneManager.sceneLoaded = (Action<Scene, LoadSceneMode>)delegate (Scene scene, LoadSceneMode mode)
                {
                    __instance.OnSceneChange(scene.name);
                };
            }
            catch
            {
                Debug.LogWarning("DiscordManager: Discord messed up");
            }

            return false;
        }

        // [Game ID -> String code] converter because the hook down below forced me to make one
        private static string CHAR_SET = "QWXRTYLPESDFGHUJKZOCVBINMA";
		public static string ConvertGameIdToString(int id = 0)
        {
            if (id == 0)
                // Invalid ID
                return null;

            if (id < -1)
            {
				// Game code is V2 - Always a negative number - 6 characters
				int firstTwo = id & 0x3FF;
				int lastFour = (id >> 10) & 0xFFFFF;

				return
					CHAR_SET[firstTwo % 26].ToString() +
					CHAR_SET[firstTwo / 26].ToString() +
					CHAR_SET[lastFour % 26].ToString() +
					CHAR_SET[(lastFour /= 26) % 26].ToString() +
					CHAR_SET[(lastFour /= 26) % 26].ToString() +
					CHAR_SET[lastFour / 26 % 26].ToString();
			}
            else
            {
                // Game code is V1 - Always a positive number - 4 characters
                return
                    new string(
                        System.Text.Encoding.UTF8.GetChars(
                            BitConverter.GetBytes(id) ));
            }
        }

        // Attempt #2 to make a custom activity for this mod
        [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
        [HarmonyPrefix]
        public static void SussyTestV2([HarmonyArgument(0)] ref Activity activity)
        {
            // Make sure activity is not null (just in case)
            if (activity == null) return;

            // Get the images and text
            string LargeImage     = GetString("LargeImageKey");
            string LargeImageText = GetString("LargeImageText");

            string SmallImage     = GetString("SmallImageKey");
            string SmallImageText = GetString("SmallImageText");
            //TODO: Add to 'string.csv'

            // Set assets and text
            activity.Assets = new ActivityAssets
            {
                LargeImage  = $"{LargeImage}",
                LargeText   = $"{LargeImageText}",
                SmallImage  = $"{SmallImage}",
                SmallText   = $"{SmallImageText}"
            };

            /*
             * Presence format:
             *          - Details
             *          - State (timestamp)
             */

            // Find what we need to do according to the GameState of AmongUsClient
            switch (AmongUsClient.Instance.GameState)
            {
                // The player is in the main menu
                case InnerNetClient.GameStates.NotJoined:
                    activity.Details = "Creating a Game";
                    activity.State = "In Menus";
                    break;

                // The player is in a lobby
                case InnerNetClient.GameStates.Joined:
                case InnerNetClient.GameStates.Started:
                    // Defaults - Start
                    activity.Details = null;
                    activity.State   = null;
                    // Defaults - End


                    if (GameStates.IsMeeting)
                    {
                        ActivityParty party = activity.Party;
                        // Format: 'CurrentSize/MaxSize'
                        party.Size = new PartySize
                        {
                            CurrentSize = Utils.AllAlivePlayersCount,
                            MaxSize     = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers
                        };
                        activity.Party = party;

                        activity.State = "In a meeting";
                    }

                    if (GameStates.IsInTask)
                        // We are not in a meeting
                        activity.State = "Playing";

                    if (GameStates.IsCountDown)
                        // We are counting down to the start
                        activity.State = "Starting in: " + GameStartManager.Instance.countDownTimer + "sec"; //TODO:: Find a way to use ONLY in specific time (8 , 4 ,2).

                    if (activity.Details == null)
                        // We don't have anything important to overwrite this
                        switch (AmongUsClient.Instance.NetworkMode)
                        {
                            case NetworkModes.LocalGame:
                                activity.Details = "Playing a local game";
                                break;
                            case NetworkModes.OnlineGame:
                                activity.Details = "Game code: " + ConvertGameIdToString(AmongUsClient.Instance.GameId);
                                break;
                            case NetworkModes.FreePlay:
                                // Make sure to be rude to noobs that are playing with dummies
                                activity.Details = "Being a Noob ♥";
                                break;

                            // Handle something to fill up empty space if nothing else handled this yet
                            default:
                                activity.Details = "Playing a new update";
                                break;
						}
                    break;

                case InnerNetClient.GameStates.Ended:
                    activity.Details = "Game ended;";
                    activity.State = "Playing again.";
                    break;

                default:
                    activity.Details = "Playing a new";
                    activity.State = "Among Us update";
                    break;
            }
        }

   //     [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
   //     [HarmonyPrefix]
   //     public static void SussyTest([HarmonyArgument(0)] ref Discord.Activity activity)
   //     {
   //         // Make sure the activity is not null to not cause any runtime errors
   //         if (activity == null) return;

   //         // Translate the images (?..) and associated text
   //         string LargeImage = GetString("LargeImageKey");
   //         string LargeImageText = GetString("LargeImageText");

   //         string SmallImage = GetString("SmallImageKey");
   //         string SmallImageText = GetString("SmallImageText");
   //         //TODO:: string.csv for these.
			//// Set assets and the text for them
			//activity.Assets = new ActivityAssets
   //         {
   //             LargeImage = $"{LargeImage}",
   //             LargeText = $"{LargeImageText}",
   //             SmallImage = $"{SmallImage}",
   //             SmallText = $"{SmallImageText}" //preferably empty.
   //         };
            
   //         // Presence format:
   //         // Details - First line
   //         // State - Second line, along with player count
   //         // Find what we need to set the presence to and do it
   //         switch (AmongUsClient.Instance.GameState)
   //         {
   //             case InnerNetClient.GameStates.NotJoined:
   //                 // The player is in the main menu
   //                 activity.Details = "Creating a Game";
   //                 activity.State = "In Menus";
   //                 break;

   //             case InnerNetClient.GameStates.Joined:
   //                 ActivityParty party = activity.Party;

   //                 // Defaults ---
   //                // activity.State = null;
   //                 activity.Details = null;
   //                 // Defaults ---
   //                 if (GameStates.IsMeeting)
   //                 {
   //                     party.Size = new PartySize
   //                     {
   //                         CurrentSize = Utils.AllAlivePlayersCount,
   //                         MaxSize = Utils.AllPlayersCount
   //                     };
   //                     activity.State = "In a meeting";
   //                 }

   //                 if (GameStates.IsInTask)
   //                 {
   //                     // We are not in meeting
   //                     activity.State = "In game";
   //                 }

   //                 if (GameStates.IsCountDown && GameStartManager.InstanceExists)
   //                 {
   //                     activity.State = "Starting in: " + GameStartManager.Instance.countDownTimer;
   //                 }

   //                 else if (activity.Details == null)
   //                     // If we have nothing better to say, just say the current state.
   //                     switch (AmongUsClient.Instance.NetworkMode)
   //                     {
   //                         case NetworkModes.LocalGame:
   //                             activity.Details = "Playing a local game";
   //                             break;
   //                         case NetworkModes.OnlineGame:
   //                             activity.Details = !GameStartManager.InstanceExists ? null : "Game code: " + GameStartManager.Instance.GameRoomNameCode?.text;
   //                             break;
   //                         case NetworkModes.FreePlay:
   //                             // Make sure to be offensive to noobs that are just practicing with dummies
   //                             activity.Details = ;
   //                             activity.State = "Unforgivably";
   //                             break;

   //                         default:
   //                             activity.Details = "Playing a new";
   //                             activity.State = "Among Us Update";
   //                             break;
   //                     }

   //                 activity.Party = party;
   //                 break;

   //             case InnerNetClient.GameStates.Started:
   //                 activity.Details = "Game code: " + GameStartManager.Instance.GameRoomNameCode?.text;
   //                 break;

   //             case InnerNetClient.GameStates.Ended:
   //                 activity.Details = "Playing again,";
   //                 activity.State = "game ended.";
   //                 break;

   //             default:
			//		activity.Details = "Playing a new";
			//		activity.State = "Among Us Update";
			//		break;

			//		// --------------------------------------------------------------------
			//		// We should try this out after commenting out the code below
			//		// --------------------------------------------------------------------
			//}

            /*
                        // ---------------------------
                        // Main menu
                        if (GameStates.IsNotJoined)
                        {
                            activity.State = "Main menu";
                            activity.Details = "Nothing interesting to see here";
                        }

                        // A lobby was created, lobby:
                        if (GameStates.IsLobby)
                        {
                            activity.Details = "Code Is: " + GameStartManager.Instance.GameRoomNameCode?.text; //this works perfectly.
             //             activity.State = "Default one";
                        }
                        if (GameStates.IsInTask)
                        {
                            activity.Details = "Code Is: " + GameStartManager.Instance.GameRoomNameCode?.text;
                        }
                        if (GameStates.IsMeeting)
                        {
                            activity.Details = "Code Is: " + GameStartManager.Instance.GameRoomNameCode?.text;

                            // Template to change active code (Fix attempt V69)

                             * PartySize temporaryVariableBecauseDotnetIsDumb = activity.Party.Size;
                             * 
                             * // Set the properties
                             * temporaryVariableBecauseDotnetIsDumb.CurrentSize = Utils.AllAlivePlayerCount;
                             * 
                             * activity.Party.Size = temporaryVariableBecauseDotnetIsDumb;
                             */

            /*
            // Fix attempt - V420
            PartySize size = activity.Party.Size;

            // Set sizes
            size.CurrentSize = Utils.AllAlivePlayersCount;
            size.MaxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers; 
            // End set sizes

            activity.Party.Size = size;
           

            // isMeeting doesnt work. at all, isintask doesnt work either.
            // Islobby worked perfectly.
            //party sizes didnt work either.

            //Backup  code test if above fails.
            activity.Party.Size = new PartySize
                {
                    CurrentSize = Utils.AllAlivePlayersCount,
                    MaxSize = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers
                };
                
            } */
            // ---------------------------




            /*
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.NotJoined)
                activity.Details = "Testing GameSDK";
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined)
                activity.Details = "Game code: " + GameStartManager.Instance.GameRoomNameCode?.text;


			 * DestroyableSingleton<DiscordManager>.Instance.presence.GetActivityManager().UpdateActivity(activity, (Action<Discord.Result>)delegate (Discord.Result res)
			 * {
             *  Logger.Warn(res.ToString(), "test", true, 0, "Test");
			 * });
			 */
        //}
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void FixAccountTabPatch(HudManager __instance)
        {
            DestroyableSingleton<AccountTab>.Instance.gameObject.SetActive(false);
        }
    }
}

//ill leave it open, type ur notes here if u finish so i know where to continue!
// ima make a v2 attempt