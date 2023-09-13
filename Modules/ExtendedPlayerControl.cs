using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

static class ExtendedPlayerControl
{
    public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[player.PlayerId].SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            if (!Cleanser.CleansedCanGetAddon.GetBool() && player.Is(CustomRoles.Cleansed)) return;
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            //if (role == CustomRoles.Cleanser) Main.PlayerStates[player.PlayerId].SetSubRole(role, AllReplace:true);
            //else Main.PlayerStates[player.PlayerId].SetSubRole(role);
        }
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerId);
            writer.WritePacked((int)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public static void RpcExile(this PlayerControl player)
    {
        RPC.ExileAsync(player);
    }
    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static int GetClientId(this PlayerControl player)
    {
        if (player == null) return -1;
        var client = player.GetClient();
        return client == null ? -1 : client.Id;
    }
    public static CustomRoles GetCustomRole(this GameData.PlayerInfo player)
    {
        return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
    }
    /// <summary>
    /// ※サブロールは取得できません。
    /// </summary>
    public static CustomRoles GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
            return CustomRoles.Crewmate;
        }
        var GetValue = Main.PlayerStates.TryGetValue(player.PlayerId, out var State);

        return GetValue ? State.MainRole : CustomRoles.Crewmate;
    }

    public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
            return new() { CustomRoles.NotAssigned };
        }
        return Main.PlayerStates[player.PlayerId].SubRoles;
    }
    public static CountTypes GetCountTypes(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCountTypesを取得しようとしましたが、対象がnullでした。", "GetCountTypes");
            return CountTypes.None;
        }

        return Main.PlayerStates.TryGetValue(player.PlayerId, out var State) ? State.countTypes : CountTypes.None;
    }
    public static void RpcSetNameEx(this PlayerControl player, string name)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        }
        HudManagerPatch.LastSetNameDesyncCount++;

        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
        player.RpcSetName(name);
    }

    public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
    {
        //player: 名前の変更対象
        //seer: 上の変更を確認することができるプレイヤー
        if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
        if (seer == null) seer = player;

        if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
        {
            //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
            return;
        }
        Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        HudManagerPatch.LastSetNameDesyncCount++;
        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for {seer.GetNameWithRole()}", "RpcSetNamePrivate");

        var clientId = seer.GetClientId();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.Reliable, clientId);
        writer.Write(name);
        writer.Write(DontShowOnModdedClient);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
    {
        //player: 名前の変更対象

        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0, bool forObserver = false)
    {
        if (target == null) target = killer;
        if (!forObserver && !MeetingStates.FirstMeeting)
            Main.AllPlayerControls
                .Where(x => x.Is(CustomRoles.Observer) && killer.PlayerId != x.PlayerId)
                .Do(x => x.RpcGuardAndKill(target, colorId, true));
        
        // Host
        if (killer.AmOwner)
        {
            killer.ProtectPlayer(target, colorId);
            killer.MurderPlayer(target);
        }
        // Other Clients
        if (killer.PlayerId != 0)
        {
            var sender = CustomRpcSender.Create("GuardAndKill Sender", SendOption.None);
            sender.StartMessage(killer.GetClientId());
            sender.StartRpc(killer.NetId, (byte)RpcCalls.ProtectPlayer)
                .WriteNetObject(target)
                .Write(colorId)
                .EndRpc();
            sender.StartRpc(killer.NetId, (byte)RpcCalls.MurderPlayer)
                .WriteNetObject(target)
                .EndRpc();
            sender.EndMessage();
            sender.SendMessage();
        }
    }
    public static void SetKillCooldownV2(this PlayerControl player, float time = -1f)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        player.SyncSettings();
        player.RpcGuardAndKill();
        player.ResetKillCooldown();
    }
    public static void SetKillCooldown(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient() || !Options.DisableShieldAnimations.GetBool())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, 11);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcGuardAndKill(target, 11, true));
        }
        player.ResetKillCooldown();
    }
    public static void SetKillCooldownV3(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient() || !Options.DisableShieldAnimations.GetBool())
        {
            player.SyncSettings();
            player.RpcGuardAndKill(target, 11);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcGuardAndKill(target, 11, true));
        }
        player.ResetKillCooldown();
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
    {
        if (target == null) target = killer;
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target);
        }
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }
    [Obsolete]
    public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            killer.ProtectPlayer(target, colorId);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(colorId);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外が実行しても何も起こさない
        Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
        if (PlayerControl.LocalPlayer == target)
        {
            //targetがホストだった場合
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            //targetがホスト以外だった場合
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
            writer.WriteNetObject(target);
            writer.Write(0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        /*
            プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
            ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
            この変更により、役職としての守護天使が無効化されます。
            ホストのクールダウンは直接リセットします。
        */
    }
    public static void RpcDesyncRepairSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
        if(!AmongUsClient.Instance.AmHost) return;
        byte KilledById;
        if(KilledBy == null)
            KilledById = byte.MaxValue;
        else
            KilledById = KilledBy.PlayerId;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(KilledById);
        AmongUsClient.Instance.FinishRpcImmediately(writer);

        RPC.BeKilled(player.PlayerId, KilledById);
    }*/
    public static void MarkDirtySettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
    }
    public static void SyncSettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
        GameOptionsSender.SendAllGameOptions();
    }
    public static TaskState GetPlayerTaskState(this PlayerControl player)
    {
        return Main.PlayerStates[player.PlayerId].GetTaskState();
    }

    /*public static GameOptionsData DeepCopy(this GameOptionsData opt)
    {
        var optByte = opt.ToBytes(5);
        return GameOptionsData.FromBytes(optByte);
    }*/

    public static string GetDisplayRoleName(this PlayerControl player, bool pure = false)
    {
        return Utils.GetDisplayRoleName(player.PlayerId, pure);
    }
    public static string GetSubRoleName(this PlayerControl player, bool forUser = false)
    {
        var SubRoles = Main.PlayerStates[player.PlayerId].SubRoles;
        if (!SubRoles.Any()) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role == CustomRoles.NotAssigned) continue;
            sb.Append($"{Utils.ColorString(Color.white, " + ")}{Utils.GetRoleName(role, forUser)}");
        }

        return sb.ToString();
    }
    public static string GetAllRoleName(this PlayerControl player, bool forUser = true)
    {
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole(), forUser);
        text += player.GetSubRoleName(forUser);
        return text;
    }
    public static string GetNameWithRole(this PlayerControl player, bool forUser = false)
    {
        return $"{player?.Data?.PlayerName}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName(forUser)})" : "");
    }
    public static string GetRoleColorCode(this PlayerControl player)
    {
        return Utils.GetRoleColorCode(player.GetCustomRole());
    }
    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

        var systemtypes = SystemTypes.Reactor;
        if (Main.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;

        _ = new LateTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 128);
        }, 0f + delay, "Reactor Desync");

        _ = new LateTask(() =>
        {
            pc.RpcSpecificMurderPlayer();
        }, 0.2f + delay, "Murder To Reset Cam");

        _ = new LateTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
        }, 0.4f + delay, "Fix Desync Reactor");
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        int clientId = pc.GetClientId();
        // Logger.Info($"{pc}", "ReactorFlash");
        var systemtypes = SystemTypes.Reactor;
        if (Main.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;
        float FlashDuration = Options.KillFlashDuration.GetFloat();

        pc.RpcDesyncRepairSystem(systemtypes, 128);

        _ = new LateTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 16);

            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string GetRealName(this PlayerControl player, bool isMeeting = false)
    {
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        int playerCount = Main.AllAlivePlayerControls.Count();
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || Pelican.IsEaten(pc.PlayerId)) return false;

        return pc.GetCustomRole() switch
        {
            //Standard
            CustomRoles.FireWorks => FireWorks.CanUseKillButton(pc),
            CustomRoles.Mafia => Utils.CanMafiaKill(),
            CustomRoles.Shaman => pc.IsAlive(),
            CustomRoles.Underdog => playerCount <= Options.UnderdogMaximumPlayersNeededToKill.GetInt(),
            CustomRoles.Inhibitor => !Utils.IsActive(SystemTypes.Electrical) && !Utils.IsActive(SystemTypes.Laboratory) && !Utils.IsActive(SystemTypes.Comms) && !Utils.IsActive(SystemTypes.LifeSupp) && !Utils.IsActive(SystemTypes.Reactor),
            CustomRoles.Saboteur => Utils.IsActive(SystemTypes.Electrical) || Utils.IsActive(SystemTypes.Laboratory) || Utils.IsActive(SystemTypes.Comms) || Utils.IsActive(SystemTypes.LifeSupp) || Utils.IsActive(SystemTypes.Reactor),
            CustomRoles.Sniper => Sniper.CanUseKillButton(pc),
            CustomRoles.Sheriff => Sheriff.CanUseKillButton(pc.PlayerId),
            CustomRoles.Jailer => pc.IsAlive(),
            CustomRoles.Crusader => Crusader.CanUseKillButton(pc.PlayerId),
            CustomRoles.CopyCat => pc.IsAlive(),
            CustomRoles.Pelican => pc.IsAlive(),
            CustomRoles.Arsonist => !pc.IsDouseDone(),
            CustomRoles.Revolutionist => !pc.IsDrawDone(),
            CustomRoles.SwordsMan => pc.IsAlive(),
            CustomRoles.Jackal => pc.IsAlive(),
            CustomRoles.Sidekick => pc.IsAlive(),
            CustomRoles.Necromancer => pc.IsAlive(),
            CustomRoles.HexMaster => pc.IsAlive(),
            CustomRoles.Occultist => pc.IsAlive(),
            CustomRoles.Poisoner => pc.IsAlive(),
            CustomRoles.Juggernaut => pc.IsAlive(),
            CustomRoles.Reverie => pc.IsAlive(),
            CustomRoles.PotionMaster => pc.IsAlive(),
            CustomRoles.NSerialKiller => pc.IsAlive(),
            CustomRoles.Werewolf => pc.IsAlive(),
            CustomRoles.Medusa => pc.IsAlive(),
            CustomRoles.Traitor => pc.IsAlive(),
            CustomRoles.Glitch => pc.IsAlive(),
            CustomRoles.Pickpocket => pc.IsAlive(),
            CustomRoles.Maverick => pc.IsAlive(),
            CustomRoles.Jinx => pc.IsAlive(),
            CustomRoles.Parasite => pc.IsAlive(),
            CustomRoles.Refugee => pc.IsAlive(),
    //        CustomRoles.Minion => pc.IsAlive(),
            CustomRoles.NWitch => pc.IsAlive(),
            CustomRoles.Witness => pc.IsAlive(),
            CustomRoles.CovenLeader => pc.IsAlive(),
            CustomRoles.Ritualist => pc.IsAlive(),
            CustomRoles.Shroud => pc.IsAlive(),
            CustomRoles.Wraith => pc.IsAlive(),
            CustomRoles.Shade => pc.IsAlive(),
            CustomRoles.Bomber => (Options.BomberCanKill.GetBool() && pc.IsAlive()),
            CustomRoles.Nuker => (Options.BomberCanKill.GetBool() && pc.IsAlive()),
            CustomRoles.Innocent => pc.IsAlive(),
            CustomRoles.Counterfeiter => Counterfeiter.CanUseKillButton(pc.PlayerId),
            CustomRoles.Pursuer => Pursuer.CanUseKillButton(pc.PlayerId),
            CustomRoles.Morphling => Morphling.CanUseKillButton(pc.PlayerId),
            CustomRoles.FFF => pc.IsAlive(),
            CustomRoles.Medic => Medic.CanUseKillButton(pc.PlayerId),
            CustomRoles.Gamer => pc.IsAlive(),
            CustomRoles.DarkHide => DarkHide.CanUseKillButton(pc),
            CustomRoles.Provocateur => pc.IsAlive(),
            CustomRoles.Assassin => Assassin.CanUseKillButton(pc),
            CustomRoles.BloodKnight => pc.IsAlive(),
            CustomRoles.Banshee => pc.IsAlive(),
            CustomRoles.Crewpostor => false,
            CustomRoles.Totocalcio => Totocalcio.CanUseKillButton(pc),
            CustomRoles.Romantic => pc.IsAlive(),
            CustomRoles.RuthlessRomantic => pc.IsAlive(),
            CustomRoles.VengefulRomantic => VengefulRomantic.CanUseKillButton(pc),
            CustomRoles.Succubus => Succubus.CanUseKillButton(pc),
            CustomRoles.CursedSoul => CursedSoul.CanUseKillButton(pc),
            CustomRoles.Admirer => Admirer.CanUseKillButton(pc),
            CustomRoles.Amnesiac => false,
            //CustomRoles.Warlock => !Main.isCurseAndKill.TryGetValue(pc.PlayerId, out bool wcs) || !wcs,
            CustomRoles.Infectious => Infectious.CanUseKillButton(pc),
            CustomRoles.Monarch => Monarch.CanUseKillButton(pc),
            CustomRoles.Deputy => Deputy.CanUseKillButton(pc),
            CustomRoles.Virus => pc.IsAlive(),
            CustomRoles.Farseer => pc.IsAlive(),
            CustomRoles.Spiritcaller => pc.IsAlive(),
            CustomRoles.PlagueBearer => pc.IsAlive(),
            CustomRoles.Pestilence => pc.IsAlive(),
            CustomRoles.Pirate => pc.IsAlive(),
            CustomRoles.Seeker => pc.IsAlive(),
            CustomRoles.Agitater => pc.IsAlive(),
            CustomRoles.ChiefOfPolice => ChiefOfPolice.CanUseKillButton(pc.PlayerId),

            _ => pc.Is(CustomRoleTypes.Impostor),
        };
    }
    public static bool CanUseImpostorVentButton(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel) return false;
        if (CopyCat.playerIdList.Contains(pc.PlayerId)) return true;
        if (pc.Is(CustomRoles.Nimble)) return true;
     //   if (pc.Is(CustomRoles.Circumvent)) return false;

        return pc.GetCustomRole() switch
        {
            CustomRoles.Minimalism or
            CustomRoles.Sheriff or
            CustomRoles.Deputy or
            CustomRoles.Innocent or
        //    CustomRoles.SwordsMan or
            CustomRoles.FFF or
            CustomRoles.Medic or
      //      CustomRoles.NWitch or
            CustomRoles.DarkHide or
            CustomRoles.Monarch or
            CustomRoles.Romantic or
            CustomRoles.Provocateur or
            CustomRoles.Totocalcio or
            CustomRoles.Succubus or
            CustomRoles.CursedSoul or
            CustomRoles.PlagueBearer or
            CustomRoles.Admirer or
    //        CustomRoles.Minion or
            CustomRoles.Amnesiac or
            CustomRoles.Glitch or
            CustomRoles.Crusader or
            CustomRoles.ChiefOfPolice or
            CustomRoles.Wildling
            => false,

            CustomRoles.Jackal => Jackal.CanVent.GetBool(),
            CustomRoles.VengefulRomantic => Romantic.VengefulCanVent.GetBool(),
            CustomRoles.RuthlessRomantic => Romantic.RuthlessCanVent.GetBool(),
            CustomRoles.Sidekick => Jackal.CanVentSK.GetBool(),
            CustomRoles.Poisoner => Poisoner.CanVent.GetBool(),
            CustomRoles.NSerialKiller => NSerialKiller.CanVent.GetBool(),
            CustomRoles.Werewolf => Werewolf.CanVent.GetBool(),
            CustomRoles.Pestilence => PlagueBearer.PestilenceCanVent.GetBool(),
            CustomRoles.Medusa => Medusa.CanVent.GetBool(),
            CustomRoles.Traitor => Traitor.CanVent.GetBool(),
            CustomRoles.NWitch => NWitch.CanVent.GetBool(),
            CustomRoles.CovenLeader => CovenLeader.CanVent.GetBool(),
            CustomRoles.Ritualist => Options.RitualistCanVent.GetBool(),
            CustomRoles.Necromancer => Options.NecromancerCanVent.GetBool(),
            CustomRoles.Shroud => Shroud.CanVent.GetBool(),
            CustomRoles.Maverick => Maverick.CanVent.GetBool(),
            CustomRoles.Jinx => Jinx.CanVent.GetBool(),
            CustomRoles.Pelican => Pelican.CanVent.GetBool(),
            CustomRoles.Gamer => Gamer.CanVent.GetBool(),
            CustomRoles.BloodKnight => BloodKnight.CanVent.GetBool(),
            CustomRoles.Banshee => Banshee.CanVent.GetBool(),
            CustomRoles.Juggernaut => Juggernaut.CanVent.GetBool(),
            CustomRoles.Infectious => Infectious.CanVent.GetBool(),
            CustomRoles.PotionMaster => PotionMaster.CanVent.GetBool(),
            CustomRoles.Virus => Virus.CanVent.GetBool(),
            CustomRoles.SwordsMan => SwordsMan.CanVent.GetBool(),
            CustomRoles.Pickpocket => Pickpocket.CanVent.GetBool(),
            CustomRoles.HexMaster => true,
            CustomRoles.Occultist => true,
            CustomRoles.Wraith => true,
            CustomRoles.Shade => true,
         //   CustomRoles.Chameleon => true,
            CustomRoles.Parasite => true,
            CustomRoles.Refugee => true,
            CustomRoles.Spiritcaller => Spiritcaller.CanVent.GetBool(),

            CustomRoles.Arsonist => pc.IsDouseDone(),
            CustomRoles.Revolutionist => pc.IsDrawDone(),

            _ => pc.Is(CustomRoleTypes.Impostor),
        };
    }
    public static bool CanUseSabotage(this PlayerControl pc) // NOTE: THIS IS FOR THE HUD FOR MODDED CLIENTS, THIS DOES NOT DETERMINE WHETHER A ROLE CAN SABOTAGE
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel) return false;
      //  if (CopyCat.playerIdList.Contains(pc.PlayerId)) return true;

        return pc.GetCustomRole() switch
        {
            CustomRoles.Sheriff or
            CustomRoles.Crusader or
            CustomRoles.Pirate or
            CustomRoles.CopyCat or
            CustomRoles.CursedSoul or
            CustomRoles.Admirer or
            CustomRoles.Amnesiac or
            CustomRoles.Monarch or
            CustomRoles.Deputy or
            CustomRoles.Arsonist or
            CustomRoles.Medusa or
            CustomRoles.SwordsMan or
            CustomRoles.Reverie or
            CustomRoles.Innocent or
            CustomRoles.Pelican or
            CustomRoles.Counterfeiter or
            CustomRoles.Pursuer or
            CustomRoles.Revolutionist or
            CustomRoles.FFF or
            CustomRoles.Medic or
            CustomRoles.Gamer or
            CustomRoles.HexMaster or
            CustomRoles.Occultist or
            CustomRoles.Wraith or
            CustomRoles.Shade or
            CustomRoles.Juggernaut or
            CustomRoles.Jinx or
            CustomRoles.DarkHide or
            CustomRoles.Provocateur or
            CustomRoles.BloodKnight or
            CustomRoles.Banshee or
            CustomRoles.Poisoner or
            CustomRoles.NSerialKiller or
            CustomRoles.Maverick or
            CustomRoles.NWitch or
            CustomRoles.CovenLeader or
            CustomRoles.Ritualist or
            CustomRoles.Shroud or
            CustomRoles.Totocalcio or
            CustomRoles.Succubus or
            CustomRoles.Infectious or
            CustomRoles.Virus or
            CustomRoles.Farseer or
            CustomRoles.Pickpocket or
            CustomRoles.PlagueBearer or
            CustomRoles.Necromancer or
            CustomRoles.Pestilence or
            CustomRoles.Werewolf or
    //        CustomRoles.Minion or
            CustomRoles.Spiritcaller
            => false,

            CustomRoles.Jackal => Jackal.CanUseSabotage.GetBool(),
            CustomRoles.Sidekick => Jackal.CanUseSabotageSK.GetBool(),
            CustomRoles.Traitor => Traitor.CanUseSabotage.GetBool(),
            CustomRoles.Parasite => true,
            CustomRoles.Glitch => true,
            CustomRoles.PotionMaster => true,
            CustomRoles.Refugee => true,
            

            _ => pc.Is(CustomRoleTypes.Impostor),
        };
    }
    public static bool IsDousedPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null || target == null || Main.isDoused == null) return false;
        Main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }
    public static bool IsDrawPlayer(this PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist == null && target == null && Main.isDraw == null) return false;
        Main.isDraw.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDraw);
        return isDraw;
    }
    public static bool IsRevealedPlayer(this PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null || Main.isRevealed == null) return false;
        Main.isRevealed.TryGetValue((player.PlayerId, target.PlayerId), out bool isDoused);
        return isDoused;
    }
    public static void RpcSetDousedPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetDrawPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDrawPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRevealtPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRevealedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
        switch (player.GetCustomRole())
        {
            case CustomRoles.SerialKiller:
                SerialKiller.ApplyKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                break;
            case CustomRoles.Jailer:
                Jailer.SetKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                break;
            case CustomRoles.TimeThief:
                TimeThief.SetKillCooldown(player.PlayerId); //タイムシーフはタイムシーフのキルクールに。
                break;
            case CustomRoles.Agitater:
                Agitater.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Cultivator:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CultivatorKillCooldown.GetFloat();
                break;
            /*    case CustomRoles.Mare:
                    Mare.SetKillCooldown(player.PlayerId);
                    break; */
            case CustomRoles.EvilDiviner:
                EvilDiviner.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Morphling:
                Morphling.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.PotionMaster:
                PotionMaster.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pickpocket:
                Pickpocket.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Arsonist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat(); //アーソニストはアーソニストのキルクールに。
                break;
            case CustomRoles.Inhibitor:
            case CustomRoles.Saboteur:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.InhibitorCD.GetFloat(); //アーソニストはアーソニストのキルクールに。
                break;
            case CustomRoles.Revolutionist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RevolutionistCooldown.GetFloat();
                break;
            case CustomRoles.Underdog:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.UnderdogKillCooldown.GetFloat();
                break;
            case CustomRoles.Jackal:
                Jackal.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Sidekick:
                Sidekick.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.PlagueBearer:
                PlagueBearer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pestilence:
                PlagueBearer.SetKillCooldownPestilence(player.PlayerId);
                break;

            case CustomRoles.Councillor:
                Councillor.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.HexMaster:
            case CustomRoles.Wraith:
            case CustomRoles.Shade:
            case CustomRoles.Occultist:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown;
                break;
            case CustomRoles.Parasite:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ParasiteCD.GetFloat();
                break;
            case CustomRoles.Shaman:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.VoodooCooldown.GetFloat();
                break;
            case CustomRoles.Refugee:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.RefugeeKillCD.GetFloat();
                break;
            case CustomRoles.NSerialKiller:
                NSerialKiller.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Werewolf:
                Main.AllPlayerKillCooldown[player.PlayerId] = Werewolf.KillCooldown.GetFloat();
                break;
            case CustomRoles.Necromancer:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.NecromancerKillCD.GetFloat();
                break;
            case CustomRoles.Traitor:
                Traitor.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Glitch:
                Glitch.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.NWitch:
                NWitch.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.CovenLeader:
                CovenLeader.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Chronomancer:
                Chronomancer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Shroud:
                Shroud.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Maverick:
                Maverick.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Jinx:
                Jinx.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Poisoner:
                Poisoner.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Sheriff:
                Sheriff.SetKillCooldown(player.PlayerId); //シェリフはシェリフのキルクールに。
                break;
            case CustomRoles.CopyCat:
                CopyCat.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Minimalism:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.MNKillCooldown.GetFloat();
                break;
            case CustomRoles.SwordsMan:
                SwordsMan.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Zombie:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ZombieKillCooldown.GetFloat();
                Main.AllPlayerSpeed[player.PlayerId] -= Options.ZombieSpeedReduce.GetFloat();
                break;
            case CustomRoles.BoobyTrap:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.BTKillCooldown.GetFloat();
                break;
            case CustomRoles.Scavenger:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ScavengerKillCooldown.GetFloat();
                break;
            case CustomRoles.Bomber:
            case CustomRoles.Nuker:
                if (Options.BomberCanKill.GetBool())
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.BomberKillCD.GetFloat();
                else
                Main.AllPlayerKillCooldown[player.PlayerId] = 300f;
                break;
            case CustomRoles.Witness:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.WitnessCD.GetFloat();
                break;
            case CustomRoles.Capitalism:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CapitalismSkillCooldown.GetFloat();
                break;
            case CustomRoles.Pelican:
                Main.AllPlayerKillCooldown[player.PlayerId] = Pelican.KillCooldown.GetFloat();
                break;
            case CustomRoles.Counterfeiter:
                Counterfeiter.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pursuer:
                Pursuer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.FFF:
                Main.AllPlayerKillCooldown[player.PlayerId] = 0f;
                break;
            case CustomRoles.Medusa:
                Medusa.SetKillCooldown(player.PlayerId);
                break;

            case CustomRoles.Cleaner:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.CleanerKillCooldown.GetFloat();
                break;
            case CustomRoles.Ludopath:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.LudopathRandomKillCD.GetFloat();
                break;
            case CustomRoles.Medic:
                Medic.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Gamer:
                Gamer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.BallLightning:
                BallLightning.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.DarkHide:
                DarkHide.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Greedier:
                Greedier.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Provocateur:
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.ProvKillCD.GetFloat();
                break;
            case CustomRoles.Assassin:
                Assassin.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Sans:
                Sans.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Juggernaut:
                Juggernaut.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Reverie:
                Reverie.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Hacker:
                Hacker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Banshee:
                Banshee.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Totocalcio:
                Totocalcio.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Romantic:
                Romantic.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.VengefulRomantic:
                Main.AllPlayerKillCooldown[player.PlayerId] = Romantic.VengefulKCD.GetFloat();
                break;
            case CustomRoles.RuthlessRomantic:
                Main.AllPlayerKillCooldown[player.PlayerId] = Romantic.RuthlessKCD.GetFloat();
                break;
            case CustomRoles.Gangster:
                Gangster.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Succubus:
                Succubus.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.CursedSoul:
                CursedSoul.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Admirer:
                Admirer.SetKillCooldown(player.PlayerId);
                break;
        /*    case CustomRoles.Amnesiac:
                Amnesiac.SetKillCooldown(player.PlayerId);
                break; */
            case CustomRoles.Infectious:
                Infectious.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Monarch:
                Monarch.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Pirate:
                Pirate.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Deputy:
                Deputy.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Virus:
                Virus.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Farseer:
                Farseer.SetCooldown(player.PlayerId);
                break;
            case CustomRoles.Dazzler:
                Dazzler.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Deathpact:
                Deathpact.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Devourer:
                Devourer.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Spiritcaller:
                Spiritcaller.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Lurker:
                Lurker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Crusader:
                Crusader.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.Seeker:
                Seeker.SetKillCooldown(player.PlayerId);
                break;
            case CustomRoles.ChiefOfPolice:
                ChiefOfPolice.SetKillCooldown(player.PlayerId);
                break;
        }
        if (player.PlayerId == LastImpostor.currentId)
            LastImpostor.SetKillCooldown();
        if (player.Is(CustomRoles.Mare))
            Main.AllPlayerKillCooldown[player.PlayerId] = Options.MareKillCD.GetFloat();
        
        if (Main.KilledDiseased.ContainsKey(player.PlayerId))
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = Main.AllPlayerKillCooldown[player.PlayerId] + Main.KilledDiseased[player.PlayerId] * Options.DiseasedCDOpt.GetFloat();
            Logger.Info($"kill cd of player set to {Main.AllPlayerKillCooldown[player.PlayerId]}", "Diseased");
        }
        if (Main.KilledAntidote.ContainsKey(player.PlayerId))
        {
            var kcd = Main.AllPlayerKillCooldown[player.PlayerId] - Main.KilledAntidote[player.PlayerId] * Options.AntidoteCDOpt.GetFloat();
            if (kcd < 0) kcd = 0;
            Main.AllPlayerKillCooldown[player.PlayerId] = kcd;
            Logger.Info($"kill cd of player set to {Main.AllPlayerKillCooldown[player.PlayerId]}", "Antidote");
        }
    }
    public static bool IsNonCrewSheriff(this PlayerControl sheriff)
    {
        return sheriff.Is(CustomRoles.Madmate)
            || sheriff.Is(CustomRoles.Charmed)
            || sheriff.Is(CustomRoles.Infected)
            || sheriff.Is(CustomRoles.Contagious)
            || sheriff.Is(CustomRoles.Egoist);
    }
    public static bool IsEvilAddons(this PlayerControl target)
    {
        return target.Is(CustomRoles.Madmate)
            || target.Is(CustomRoles.Egoist)
            || target.Is(CustomRoles.Charmed)
            || target.Is(CustomRoles.Recruit)
            || target.Is(CustomRoles.Infected)
            || target.Is(CustomRoles.Contagious)
            || target.Is(CustomRoles.Rogue)
            || target.Is(CustomRoles.Rascal)
            || target.Is(CustomRoles.Soulless);
    }
    public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
    {
        Logger.Info($"{target?.Data?.PlayerName}はTrapperだった", "Trapper");
        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
    }
    public static bool IsDouseDone(this PlayerControl player)
    {
        if (!player.Is(CustomRoles.Arsonist)) return false;
        var count = Utils.GetDousedPlayerCount(player.PlayerId);
        return count.Item1 >= count.Item2;
    }
    public static bool IsDrawDone(this PlayerControl player)//判断是否拉拢完成
    {
        if (!player.Is(CustomRoles.Revolutionist)) return false;
        var count = Utils.GetDrawPlayerCount(player.PlayerId, out var _);
        return count.Item1 >= count.Item2;
    }
    public static void RpcExileV2(this PlayerControl player)
    {
        player.Exiled();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcMurderPlayerV3(this PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId && killer.shapeshifting)
        {
            _ = new LateTask(() => { killer.RpcMurderPlayer(target); }, 1.5f, "Shapeshifting Suicide Delay");
            return;
        }

        killer.RpcMurderPlayer(target);
    }
    public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
    {
        if (target == null) target = killer;
        if (AmongUsClient.Instance.AmClient)
        {
            killer.MurderPlayer(target);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        Utils.NotifyRoles();
    }
    public static bool RpcCheckAndMurder(this PlayerControl killer, PlayerControl target, bool check = false) => CheckMurderPatch.RpcCheckAndMurder(killer, target, check);
    public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target, bool force = false)
    { /*サボタージュ中でも関係なしに会議を起こせるメソッド
        targetがnullの場合はボタンとなる*/
        if (Options.DisableMeeting.GetBool() && !force) return;
        ReportDeadBodyPatch.AfterReportTasks(reporter, target);
        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = new();
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL)
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }
    public static bool IsNeutralKiller(this PlayerControl player) => player.GetCustomRole().IsNK();
    public static bool IsNeutralBenign(this PlayerControl player) => player.GetCustomRole().IsNB();
    public static bool IsNeutralEvil(this PlayerControl player) => player.GetCustomRole().IsNE();
    public static bool IsNeutralChaos(this PlayerControl player) => player.GetCustomRole().IsNC();
    public static bool IsCovenMember(this PlayerControl player) => player.GetCustomRole().IsCoven();
    public static bool IsNonNeutralKiller(this PlayerControl player) => player.GetCustomRole().IsNonNK();
    public static bool IsSnitchTarget(this PlayerControl player) => player.GetCustomRole().IsSnitchTarget();
    
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Doctor) || seer.Is(CustomRoles.Autopsy)
        || (seer.Data.IsDead && Options.GhostCanSeeDeathReason.GetBool()))
        && target.Data.IsDead || target.Is(CustomRoles.Gravestone) && target.Data.IsDead;

    public static bool KnowDeadTeam(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Necroview))
        && target.Data.IsDead;

    public static bool KnowLivingTeam(this PlayerControl seer, PlayerControl target)
        => (seer.Is(CustomRoles.Visionary))
        && !target.Data.IsDead;

    public static bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.God) || target.Is(CustomRoles.GM) || (seer.AmOwner && Main.GodMode.Value)) return true;
        else if (Main.VisibleTasksCount && seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return true;
        else if (target.Is(CustomRoles.Gravestone) && target.Data.IsDead) return true;
        else if (Options.SeeEjectedRolesInMeeting.GetBool() && Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote) return true;
        else if (Options.MimicCanSeeDeadRoles.GetBool() && Main.VisibleTasksCount && seer.Is(CustomRoles.Mimic) && target.Data.IsDead) return true;
        else if (Options.LoverKnowRoles.GetBool() && seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) return true;
        else if (Options.KnowNtrRole.GetBool() && target.Is(CustomRoles.Ntr)) return true;
        else if (Options.ImpKnowAlliesRole.GetBool() && seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.CovenKnowAlliesRole.GetBool() && seer.GetCustomRole().IsCoven() && target.GetCustomRole().IsCoven()) return true;
        else if (Options.MadmateKnowWhosImp.GetBool() && seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.ImpKnowWhosMadmate.GetBool() && target.Is(CustomRoles.Madmate) && seer.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.AlliesKnowCrewpostor.GetBool() && seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Crewpostor)) return true;
        else if (Options.CrewpostorKnowsAllies.GetBool() && seer.Is(CustomRoles.Crewpostor) && target.Is(CustomRoleTypes.Impostor)) return true;
        else if (Options.WorkaholicVisibleToEveryone.GetBool() && target.Is(CustomRoles.Workaholic)) return true;
        else if (Options.DoctorVisibleToEveryone.GetBool() && target.Is(CustomRoles.Doctor) && !target.IsEvilAddons()) return true;
        else if (Options.MayorRevealWhenDoneTasks.GetBool() && target.Is(CustomRoles.Mayor) && target.GetPlayerTaskState().IsTaskFinished) return true;
        else if (target.GetPlayerTaskState().IsTaskFinished && seer.Is(CustomRoleTypes.Crewmate) && target.Is(CustomRoles.Marshall)) return true;
        else if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;
        else if (seer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) return true;
        else if (seer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;
        else if (seer.IsRevealedPlayer(target) && !target.Is(CustomRoles.Trickster)) return true;
        else if (Totocalcio.KnowRole(seer, target)) return true;
        else if (Romantic.KnowRole(seer, target)) return true;
        else if (Lawyer.KnowRole(seer, target)) return true;
        else if (EvilDiviner.IsShowTargetRole(seer, target)) return true;
        else if (PotionMaster.IsShowTargetRole(seer, target)) return true;
        else if (Executioner.KnowRole(seer, target)) return true;
        else if (Succubus.KnowRole(seer, target)) return true;
        else if (CursedSoul.KnowRole(seer, target)) return true;
        else if (Admirer.KnowRole(seer, target)) return true;
        else if (Amnesiac.KnowRole(seer, target)) return true;
        else if (Infectious.KnowRole(seer, target)) return true;
        else if (Virus.KnowRole(seer, target)) return true;

        else return false;
    }
    public static bool KnowRoleAddonsTarget(PlayerControl seer, PlayerControl target)
    {

        if (seer.Data.IsDead || seer.Is(CustomRoles.GM) || (seer.AmOwner && Main.GodMode.Value)) return true;
        else if (seer.Is(CustomRoles.God) && Options.GodKnowAddons.GetBool()) return true;
        else if (Options.LoverKnowAddons.GetBool() && Options.LoverKnowRoles.GetBool()
            && seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) return true;

        //Niko also want to add imposters and some neutrals here. But things related with converted roles and task states confused Niko.
        //Maybe these stuffs will be added later!
        else return false;
    }
    public static bool CanSeeLoverMark(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) return true;
        else if ((seer.Data.IsDead || seer.Is(CustomRoles.GM) 
        || (seer.AmOwner && Main.GodMode.Value)) && target.Is(CustomRoles.Lovers)) return true;
        else if (seer.Is(CustomRoles.God) && Options.GodKnowAddons.GetBool() && target.Is(CustomRoles.Lovers)) return true;
        else if (seer.Is(CustomRoles.Ntr) || target.Is(CustomRoles.Ntr)) return true;
        
        else return false;
    }
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
    {
        var role = player.GetCustomRole();
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong)
            switch (role)
            {
                case CustomRoles.Mafia:
                    Prefix = Utils.CanMafiaKill() ? "After" : "Before";
                    break;
            };
        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        return GetString($"{Prefix}{text}{Info}");
    }
    public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        var State = Main.PlayerStates[target.PlayerId];
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        byte killerId = killer == null ? byte.MaxValue : killer.PlayerId;
        RPC.SetRealKiller(target.PlayerId, killerId);
    }
    public static PlayerControl GetRealKiller(this PlayerControl target)
    {
        var killerId = Main.PlayerStates[target.PlayerId].GetRealKiller();
        return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
    }
    public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc)
    {
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return null;
        var Rooms = ShipStatus.Instance.AllRooms;
        if (Rooms == null) return null;
        foreach (var room in Rooms)
        {
            if (!room.roomArea) continue;
            if (pc.Collider.IsTouching(room.roomArea))
                return room;
        }
        return null;
    }

    //汎用
    public static bool Is(this PlayerControl target, CustomRoles role) =>
        role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
    public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
    public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
    public static bool IsAlive(this PlayerControl target)
    {
        //ロビーなら生きている
        //targetがnullならば切断者なので生きていない
        //targetがnullでなく取得できない場合は登録前なので生きているとする
        if (target == null || target.Is(CustomRoles.GM)) return false;
        return GameStates.IsLobby || (target != null && (!Main.PlayerStates.TryGetValue(target.PlayerId, out var ps) || !ps.IsDead));
    }
    public static bool IsExiled(this PlayerControl target)
    {
        return GameStates.InGame || (target != null && (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote));
    }

}
