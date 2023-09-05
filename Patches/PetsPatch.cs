namespace TOHE;

public static class PetsPatch
{
    public static void RpsRemovePet(this PlayerControl pc)
    {
        if (!GameStates.IsInGame) return;
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;
        if (Camouflage.PlayerSkins[pc.PlayerId].PetId != "" && !pc.IsAlive() && pc.Data.IsDead) return;

        pc.RpcSetPet("");
    }
}