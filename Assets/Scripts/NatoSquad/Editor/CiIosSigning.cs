using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NatoSquad.EditorTools
{
    /// <summary>
    /// Cloud iOS builds (.github/workflows/build-ios.yml) pass the Apple team and provisioning profile on the Unity
    /// command line: -iosTeamId TEAMID -iosProfileId PROFILE-UUID -iosProfileType distribution|development.
    /// Unity then writes manual signing into the exported Xcode project for the app target only (not for
    /// UnityFramework), so the Mac job can archive and export a signed .ipa without editing the project.
    /// Without these arguments nothing changes (local builds keep whatever is in Player Settings).
    /// </summary>
    class CiIosSigning : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string team = Arg("-iosTeamId"), profile = Arg("-iosProfileId"), type = Arg("-iosProfileType");
            if (string.IsNullOrEmpty(team) && string.IsNullOrEmpty(profile)) return;

            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
            if (!string.IsNullOrEmpty(profile))
            {
                PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                PlayerSettings.iOS.iOSManualProvisioningProfileID = profile;
                PlayerSettings.iOS.iOSManualProvisioningProfileType =
                    type == "development" ? ProvisioningProfileType.Development : ProvisioningProfileType.Distribution;
            }
            Debug.Log("[NATO Squad CI] iOS signing: team " + team + ", profile " + profile + " (" + (string.IsNullOrEmpty(type) ? "distribution" : type) + ")");
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
