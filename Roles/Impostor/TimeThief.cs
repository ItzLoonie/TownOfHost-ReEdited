using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

public static class TimeThief
{
    private static readonly int Id = 3300;
    private static List<byte> playerIdList = new();
    public static OptionItem KillCooldown;
    public static OptionItem DecreaseMeetingTime;
    public static OptionItem LowerLimitVotingTime;
    public static OptionItem ReturnStolenTimeUponDeath;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.TimeThief);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
        DecreaseMeetingTime = IntegerOptionItem.Create(Id + 11, "TimeThiefDecreaseMeetingTime", new(0, 100, 1), 25, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
        LowerLimitVotingTime = IntegerOptionItem.Create(Id + 12, "TimeThiefLowerLimitVotingTime", new(1, 300, 1), 10, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
            .SetValueFormat(OptionFormat.Seconds);
        ReturnStolenTimeUponDeath = BooleanOptionItem.Create(Id + 13, "TimeThiefReturnStolenTimeUponDeath", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    private static int StolenTime(byte id)
    {
        return playerIdList.Contains(id) && (Utils.GetPlayerById(id).IsAlive() || !ReturnStolenTimeUponDeath.GetBool())
            ? DecreaseMeetingTime.GetInt() * Main.PlayerStates[id].GetKillCount(true)
            : 0;
    }
    public static int TotalDecreasedMeetingTime()
    {
        int sec = 0;
        foreach (var playerId in playerIdList)
            sec -= StolenTime(playerId);
        Logger.Info($"{sec}second", "TimeThief.TotalDecreasedMeetingTime");
        return sec;
    }
    public static string GetProgressText(byte playerId)
        => StolenTime(playerId) > 0 ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"{-StolenTime(playerId)}s") : "";
}