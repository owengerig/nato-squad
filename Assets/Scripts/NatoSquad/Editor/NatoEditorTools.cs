using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace NatoSquad.EditorTools
{
    static class NatoEditorTools
    {
        const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem("NATO Squad/Play Game", false, 0)]
        static void Play()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(MainScene);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("NATO Squad/Give 100,000 Test Gold (Play Mode)", false, 50)]
        static void TestGold()
        {
            if (!Application.isPlaying || GameManager.I == null) { Debug.LogWarning("[NATO Squad] Enter Play Mode first."); return; }
            GameManager.I.Save.AddGold(100000);
            GameManager.I.SaveGame();
        }

        [MenuItem("NATO Squad/Unlock All Missions (Play Mode)", false, 51)]
        static void UnlockAll()
        {
            if (!Application.isPlaying || GameManager.I == null) { Debug.LogWarning("[NATO Squad] Enter Play Mode first."); return; }
            for (int i = 0; i < Missions.Count; i++) GameManager.I.Save.SetStars(i, 1);
            GameManager.I.SaveGame();
        }

        [MenuItem("NATO Squad/Delete Save Data", false, 60)]
        static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Delete save data?", "Deletes the local NATO Squad save and settings on this computer.", "Delete", "Cancel")) return;
            SaveSystem.Delete();
            PlayerPrefs.DeleteKey("NatoSquad.Settings");
        }

        [MenuItem("NATO Squad/Open Save Folder", false, 61)]
        static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    /// <summary>
    /// iOS: answers the export-compliance question in Info.plist (the game only uses HTTPS through the OS), so
    /// TestFlight / App Store uploads don't stop to ask.
    /// </summary>
    class IosPostBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform != BuildTarget.iOS) return;
            string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            if (!File.Exists(plistPath)) return;
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }
    }
}
