using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace KiyiDongusu.Editor
{
    public static class ApplicationBranding
    {
        const string IconPath = "Assets/_Game/Branding/WayToGreen-Icon.png";

        [MenuItem("Way to Green/Apply Application Icon")]
        public static void Apply()
        {
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Application icon is missing: " + IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            Set(NamedBuildTarget.Unknown, icon);
            Set(NamedBuildTarget.Standalone, icon);
            AssetDatabase.SaveAssets();
        }

        static void Set(NamedBuildTarget target, Texture2D icon)
        {
            int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Any);
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIcons(target, icons, IconKind.Any);
            var assigned = PlayerSettings.GetIcons(target, IconKind.Any);
            if (assigned.Length == 0) throw new InvalidOperationException("No application icon slots: " + target.TargetName);
            foreach (var value in assigned)
                if (value != icon) throw new InvalidOperationException("Application icon assignment failed.");
            Debug.Log("WAY_TO_GREEN_ICON target=" + target.TargetName + " sizes=" + string.Join(",", sizes));
        }
    }
}
