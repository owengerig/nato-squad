using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NatoSquad.EditorTools
{
    /// <summary>
    /// The app icon (Assets/Branding): AppIcon.png (1024×1024, no transparency, as the App Store requires) is the default icon
    /// for every platform. On Android, the adaptive icon uses AppIcon_AdaptiveBackground / AppIcon_AdaptiveForeground.
    /// Applied once automatically when the project opens (if no icon is set yet); menu: NATO Squad > Apply App Icons.
    /// </summary>
    [InitializeOnLoad]
    static class AppIconSetup
    {
        const string Folder = "Assets/Branding/";
        const string Icon = Folder + "AppIcon.png";
        const string AdaptiveBg = Folder + "AppIcon_AdaptiveBackground.png";
        const string AdaptiveFg = Folder + "AppIcon_AdaptiveForeground.png";

        static AppIconSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
                if (current == null || current.Length == 0 || current.All(t => t == null) || !IosIconsSet()) Apply();
            };
        }

        /// <summary>True when every iOS icon slot (including the 1024 App Store icon) has a texture, or iOS support isn't installed.</summary>
        public static bool IosIconsSet()
        {
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
                foreach (var pi in PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind))
                    if (pi.GetTexture() == null) return false;
            return true;
        }

        [MenuItem("NATO Squad/Apply App Icons")]
        public static void Apply()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Icon);
            if (icon == null) { Debug.LogWarning("[NATO Squad] " + Icon + " not found (not imported yet?)."); return; }

            // the default icon: Unity scales it to every size iOS, Android and desktop need (including the 1024 App Store icon)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            // Android adaptive icon: background + foreground layers (Android 8+ masks them into circles, squircles, ...)
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveBg);
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveFg);
            if (bg != null && fg != null)
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
                {
                    var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                    bool adaptive = kind.ToString().Contains("Adaptive");
                    foreach (var pi in icons)
                    {
                        if (adaptive && pi.maxLayerCount >= 2) pi.SetTextures(bg, fg);
                        else pi.SetTexture(icon);
                    }
                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
                }
            }
            // iOS: fill every slot explicitly, including the 1024x1024 App Store ("marketing") icon, which App Store Connect
            // rejects a build without. AppIcon.png is opaque, as Apple requires.
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (var pi in icons) pi.SetTexture(icon);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[NATO Squad] App icons applied. Commit ProjectSettings/ProjectSettings.asset.");
        }
    }

    /// <summary>Import settings for the icon images: full quality, no mipmaps, no resizing.</summary>
    class BrandingImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Branding/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = assetPath.EndsWith("Foreground.png");
        }
    }
}
