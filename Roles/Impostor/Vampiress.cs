using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using UnityEngine;
using TOHE.Roles.Crewmate;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Vampiress
{
    private class BittenInfo
    {
        public byte VampiressId;
        public float KillTimer;

        public BittenInfo(byte vampierId, float killTimer)
        {
            VampiressId = vampierId;
            KillTimer = killTimer;
        }
    }

    private static readonly int Id = 4550;
    private static readonly List<byte> PlayerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem OptionKillDelay;
    private static OptionItem ShapeshiftDur;
    private static OptionItem ShapeshiftCD;
    public static OptionItem KillCooldown;
    public static OptionItem CanVent;
    private static float KillDelay;
    private static readonly Dictionary<byte, BittenInfo> BittenPlayers = new();
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vampiress);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampiress])
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillDelay = FloatOptionItem.Create(Id + 11, "VampireKillDelay", new(1f, 60f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampiress])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCD = FloatOptionItem.Create(Id + 12, "ShapeshiftCooldown", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampiress])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDur = FloatOptionItem.Create(Id + 13, "ShapeshiftDuration", new(1f, 60f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampiress])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 14, "CanVent", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampiress]);
    }
    public static void Init()
    {
        PlayerIdList.Clear();
        BittenPlayers.Clear();
        IsEnable = false;

        KillDelay = OptionKillDelay.GetFloat();
    }
    public static void Add(byte playerId)
    {
        PlayerIdList.Add(playerId);
        IsEnable = true;
    }
    public static bool IsThisRole(byte playerId) => PlayerIdList.Contains(playerId);

    public static bool OnCheckBite(PlayerControl killer, PlayerControl target)
    {
        if (!IsThisRole(killer.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Glitch)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) return false;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        killer.SetKillCooldown();
        killer.RPCPlayCustomSound("Bite");

        //誰かに噛まれていなければ登録
        if (!BittenPlayers.ContainsKey(target.PlayerId))
        {
            BittenPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
        }
        return false;
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!IsThisRole(killer.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Glitch)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) return false;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;
        
        return false;
    }


    public static void OnFixedUpdate(PlayerControl vampiress)
    {
        if (!IsEnable) return;
        if (!GameStates.IsInTask) return;

        var vampiressID = vampiress.PlayerId;
        if (!IsThisRole(vampiress.PlayerId)) return;

        List<byte> targetList = new(BittenPlayers.Where(b => b.Value.VampiressId == vampiressID).Select(b => b.Key));

        foreach (var targetId in targetList)
        {
            var bitten = BittenPlayers[targetId];
            if (bitten.KillTimer >= KillDelay)
            {
                var target = Utils.GetPlayerById(targetId);
                KillBitten(vampiress, target);
                BittenPlayers.Remove(targetId);
            }
            else
            {
                bitten.KillTimer += Time.fixedDeltaTime;
                BittenPlayers[targetId] = bitten;
            }
        }
    }
    public static void KillBitten(PlayerControl vampiress, PlayerControl target, bool isButton = false)
    {
        if (vampiress == null || target == null || target.Data.Disconnected) return;
        if (target.IsAlive())
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bite;
            target.SetRealKiller(vampiress);
            target.RpcMurderPlayerV3(target);
            Medic.IsDead(target);
            Logger.Info($"Vampiressに噛まれている{target.name}を自爆させました。", "Vampiress");
            if (!isButton && vampiress.IsAlive())
            {
                RPC.PlaySoundRPC(vampiress.PlayerId, Sounds.KillSound);
                if (target.Is(CustomRoles.Trapper))
                    vampiress.TrapperKilled(target);
                vampiress.Notify(GetString("VampireTargetDead"));
                vampiress.SetKillCooldown();
            }
        }
        else
        {
            Logger.Info("Vampiressに噛まれている" + target.name + "はすでに死んでいました。", "Vampiress");
        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDur.GetFloat();
    }


    public static void OnStartMeeting()
    {
        foreach (var targetId in BittenPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            var vampiress = Utils.GetPlayerById(BittenPlayers[targetId].VampiressId);
            KillBitten(vampiress, target);
        }
        BittenPlayers.Clear();
    }
}
