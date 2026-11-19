using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NatoSquad.EditorTools
{
    /// <summary>
    /// NATO Squad → Version: raises the version (semantic MAJOR.MINOR.PATCH) and the build number together.
    /// - Bump Patch: fixes (0.0.1 → 0.0.2).
    /// - Bump Minor: new features and content (0.0.2 → 0.1.0).
    /// - Bump Major: big releases (0.1.0 → 1.0.0).
    /// - Bump Build Only: another upload of the same version (stores need a new build number every upload).
    /// Each sets Player Settings → Version, the Android versionCode, the iOS build number and GameVersion.Build to
    /// the same values. Commit ProjectSettings.asset and GameVersion.cs afterwards.
    /// Before every build, a check makes sure they all still agree.
    /// </summary>
    class VersionTools : IPreprocessBuildWithReport
    {
        const string VersionFile = "Assets/Scripts/NatoSquad/Core/GameVersion.cs";

        [MenuItem("NATO Squad/Version/Show Current Version", false, 200)]
        static void Show()
        {
            EditorUtility.DisplayDialog("NATO Squad version",
                "Version: " + PlayerSettings.bundleVersion + "\nBuild: " + PlayerSettings.Android.bundleVersionCode +
                " (Android versionCode), " + PlayerSettings.iOS.buildNumber + " (iOS build)\nShown in the game as: v" +
                PlayerSettings.bundleVersion + " (build " + FileBuild() + ")", "OK");
        }

        [MenuItem("NATO Squad/Version/Bump Patch (fixes)", false, 211)] static void Patch() { Bump(2); }
        [MenuItem("NATO Squad/Version/Bump Minor (new features)", false, 212)] static void Minor() { Bump(1); }
        [MenuItem("NATO Squad/Version/Bump Major (big release)", false, 213)] static void Major() { Bump(0); }
        [MenuItem("NATO Squad/Version/Bump Build Only (re-upload)", false, 230)] static void BuildOnly() { Bump(-1); }

        /// <param name="part">0 major, 1 minor, 2 patch, -1 only the build number.</param>
        static void Bump(int part)
        {
            var v = Parse(PlayerSettings.bundleVersion);
            if (part >= 0)
            {
                v[part]++;
                for (int i = part + 1; i < 3; i++) v[i] = 0;
            }
            string version = v[0] + "." + v[1] + "." + v[2];
            int build = Mathf.Max(PlayerSettings.Android.bundleVersionCode, FileBuild()) + 1;
            if (!EditorUtility.DisplayDialog("NATO Squad version",
                    "Change the version from " + PlayerSettings.bundleVersion + " to " + version + ", build " + build + "?", "Change", "Cancel")) return;
            Apply(version, build);
        }

        static void Apply(string version, int build)
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = build;
            PlayerSettings.iOS.buildNumber = build.ToString();
            WriteFileBuild(build);
            AssetDatabase.SaveAssets();
            Debug.Log("[NATO Squad] version " + version + " (build " + build + ")");
        }

        static int[] Parse(string s)
        {
            var v = new int[3];
            var parts = (s ?? "").Split('.');
            for (int i = 0; i < 3 && i < parts.Length; i++) int.TryParse(parts[i], out v[i]);
            return v;
        }

        static int FileBuild()
        {
            if (!File.Exists(VersionFile)) return 0;
            var m = Regex.Match(File.ReadAllText(VersionFile), @"public const int Build = (\d+);");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        static void WriteFileBuild(int build)
        {
            if (!File.Exists(VersionFile)) return;
            string s = File.ReadAllText(VersionFile);
            s = Regex.Replace(s, @"public const int Build = \d+;", "public const int Build = " + build + ";");
            File.WriteAllText(VersionFile, s);
            AssetDatabase.ImportAsset(VersionFile);
        }

        public int callbackOrder => 0;

        /// <summary>Before a build: the version must be MAJOR.MINOR.PATCH and the build numbers must agree.</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!Regex.IsMatch(PlayerSettings.bundleVersion, @"^\d+\.\d+\.\d+$"))
                Debug.LogWarning("[NATO Squad] version \"" + PlayerSettings.bundleVersion + "\" isn't MAJOR.MINOR.PATCH; use NATO Squad → Version.");
            int fileBuild = FileBuild(), android = PlayerSettings.Android.bundleVersionCode;
            if (fileBuild != android || PlayerSettings.iOS.buildNumber != android.ToString())
                Debug.LogWarning("[NATO Squad] build numbers differ (game shows " + fileBuild + ", Android " + android + ", iOS " +
                                 PlayerSettings.iOS.buildNumber + "); use NATO Squad → Version → Bump Build Only to line them up.");
        }
    }
}
