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
            // Las ráfagas y cargas de pedo son loops aunque su nombre no diga "loop" (fart_blast_<pj>_nn).
            bool loop = assetPath.Contains("loop") || assetPath.Contains("fart_blast") || assetPath.Contains("fart_charge");

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
