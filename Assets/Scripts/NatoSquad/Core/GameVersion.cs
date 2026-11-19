using UnityEngine;

namespace NatoSquad
{
    /// <summary>
    /// The game's version, shown on the main menu next to the creator line.
    /// - Version: Application.version (Player Settings → Version), MAJOR.MINOR.PATCH. Starts at 0.0.1.
    /// - Build: whole number that rises with every store upload (Android versionCode / iOS build number).
    /// Change both with the editor menu NATO Squad → Version (Editor/VersionTools.cs); it rewrites Build below.
    /// </summary>
    public static class GameVersion
    {
        public const int Build = 1; // written by Editor/VersionTools.cs; keep it equal to the Android versionCode

        public static string Version => Application.version;

        /// <summary>"v0.0.1"</summary>
        public static string Label => "v" + Version;

        /// <summary>"v0.0.1 (build 1)"</summary>
        public static string LongLabel => "v" + Version + " (build " + Build + ")";
    }

    /// <summary>Web pages the game links to.</summary>
    public static class Links
    {
        public const string PrivacyPolicy = "https://sites.google.com/view/og-software/privacy-policy";
        public const string Support = "https://sites.google.com/view/og-software/support";
        public const string Creator = "OG Software";
    }
}
