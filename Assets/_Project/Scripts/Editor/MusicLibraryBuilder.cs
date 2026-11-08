using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Audio;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // M5-40: arma MusicLibrary con lo que hay en Audio/Music (`<id>_base.ogg`, `<id>_intensity.ogg`, `<id>.ogg` sin capas y
    // `<id>_beats.json`) y agrega MusicDirector al prefab GameServices. Se puede repetir sin efectos secundarios.
    public static class MusicLibraryBuilder
    {
        private const string MUSIC = "Assets/_Project/Audio/Music/";
        private const string LIBRARY = "Assets/_Project/Data/Audio/MusicLibrary.asset";
        private const string SERVICES = "Assets/_Project/Prefabs/Services/Resources/GameServices.prefab";

        [MenuItem("Gas Safari/Setup/Crear biblioteca de música (M5-40)")]
        public static void Build()
        {
            var tracks = new Dictionary<string, MusicLibrary.Track>();
            foreach (var path in Directory.GetFiles(MUSIC, "*.ogg").Select(p => p.Replace('\\', '/')))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                bool isIntensity = name.EndsWith("_intensity");
                string id = name.EndsWith("_base") ? name.Substring(0, name.Length - 5)
                    : isIntensity ? name.Substring(0, name.Length - 10) : name;
                if (!tracks.TryGetValue(id, out var track))
                    tracks[id] = track = new MusicLibrary.Track { id = id };
                if (isIntensity)
                    track.intensityClip = clip;
                else
                    track.baseClip = clip;
            }
            foreach (var path in Directory.GetFiles(MUSIC, "*_beats.json").Select(p => p.Replace('\\', '/')))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string id = name.Substring(0, name.Length - "_beats".Length);
                if (tracks.TryGetValue(id, out var track))
                    track.beats = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Audio"))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Audio");
            var library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(LIBRARY);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<MusicLibrary>();
                AssetDatabase.CreateAsset(library, LIBRARY);
            }
            library.Set(tracks.Values.Where(t => t.baseClip != null).OrderBy(t => t.id).ToArray());
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(SERVICES);
            try
            {
                var director = root.GetComponentInChildren<MusicDirector>(true);
                if (director == null)
                {
                    var go = new GameObject("Music");
                    go.transform.SetParent(root.transform, false);
                    director = go.AddComponent<MusicDirector>();
                }
                director.Configure(library);
                EditorUtility.SetDirty(director);
                PrefabUtility.SaveAsPrefabAsset(root, SERVICES);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"M5-40 biblioteca de música: {library.Tracks.Length} pistas, {library.Tracks.Count(t => t.intensityClip != null)} con capa de intensidad");
        }
    }
}
