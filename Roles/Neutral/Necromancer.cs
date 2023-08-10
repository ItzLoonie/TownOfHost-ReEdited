using System.Linq;
using System.Collections.Generic;

namespace TOHE.Roles.Neutral;

public static class Necromancer
{
    public static List<byte> playerIdList = new();

    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Any();
}