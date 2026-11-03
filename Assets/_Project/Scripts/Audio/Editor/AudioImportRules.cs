using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Ajustes de importación de docs/08 §3 según la carpeta.
    public class AudioImportRules : AssetPostprocessor
    {
        private const string ROOT = "Assets/_Project/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(ROOT))
                return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            bool music = assetPath.StartsWith(ROOT + "Music/");
            bool loop = assetPath.Contains("loop");

            if (music)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.5f;
                importer.forceToMono = false;
                settings.preloadAudioData = false;
            }
            else if (loop)
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.quality = 0.6f;
                importer.forceToMono = true;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.7f;
                importer.forceToMono = true;
            }
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            importer.defaultSampleSettings = settings;
        }
    }
}
