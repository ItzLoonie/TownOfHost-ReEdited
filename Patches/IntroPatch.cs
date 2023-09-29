using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Linq;
using System.Threading.Tasks;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
class SetUpRoleTextPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsModHost) return;
        _ = new LateTask(() =>
        {
            CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

            if (!role.IsVanilla())
            {
                __instance.YouAreText.color = Utils.GetRoleColor(role);
                __instance.RoleText.text = Utils.GetRoleName(role);
                __instance.RoleText.color = Utils.GetRoleColor(role);
                __instance.RoleBlurbText.color = Utils.GetRoleColor(role);
                __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
            }

            foreach (var subRole in Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SubRoles)
                __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));

            if (!PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && !PlayerControl.LocalPlayer.Is(CustomRoles.Ntr) && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), GetString($"{CustomRoles.Lovers}Info"));

            __instance.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId, false, true);

        }, 0.01f, "Override Role Text");
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
class CoBeginPatch
{
    public static void Prefix()
    {
        var logger = Logger.Handler("Info");
        logger.Info("------------显示名称------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
            pc.cosmetics.nameText.text = pc.name;
        }
        logger.Info("------------职业分配------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
        }
        logger.Info("------------运行环境------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            try
            {
                var text = pc.AmOwner ? "[*]" : "   ";
                text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                    text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                else text += ":Vanilla";
                logger.Info(text);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Platform");
            }
        }
        logger.Info("------------基本设置------------");
        var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
        foreach (var t in tmp) logger.Info(t);
        logger.Info("------------详细设置------------");
        foreach (var o in OptionItem.AllOptions)
            if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.GetBool()))
                logger.Info($"{(o.Parent == null ? o.GetName(true, true).RemoveHtmlTags().PadRightV2(40) : $"┗ {o.GetName(true, true).RemoveHtmlTags()}".PadRightV2(41))}:{o.GetString().RemoveHtmlTags()}");
        logger.Info("-------------其它信息-------------");
        logger.Info($"玩家人数: {Main.AllPlayerControls.Count()}");
        Main.AllPlayerControls.Do(x => Main.PlayerStates[x.PlayerId].InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

        Utils.NotifyRoles();

        GameStates.InGame = true;
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral) && !PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
    //        __instance.BeginImpostor(teamToDisplay);
    //        __instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral) && !PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
     //       __instance.BeginImpostor(teamToDisplay);
     //       __instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            __instance.BeginImpostor(teamToDisplay);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
         else if (PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
    //        __instance.BeginImpostor(teamToDisplay);
      //      __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
         else if (PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
    //        __instance.BeginImpostor(teamToDisplay);
      //      __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
         else if (PlayerControl.LocalPlayer.GetCustomRole().IsMadmate())
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
          //  __instance.BeginImpostor(teamToDisplay);
        //    __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Executioner))
            {
                var exeTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                exeTeam.Add(PlayerControl.LocalPlayer);
                foreach (var execution in Executioner.Target)
                {
                    PlayerControl executing = Utils.GetPlayerById(execution.Value);
                    exeTeam.Add(executing);
                }
                teamToDisplay = exeTeam;
            }
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Lawyer))
            {
                var lawyerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                lawyerTeam.Add(PlayerControl.LocalPlayer);
                foreach (var help in Lawyer.Target)
                {
                    PlayerControl helping = Utils.GetPlayerById(help.Value);
                    lawyerTeam.Add(helping);
                }
                teamToDisplay = lawyerTeam;
            }
        if (PlayerControl.LocalPlayer.Is(CustomRoles.NSerialKiller))
            {
                var serialkillerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                serialkillerTeam.Add(PlayerControl.LocalPlayer);
                foreach (var ar in PlayerControl.AllPlayerControls)
                {
                    if (ar.Is(CustomRoles.NSerialKiller) && ar != PlayerControl.LocalPlayer)
                        serialkillerTeam.Add(ar);
                }
                teamToDisplay = serialkillerTeam;
        }
    /*    if (PlayerControl.LocalPlayer.GetCustomRole().IsCoven())  // Likely will be repurposed
            {
                var covenTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                covenTeam.Add(PlayerControl.LocalPlayer);
                foreach (var ar in PlayerControl.AllPlayerControls)
                {
                    if (ar.GetCustomRole().IsCoven() && ar != PlayerControl.LocalPlayer)
                        covenTeam.Add(ar);
                }
                teamToDisplay = covenTeam;
        }*/
       
        return true;
    }
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        //チーム表示変更
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        __instance.ImpostorText.gameObject.SetActive(false);
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                __instance.TeamTitle.text = GetString("TeamImpostor");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Impostor");
                break;
            case CustomRoleTypes.Crewmate:
                __instance.TeamTitle.text = GetString("TeamCrewmate");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(140, 255, 255, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Crewmate");
                break;
            case CustomRoleTypes.Neutral:
            //    if (!PlayerControl.LocalPlayer.GetCustomRole().IsCoven())
                __instance.TeamTitle.text = GetString("TeamNeutral");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(127, 140, 141, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Neutral");
                break;
        }
        switch (role)
        {
            case CustomRoles.Terrorist:
                var sound = ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault()
                .MinigamePrefab.OpenSound;
                PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                break;

            case CustomRoles.Workaholic:
            case CustomRoles.Snitch:
            case CustomRoles.TaskManager:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                break;

            case CustomRoles.Opportunist:
            case CustomRoles.FFF:
            case CustomRoles.Revolutionist:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;

            case CustomRoles.EngineerTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Engineer);
                break;

            case CustomRoles.SabotageMaster:
            case CustomRoles.Provocateur:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                break;

            case CustomRoles.Doctor:
            case CustomRoles.Medic:
            case CustomRoles.ScientistTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Scientist);
                break;

            case CustomRoles.GM:
                __instance.TeamTitle.text = Utils.GetRoleName(role);
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                __instance.ImpostorText.gameObject.SetActive(false);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                break;
            case CustomRoles.Sheriff:
            case CustomRoles.Veteran:
            case CustomRoles.SwordsMan:
            case CustomRoles.Minimalism:
            case CustomRoles.Reverie:
            case CustomRoles.NiceGuesser:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = PlayerControl.LocalPlayer.KillSfx;
                break;
            case CustomRoles.Swooper:
            case CustomRoles.Wraith:
            case CustomRoles.Chameleon:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = PlayerControl.LocalPlayer.MyPhysics.ImpostorDiscoveredSound;
                break;
        /*    case CustomRoles.ParityCop:
            case CustomRoles.Mediumshiper:
            case CustomRoles.Mayor:
            case CustomRoles.Dictator:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = HudManager.Instance.Chat.messageSound;
                break; */
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            __instance.TeamTitle.text = GetString("TeamMadmate");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Madmate");
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            __instance.TeamTitle.text = GetString("TeamMadmate");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Madmate");
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            __instance.TeamTitle.text = GetString("TeamMadmate");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Madmate");
        }
    /*    if (PlayerControl.LocalPlayer.GetCustomRole().IsCoven())
        {
                __instance.TeamTitle.text = GetString("TeamCoven");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(102, 51, 153, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Coven");
        } */

        if (Input.GetKey(KeyCode.RightShift))
        {
            __instance.TeamTitle.text = "明天就跑路啦";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "嘿嘿嘿嘿嘿嘿";
            __instance.TeamTitle.color = Color.cyan;
            StartFadeIntro(__instance, Color.cyan, Color.yellow);
        }
        if (Input.GetKey(KeyCode.RightControl))
        {
            __instance.TeamTitle.text = "警告";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "请远离无知的玩家";
            __instance.TeamTitle.color = Color.magenta;
            StartFadeIntro(__instance, Color.magenta, Color.magenta);
        }
    }
    public static AudioClip GetIntroSound(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
    }
    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        await Task.Delay(1000);
        int milliseconds = 0;
        while (true)
        {
            await Task.Delay(20);
            milliseconds += 20;
            float time = milliseconds / (float)500;
            Color LerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                Logger.Info("ループを終了します", "StartFadeIntro");
                break;
            }
            __instance.BackgroundBar.material.color = LerpingColor;
        }
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
class BeginImpostorPatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        var role = PlayerControl.LocalPlayer.GetCustomRole();
        if (role is CustomRoles.Crewpostor)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (role is CustomRoles.Sheriff or CustomRoles.Jailer or CustomRoles.SwordsMan or CustomRoles.Medic or CustomRoles.Counterfeiter or CustomRoles.Witness or CustomRoles.Monarch or CustomRoles.Farseer or CustomRoles.Reverie or CustomRoles.Admirer or CustomRoles.Deputy or CustomRoles.Crusader or CustomRoles.CopyCat)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner)) yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = Palette.CrewmateBlue;
            return false;
        }
        else if (role is CustomRoles.Romantic or CustomRoles.Doppelganger or CustomRoles.Pyromaniac or CustomRoles.Huntsman or CustomRoles.RuthlessRomantic or CustomRoles.VengefulRomantic or CustomRoles.NSerialKiller or CustomRoles.Jackal or CustomRoles.Seeker or CustomRoles.Agitater or CustomRoles.Occultist or CustomRoles.Shade or CustomRoles.CursedSoul or CustomRoles.Pirate or CustomRoles.Amnesiac or CustomRoles.Arsonist or CustomRoles.Sidekick or CustomRoles.Innocent or CustomRoles.Pelican or CustomRoles.Pursuer or CustomRoles.Revolutionist or CustomRoles.FFF or CustomRoles.Gamer or CustomRoles.Glitch or CustomRoles.Juggernaut or CustomRoles.DarkHide or CustomRoles.Provocateur or CustomRoles.BloodKnight or CustomRoles.NSerialKiller or CustomRoles.Werewolf or CustomRoles.Maverick or CustomRoles.NWitch or CustomRoles.Shroud or CustomRoles.Totocalcio or CustomRoles.Succubus or CustomRoles.Pelican or CustomRoles.Infectious or CustomRoles.Virus or CustomRoles.Pickpocket or CustomRoles.Traitor or CustomRoles.PlagueBearer or CustomRoles.Pestilence or CustomRoles.Spiritcaller or CustomRoles.CovenLeader or CustomRoles.Ritualist or CustomRoles.Banshee or CustomRoles.Necromancer or CustomRoles.Medusa or CustomRoles.HexMaster or CustomRoles.Wraith or CustomRoles.Jinx or CustomRoles.Poisoner or CustomRoles.PotionMaster)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner)) yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
            return false;
        }
        BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
        return true;
    }
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
class IntroCutsceneDestroyPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsInGame) return;
        Main.introDestroyed = true;
        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.NormalOptions.MapId != 4)
            {
                Main.AllPlayerControls.Do(pc => pc.RpcResetAbilityCooldown());
                if (Options.FixFirstKillCooldown.GetBool())
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerControls.Do(x => x.ResetKillCooldown());
                        Main.AllPlayerControls.Where(x => (Main.AllPlayerKillCooldown[x.PlayerId] - 2f) > 0f).Do(pc => pc.SetKillCooldown(Options.FixKillCooldownValue.GetFloat() - 2f));
                    }, 2f, "FixKillCooldownTask");
            }

            _ = new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "SetImpostorForServer");
            
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                PlayerControl.LocalPlayer.RpcExile();
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            if (Options.RandomSpawn.GetBool())
            {
                RandomSpawn.SpawnMap map;
                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }

            var amDesyncImpostor = Main.ResetCamPlayerList.Contains(PlayerControl.LocalPlayer.PlayerId);
            if (amDesyncImpostor)
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
            }
        }
        Logger.Info("OnDestroy", "IntroCutscene");
    }
}
 