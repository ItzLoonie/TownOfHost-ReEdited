using TOHE;
using System;
using System.Collections.Generic;
using System.Linq;

internal static class GuessManagerHelpers
{
    public static Dictionary<CustomRoles, OptionItem> GuessOptionTarget = new Dictionary<CustomRoles, OptionItem>();

    public static readonly string[] GuessOption = { "CanGuessAll", "CanGuessIndividual" };

    public static void SetUpSpecialOptions(int Id)
    {
        foreach (var special in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAdditionRole() && x is not CustomRoles.KB_Normal))
        {
            SetUpSpecialAbilityOption(special, Id, true, Options.GuessAddonExemptions);
            Id++;
        }
    }

    public static void SetUpSpecialAbilityOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        parent ??= Options.GuessAddonExemptions;
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new Dictionary<string, string>() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        GuessOptionTarget[role] = BooleanOptionItem.Create(Id, "CanGuess%role%Addon", defaultValue, TOHE.TabGroup.GameSettings, false).SetParent(parent);
        GuessOptionTarget[role].ReplacementDictionary = replacementDic;
    }

    public static bool CanGuessAddon(CustomRoles role)
    {
        if (role.IsAdditionRole())
        {
            if (Options.GuesserMode.GetBool())
            {
                if (!Options.CanGuessAddons.GetBool())
                {
                    if (role.GetCustomRoleTypes() == CustomRoleTypes.Impostor && !Options.ImpostorsCanGuess.GetBool())
                        return false;
                    if (role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate && !Options.CrewmatesCanGuess.GetBool())
                        return false;
                    if (role.GetCustomRoleTypes() == CustomRoleTypes.Neutral && (!Options.NeutralKillersCanGuess.GetBool() || !Options.PassiveNeutralsCanGuess.GetBool()))
                        return false;
                }
            }
            else
            {
                if (role.GetCustomRoleTypes() == CustomRoleTypes.Impostor && !Options.EGCanGuessAdt.GetBool())
                    return false;
                if (role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate && !Options.GGCanGuessAdt.GetBool())
                    return false;
                if (role.GetCustomRoleTypes() == CustomRoleTypes.Neutral && !Options.GCanGuessAdt.GetBool())
                    return false;
            }
        }
        return true;
    }
}