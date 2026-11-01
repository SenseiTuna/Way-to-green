using UnityEditor;

namespace KiyiDongusu.Editor
{
    public sealed class GameArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Game/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
        }
    }
}
