using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GasSafari.Editor
{
    // Perfiles de post-proceso por mundo (skill level-builder, "Dirección visual"): el Bloom es lo que hace brillar
    // el neón de W1, los cristales y la lava de W4. Cada nivel pone en su Volume el perfil de su mundo.
    public static class WorldVolumeBuilder
    {
        private const string FOLDER = "Assets/_Project/Settings/Volumes";

        // intensidad, umbral, dispersión, viñeta
        private static readonly (string world, float intensity, float threshold, float scatter, float vignette)[] Worlds =
        {
            ("W1", 2.2f, 0.8f, 0.75f, 0.28f), // noche: neón con Bloom fuerte
            ("W2", 0.5f, 1.0f, 0.60f, 0.15f), // día soleado
            ("W3", 0.9f, 0.9f, 0.70f, 0.22f), // atardecer tropical
            ("W4", 1.8f, 0.8f, 0.75f, 0.32f), // cuevas: cristales cian y lava
            ("W5", 1.0f, 0.9f, 0.70f, 0.20f), // atardecer dorado
            ("W6", 2.2f, 0.8f, 0.75f, 0.30f)  // fiesta nocturna: Bloom fuerte (docs/09)
        };

        [MenuItem("Gas Safari/Setup/Crear perfiles de volumen por mundo")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
                AssetDatabase.CreateFolder("Assets/_Project/Settings", "Volumes");
            foreach (var w in Worlds)
            {
                string path = $"{FOLDER}/Volume_{w.world}.asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, path);
                }
                // Se rehace el contenido conservando el GUID del asset (los niveles lo referencian).
                foreach (var old in profile.components.ToArray())
                {
                    profile.components.Remove(old);
                    Object.DestroyImmediate(old, true);
                }
                if (!profile.TryGet(out Bloom bloom))
                    bloom = AddSaved<Bloom>(profile);
                bloom.active = true;
                bloom.intensity.Override(w.intensity);
                bloom.threshold.Override(w.threshold);
                bloom.scatter.Override(w.scatter);
                bloom.highQualityFiltering.Override(false); // 60 FPS
                if (!profile.TryGet(out Vignette vignette))
                    vignette = AddSaved<Vignette>(profile);
                vignette.active = true;
                vignette.intensity.Override(w.vignette);
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"VOL perfiles creados en {FOLDER}");
        }

        // VolumeProfile.Add solo crea el componente en memoria: hay que guardarlo como sub-asset o se pierde al recargar.
        private static T AddSaved<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(false);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
