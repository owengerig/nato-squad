using UnityEngine;

namespace NatoSquad
{
    /// <summary>
    /// The game's version, shown on the main menu next to the creator line.
    /// - Version: Application.version (Player Settings → Version), MAJOR.MINOR.PATCH. Reset to 0.1.0 with the other OG Software apps.
    /// - Build: whole number that rises with every store upload (Android versionCode / iOS build number).
    /// Change both with the editor menu NATO Squad → Version (Editor/VersionTools.cs); it rewrites Build below.
    /// </summary>
    public static class GameVersion
    {
        public const int Build = 2; // written by Editor/VersionTools.cs; keep it equal to the Android versionCode

        public static string Version => Application.version;

        /// <summary>"v0.1.0"</summary>
        public static string Label => "v" + Version;

        /// <summary>"v0.1.0 (build 2)"</summary>
        public static string LongLabel => "v" + Version + " (build " + Build + ")";

        /// <summary>Bottom-left line on the main menu, the same in every OG Software app: "Created by OG Software v0.1.0".</summary>
        public static string Credit => "Created by " + Links.Creator + " v" + Version;
    }

    /// <summary>Web pages the game links to.</summary>
    public static class Links
    {
        public const string PrivacyPolicy = "https://sites.google.com/view/og-software/privacy-policy";
        public const string Support = "https://sites.google.com/view/og-software/support";
        public const string Creator = "OG Software";
    }
}
