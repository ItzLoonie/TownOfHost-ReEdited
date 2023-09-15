using System.Collections.Generic;
using System.Linq;

namespace TOHE.Roles.Crewmate;

public static class Repairman
{
    private static readonly int Id = 7050;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem SkillLimit;
    public static OptionItem FixesDoors;
    public static OptionItem FixesReactors;
    public static OptionItem FixesOxygens;
    public static OptionItem FixesComms;
    public static OptionItem FixesElectrical;
    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;
    public static OptionItem SMAbilityUseGainWithEachTaskCompleted;
    public static OptionItem UsesUsedWhenFixingReactorOrO2;
    public static OptionItem UsesUsedWhenFixingLightsOrComms;
    public static float UsedSkillCount;

    private static bool DoorsProgressing = false;

    public static void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Repairman, canSetNum: true);
        SkillLimit = IntegerOptionItem.Create(Id + 10, "RepairmanSkillLimit", new(0, 100, 1), 50, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman])
            .SetValueFormat(OptionFormat.Times);
        FixesDoors = BooleanOptionItem.Create(Id + 11, "SabotageMasterFixesDoors", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        FixesReactors = BooleanOptionItem.Create(Id + 12, "SabotageMasternFixesReactors", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        FixesOxygens = BooleanOptionItem.Create(Id + 13, "SabotageMasterFixesOxygens", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        FixesComms = BooleanOptionItem.Create(Id + 14, "SabotageMasterFixesCommunications", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        FixesElectrical = BooleanOptionItem.Create(Id + 15, "SabotageMasterFixesElectrical", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        UsesUsedWhenFixingReactorOrO2 = FloatOptionItem.Create(Id + 17, "SMUsesUsedWhenFixingReactorOrO2", new(0f, 5f, 0.1f), 4f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman])
            .SetValueFormat(OptionFormat.Times);
        UsesUsedWhenFixingLightsOrComms = FloatOptionItem.Create(Id + 18, "SMUsesUsedWhenFixingLightsOrComms", new(0f, 5f, 0.1f), 1f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman])
            .SetValueFormat(OptionFormat.Times);
        CanBeOnImp = BooleanOptionItem.Create(Id + 19, "ImpCanBeRepairman", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 20, "CrewCanBeRepairman", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 21, "NeutralCanBeRepairman", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Repairman]);
    }
    public static void Init()
    {
        playerIdList = new();
        UsedSkillCount = 0;
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
    }
    public static void RepairSystem(ShipStatus __instance, SystemTypes systemType, byte amount)
    {
        switch (systemType)
        {
            case SystemTypes.Reactor:
                if (!FixesReactors.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount + UsesUsedWhenFixingReactorOrO2.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 16);
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 17);
                    UsedSkillCount += UsesUsedWhenFixingReactorOrO2.GetFloat();
                }
                break;
            case SystemTypes.Laboratory:
                if (!FixesReactors.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount + UsesUsedWhenFixingReactorOrO2.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                    UsedSkillCount += UsesUsedWhenFixingReactorOrO2.GetFloat();
                }
                break;
            case SystemTypes.LifeSupp:
                if (!FixesOxygens.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount + UsesUsedWhenFixingReactorOrO2.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                    UsedSkillCount += UsesUsedWhenFixingReactorOrO2.GetFloat();
                }
                break;
            case SystemTypes.Comms:
                if (!FixesComms.GetBool()) break;
                if (SkillLimit.GetFloat() > 0 && UsedSkillCount + UsesUsedWhenFixingLightsOrComms.GetFloat() - 1 >= SkillLimit.GetFloat()) break;
                if (amount is 64 or 65)
                {
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16);
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 17);
                    UsedSkillCount += UsesUsedWhenFixingLightsOrComms.GetFloat();
                }
                break;
            case SystemTypes.Doors:
                if (!FixesDoors.GetBool()) break;
                if (DoorsProgressing == true) break;

                int mapId = Main.NormalOptions.MapId;
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                DoorsProgressing = true;
                if (mapId == 2)
                {
                    //Polus
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 72);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 67, 68);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 66);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 73, 74);
                }
                else if (mapId == 4)
                {
                    //Airship
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 67);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 73);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 74, 75);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 76, 78);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 68, 70);
                    RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 83, 84);
                }
                DoorsProgressing = false;
                break;
        }
    }
    public static void SwitchSystemRepair(SwitchSystem __instance, byte amount)
    {
        if (!FixesElectrical.GetBool()) return;
        if (SkillLimit.GetFloat() > 0 &&
            UsedSkillCount + UsesUsedWhenFixingLightsOrComms.GetFloat() - 1 >= SkillLimit.GetFloat())
            return;

        if (amount is >= 0 and <= 4)
        {
            __instance.ActualSwitches = 0;
            __instance.ExpectedSwitches = 0;
            UsedSkillCount += UsesUsedWhenFixingLightsOrComms.GetFloat();
        }
    }
}