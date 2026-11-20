using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OGSoftware.Platform.EditorTools
{
    /// <summary>
    /// Build setup for OGPlatform, so nothing has to be done by hand in Xcode or Gradle:
    /// - iOS: links GameKit and turns on the Game Center and iCloud (Documents) capabilities. iCloud is what
    ///   Game Center saved games are stored in. The App ID needs both capabilities in the Apple developer portal.
    /// - Android: adds the Play Games Services v2 library, the game's Play Games App ID
    ///   (OGPlatformProject.AndroidAppId, from Play Console → Play Games Services → Configuration) and an R8 keep
    ///   rule for the bridge class.
    /// </summary>
    class OGPlatformIosBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 200;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform != BuildTarget.iOS) return;
            string path = report.summary.outputPath;
            string projPath = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(path);
            var proj = new UnityEditor.iOS.Xcode.PBXProject();
            proj.ReadFromFile(projPath);
            string main = proj.GetUnityMainTargetGuid();
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "GameKit.framework", false);
            proj.WriteToFile(projPath);

            var caps = new UnityEditor.iOS.Xcode.ProjectCapabilityManager(projPath, "Unity-iPhone/" + EntitlementsName + ".entitlements", null, main);
            caps.AddGameCenter();
            caps.AddiCloud(false, true, false, true, null);
            caps.WriteToFile();
            Debug.Log("[OGPlatform] Game Center + iCloud added to the Xcode project.");
#endif
        }

        static string EntitlementsName => string.IsNullOrEmpty(PlayerSettings.productName) ? "app" : PlayerSettings.productName.Replace(" ", "");
    }

#if UNITY_ANDROID
    class OGPlatformAndroidBuild : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        public const string PlayGamesVersion = "20.1.2";
        const string AndroidNs = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 200;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // path is the unityLibrary module, which also compiles Assets/Plugins/Android/OGPlatform/OGPlayGames.java
            string appId = OGPlatformProject.AndroidAppId;
            if (string.IsNullOrEmpty(appId) || appId.StartsWith("TODO"))
                Debug.LogWarning("[OGPlatform] OGPlatformProject.AndroidAppId isn't set: Play Games sign-in will fail until it is.");

            string gradle = Path.Combine(path, "build.gradle");
            if (File.Exists(gradle) && !File.ReadAllText(gradle).Contains("play-services-games-v2"))
                File.AppendAllText(gradle, "\n// added by OGPlatformBuild\ndependencies {\n    implementation 'com.google.android.gms:play-services-games-v2:" + PlayGamesVersion + "'\n}\n");

            string values = Path.Combine(path, "src", "main", "res", "values");
            Directory.CreateDirectory(values);
            File.WriteAllText(Path.Combine(values, "og_platform.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n    <string name=\"og_game_services_project_id\" translatable=\"false\">" +
                System.Security.SecurityElement.Escape(appId ?? "") + "</string>\n</resources>\n");

            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (File.Exists(manifestPath))
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(manifestPath);
                var manifest = doc.DocumentElement;
                var app = manifest.SelectSingleNode("application") as System.Xml.XmlElement;
                if (app == null) { app = doc.CreateElement("application"); manifest.AppendChild(app); }
                bool has = false;
                foreach (System.Xml.XmlNode n in app.ChildNodes)
                    if (n is System.Xml.XmlElement e && e.Name == "meta-data" && e.GetAttribute("name", AndroidNs) == "com.google.android.gms.games.APP_ID") has = true;
                if (!has)
                {
                    var meta = doc.CreateElement("meta-data");
                    var nameAttr = doc.CreateAttribute("android", "name", AndroidNs); nameAttr.Value = "com.google.android.gms.games.APP_ID";
                    var valueAttr = doc.CreateAttribute("android", "value", AndroidNs); valueAttr.Value = "@string/og_game_services_project_id";
                    meta.Attributes.Append(nameAttr);
                    meta.Attributes.Append(valueAttr);
                    app.AppendChild(meta);
                    doc.Save(manifestPath);
                }
            }

            string proguard = Path.Combine(path, "proguard-unity.txt");
            const string keep = "-keep class com.ogsoftware.platform.** { *; }";
            if (File.Exists(proguard) && !File.ReadAllText(proguard).Contains(keep)) File.AppendAllText(proguard, "\n" + keep + "\n");
            Debug.Log("[OGPlatform] Play Games Services added to the Gradle project.");
        }
    }
#endif
}
