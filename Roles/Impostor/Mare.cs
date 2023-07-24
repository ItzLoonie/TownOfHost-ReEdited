using System.Collections.Generic;

using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Mare
{
    private static readonly int Id = 1600;
    public static List<byte> playerIdList = new();

    private static OptionItem KillCooldownInLightsOut;
    private static OptionItem KillCooldownNormally;
  //  private static OptionItem SpeedInLightsOut;
    private static bool idAccelerated = false;  //加速済みかフラグ


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mare);
     //   SpeedInLightsOut = FloatOptionItem.Create(Id + 10, "MareAddSpeedInLightsOut", new(0.1f, 0.5f, 0.1f), 0.3f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
      //      .SetValueFormat(OptionFormat.Multiplier);
        KillCooldownInLightsOut = FloatOptionItem.Create(Id + 11, "MareKillCooldownInLightsOut", new(0f, 180f, 2.5f), 7.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
            .SetValueFormat(OptionFormat.Seconds);
        KillCooldownNormally = FloatOptionItem.Create(Id + 12, "KillCooldownNormally", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mare])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte mare)
    {
        playerIdList.Add(mare);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static float GetKillCooldown => Utils.IsActive(SystemTypes.Electrical) ? KillCooldownInLightsOut.GetFloat() : DefaultKillCooldown;
    public static void SetKillCooldown(byte id)
    {
        if (Utils.IsActive(SystemTypes.Electrical))
        Main.AllPlayerKillCooldown[id] = KillCooldownInLightsOut.GetFloat();
        else
        Main.AllPlayerKillCooldown[id] = KillCooldownNormally.GetFloat();
    }
    public static void ApplyGameOptions(byte playerId)
    {
        if (Utils.IsActive(SystemTypes.Electrical) && !idAccelerated)
        { //停電中で加速済みでない場合
            idAccelerated = true;
        //    Main.AllPlayerSpeed[playerId] += SpeedInLightsOut.GetFloat();//Mareの速度を加算
        }
        else if (!Utils.IsActive(SystemTypes.Electrical) && idAccelerated)
        { //停電中ではなく加速済みになっている場合
            idAccelerated = false;
          //  Main.AllPlayerSpeed[playerId] -= SpeedInLightsOut.GetFloat();//Mareの速度を減算
        }
    }

    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
        => !isMeeting && playerIdList.Contains(target.PlayerId) && Utils.IsActive(SystemTypes.Electrical);
}