using System;
using UnityEngine;

namespace TOHE.Modules;

public static class VersionChecker
{
    public static bool IsSupported { get; private set; } = true;

    public static void Check()
    {
        var amongUsVersion = Version.Parse(Application.version);
        Logger.Info($" {amongUsVersion}", "Among Us Version Check");

        var SupportedVersion = Version.Parse(Main.SupportedVersionAU);
        Logger.Info($" {SupportedVersion}", "Supported Version Check");

        IsSupported = amongUsVersion >= SupportedVersion;
        Logger.Info($" {IsSupported}", "Version Is Supported?");

        if (!IsSupported)
        {
            ErrorText.Instance.AddError(ErrorCode.UnsupportedVersion);
        }
    }
}
