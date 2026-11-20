using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

namespace OGSoftware.EditorTools
{
    /// <summary>
    /// Keeps this project on the Universal Render Pipeline (2D Renderer), the same setup as 2D Clash. Unity 6.5+
    /// deprecates the Built-in Render Pipeline, which is what the Unity Hub warning was about.
    ///
    /// Runs by itself every time the editor loads scripts:
    /// 1. If the URP package isn't installed, it installs it (Unity reloads, then step 2 runs).
    /// 2. It makes Assets/Settings/Rendering/RenderPipeline.asset (which uses Renderer2D.asset) the render pipeline
    ///    in Graphics settings and in every Quality level, and saves.
    /// Nothing happens once that's done. Uses no URP types itself, so it compiles before URP is installed.
    /// Menu: OG Software → Render Pipeline → Check URP Setup.
    /// </summary>
    [InitializeOnLoad]
    static class URPSetup
    {
        const string Package = "com.unity.render-pipelines.universal";
        const string PipelinePath = "Assets/Settings/Rendering/RenderPipeline.asset";
        const string InstallRequestedKey = "OGSoftware.URPSetup.InstallRequested";

        static URPSetup()
        {
            // after the editor has finished loading (asset database ready)
            EditorApplication.delayCall += () => Ensure(false);
        }

        [MenuItem("OG Software/Render Pipeline/Check URP Setup")]
        static void CheckFromMenu() => Ensure(true);

        static void Ensure(bool verbose)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!PackageListed())
            {
                if (SessionState.GetBool(InstallRequestedKey, false)) return; // already asked this session
                SessionState.SetBool(InstallRequestedKey, true);
                Debug.Log("[URP setup] Installing the Universal Render Pipeline package; the editor reloads when it's done.");
                Client.Add(Package);
                return;
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                // package listed but not imported yet (it resolves after the next reload), or the asset is missing
                if (verbose) Debug.LogWarning("[URP setup] " + PipelinePath + " couldn't be loaded yet. If URP just installed, it's picked up after the reload.");
                return;
            }

            bool changed = false;
            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                changed = true;
            }

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) == pipeline) continue;
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
                changed = true;
            }
            if (QualitySettings.GetQualityLevel() != current) QualitySettings.SetQualityLevel(current, false);

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[URP setup] Render pipeline set to " + PipelinePath + " in Graphics settings and all " +
                          QualitySettings.names.Length + " Quality levels. Commit ProjectSettings/ to keep it.");
            }
            else if (verbose)
            {
                Debug.Log("[URP setup] Already on URP (" + pipeline.name + ") in Graphics settings and every Quality level.");
            }
        }

        static bool PackageListed()
        {
            try { return File.ReadAllText("Packages/manifest.json").Contains("\"" + Package + "\""); }
            catch { return false; }
        }
    }
}
