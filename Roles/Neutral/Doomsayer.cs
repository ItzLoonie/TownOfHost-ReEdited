﻿using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TOHE.Roles.Neutral;

public static class Doomsayer
{
    private static readonly int Id = 27000;
    public static List<byte> playerIdList = new();

    public static int GuessesCount = 0;
    public static int GuessesCountPerMeeting = 0;
    public static bool CantGuess = false;

    public static OptionItem DoomsayerAmountOfGuessesToWin;
    public static OptionItem DCanGuessImpostors;
    public static OptionItem DCanGuessCrewmates;
    public static OptionItem DCanGuessNeutrals;
    public static OptionItem DCanGuessAdt;
    public static OptionItem AdvancedSettings;
    public static OptionItem MaxNumberOfGuessesPerMeeting;
    public static OptionItem KillCorrectlyGuessedPlayers;
    public static OptionItem DoesNotSuicideWhenMisguessing;
    public static OptionItem MisguessRolePrevGuessRoleUntilNextMeeting;
    public static OptionItem DoomsayerTryHideMsg;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Doomsayer);
        DoomsayerAmountOfGuessesToWin = IntegerOptionItem.Create(Id + 10, "DoomsayerAmountOfGuessesToWin", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer])
            .SetValueFormat(OptionFormat.Times);
        DCanGuessImpostors = BooleanOptionItem.Create(Id + 12, "DCanGuessImpostors", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessCrewmates = BooleanOptionItem.Create(Id + 13, "DCanGuessCrewmates", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessNeutrals = BooleanOptionItem.Create(Id + 14, "DCanGuessNeutrals", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        DCanGuessAdt = BooleanOptionItem.Create(Id + 15, "DCanGuessAdt", false, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);

        AdvancedSettings = BooleanOptionItem.Create(Id + 16, "DoomsayerAdvancedSettings", true, TabGroup.NeutralRoles, true)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
        MaxNumberOfGuessesPerMeeting = IntegerOptionItem.Create(Id + 17, "DoomsayerMaxNumberOfGuessesPerMeeting", new(1, 10, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        KillCorrectlyGuessedPlayers = BooleanOptionItem.Create(Id + 18, "DoomsayerKillCorrectlyGuessedPlayers", true, TabGroup.NeutralRoles, true)
            .SetParent(AdvancedSettings);
        DoesNotSuicideWhenMisguessing = BooleanOptionItem.Create(Id + 19, "DoomsayerDoesNotSuicideWhenMisguessing", false, TabGroup.NeutralRoles, false)
            .SetParent(AdvancedSettings);
        MisguessRolePrevGuessRoleUntilNextMeeting = BooleanOptionItem.Create(Id + 20, "DoomsayerMisguessRolePrevGuessRoleUntilNextMeeting", true, TabGroup.NeutralRoles, true)
            .SetParent(DoesNotSuicideWhenMisguessing);

        DoomsayerTryHideMsg = BooleanOptionItem.Create(Id + 21, "DoomsayerTryHideMsg", true, TabGroup.NeutralRoles, true)
            .SetColor(Color.green)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Doomsayer]);
    }
    public static void Init()
    {
        playerIdList = new();
        GuessesCount = 0;
        GuessesCountPerMeeting = 0;
        CantGuess = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDoomsayerProgress, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC()
    {
        GuessesCount++;
    }
    public static (int, int) GuessedPlayerCount()
    {
        int doomsayerguess = GuessesCount, GuessesToWin = DoomsayerAmountOfGuessesToWin.GetInt();

        return (doomsayerguess, GuessesToWin);
    }
    public static void CheckCountGuess(PlayerControl pirate)
    {
        if (!(GuessesCount >= DoomsayerAmountOfGuessesToWin.GetInt())) return;

        GuessesCount = DoomsayerAmountOfGuessesToWin.GetInt();
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Doomsayer);
        CustomWinnerHolder.WinnerIds.Add(pirate.PlayerId);
    }
    public static void OnReportDeadBody()
    {
        if (!(IsEnable && AdvancedSettings.GetBool())) return;

        CantGuess = false;
        GuessesCountPerMeeting = 0;
    }
}
