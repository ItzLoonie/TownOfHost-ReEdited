using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS.Internal.Xml.XPath;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor
{
    public static class SkinEater
    {
        static GameData.PlayerOutfit CosumedOutfit = new GameData.PlayerOutfit().Set("", 0, "", "", "visor_Lava", "");

        private static readonly int Id = 903634;
        public static List<byte> playerIdList = new();

        private static OptionItem DefaultKillCooldown;
        private static OptionItem ReduceKillCooldown;
        private static OptionItem MinKillCooldown;
        private static OptionItem ShapeshiftCooldown;
        private static OptionItem ShapeshiftDuration;

        public static Dictionary<byte, List<float>> PlayerSkinsCosumed = new();

        private static Dictionary<byte, float> NowCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SkinEater);
            DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SkinEater])
                .SetValueFormat(OptionFormat.Seconds);
            ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SkinEater])
                .SetValueFormat(OptionFormat.Seconds);
            MinKillCooldown = FloatOptionItem.Create(Id + 12, "SansMinKillCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SkinEater])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 14, "ShapeshiftCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SkinEater])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftDuration = FloatOptionItem.Create(Id + 15, "ShapeshiftDuration", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SkinEater])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            PlayerSkinsCosumed = new();
            NowCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            PlayerSkinsCosumed.TryAdd(playerId, new List<float>());
            NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
        }

        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];

        public static void OnShapeshift(PlayerControl pc, PlayerControl target)
        {
            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

            if (!Camouflage.IsCamouflage)
            {
                SetConsumedSkin(target);
            }

            if (!PlayerSkinsCosumed[pc.PlayerId].Contains(target.PlayerId))
            {
                PlayerSkinsCosumed[pc.PlayerId].Add(target.PlayerId);
                Camouflage.PlayerSkins[target.PlayerId] = CosumedOutfit;

                target.Notify(ColorString(GetRoleColor(CustomRoles.SkinEater), string.Format(GetString("SkinEaterCosumeSkin"), target.GetRealName())));
            }

            float cdReduction = ReduceKillCooldown.GetFloat() * PlayerSkinsCosumed[pc.PlayerId].Count;
            float cd = DefaultKillCooldown.GetFloat() - cdReduction;

            NowCooldown[pc.PlayerId] = cd < MinKillCooldown.GetFloat() ? MinKillCooldown.GetFloat() : cd;
        }

        private static void SetConsumedSkin(PlayerControl target)
        {
            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");

            target.SetColor(CosumedOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(CosumedOutfit.ColorId)
                .EndRpc();

            target.SetHat(CosumedOutfit.HatId, CosumedOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(CosumedOutfit.HatId)
                .EndRpc();

            target.SetSkin(CosumedOutfit.SkinId, CosumedOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(CosumedOutfit.SkinId)
                .EndRpc();

            target.SetVisor(CosumedOutfit.VisorId, CosumedOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(CosumedOutfit.VisorId)
                .EndRpc();

            target.SetPet(CosumedOutfit.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(CosumedOutfit.PetId)
                .EndRpc();

            sender.SendMessage();
        }
    }
}
