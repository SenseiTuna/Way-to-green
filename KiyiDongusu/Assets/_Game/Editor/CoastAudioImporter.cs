using UnityEditor;
using UnityEngine;
namespace KiyiDongusu.Editor
{
    public sealed class CoastAudioImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/_Game/Resources/Audio/"))return;
            var importer=(AudioImporter)assetImporter; var settings=importer.defaultSampleSettings;
            settings.compressionFormat=AudioCompressionFormat.Vorbis; settings.quality=.65f;
            settings.loadType=assetPath.Contains("Tideglass")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings=settings; importer.loadInBackground=true;
        }
    }
}
