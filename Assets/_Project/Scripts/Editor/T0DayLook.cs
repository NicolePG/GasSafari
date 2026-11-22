using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // T-0 (docs/04, tabla Mundos): piscina del zoológico de día, celestes y blancos. Las paredes del cauce usaban el material
    // genérico M_H01_Wall (gris azulado con sombra morada: de día se veían azul marino) y los fondos son las piezas de W1, hechas
    // para la ciudad de noche (fachadas azul marino y ventanas encendidas). Aquí: paredes de azulejo blanco celeste y copias de día
    // de los materiales de los fondos (más claros, sin emisión), sin tocar los de W1.
    public static class T0DayLook
    {
        private const string WALL_MATERIAL = WorldLook.MATS + "M_Wall_T0.mat";
        private const string BACKDROP_MATERIALS = "Assets/_Project/Art/Environment/Backdrops/T0/Materials";
        private static readonly Color WallColor = new Color(0.93f, 0.97f, 1f);
        private static readonly Color WallShadow = new Color(0.62f, 0.8f, 0.95f);
        private static readonly Color DayShadow = new Color(0.68f, 0.8f, 0.93f);
        private static readonly Color Glass = new Color(0.72f, 0.87f, 0.97f);

        public static void Apply(Scene scene, Transform visualRoot)
        {
            Walls(scene);
            Backdrops(visualRoot);
        }

        private static void Walls(Scene scene)
        {
            var material = LoadOrCreate(WALL_MATERIAL, AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Hazards/M_H01_Wall.mat"));
            material.SetColor("_BaseColor", WallColor);
            material.SetColor("_ShadowColor", WallShadow);
            EditorUtility.SetDirty(material);
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.gameObject.name == "Wall" && r.gameObject.layer == Core.GameLayers.Environment)
                        r.sharedMaterial = material;
                }
            }
        }

        private static void Backdrops(Transform visualRoot)
        {
            var backdrops = visualRoot.Find("Backdrops");
            if (backdrops == null)
                return;
            EnsureFolder(BACKDROP_MATERIALS);
            var cache = new Dictionary<Material, Material>();
            foreach (var r in backdrops.GetComponentsInChildren<Renderer>(true))
            {
                var materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var night = materials[i];
                    if (night == null)
                        continue;
                    if (!cache.TryGetValue(night, out var day))
                    {
                        day = DayVariant(night);
                        cache[night] = day;
                    }
                    materials[i] = day;
                }
                r.sharedMaterials = materials;
            }
        }

        private static Material DayVariant(Material night)
        {
            var day = LoadOrCreate($"{BACKDROP_MATERIALS}/{night.name}.mat", night);
            day.CopyPropertiesFromMaterial(night);
            var color = night.HasProperty("_BaseColor") ? night.GetColor("_BaseColor") : Color.gray;
            if (night.name.StartsWith("E_"))
            {
                // Ventanas, neón y faroles: de día son vidrio y pintura, no luz.
                color = Color.Lerp(color, Glass, 0.7f);
            }
            else
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);
                color = Color.HSVToRGB(h, s * 0.7f, Mathf.Lerp(v, 1f, 0.55f));
            }
            day.SetColor("_BaseColor", color);
            if (day.HasProperty("_ShadowColor"))
                day.SetColor("_ShadowColor", DayShadow);
            if (day.HasProperty("_EmissionColor"))
                day.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(day);
            return day;
        }

        private static Material LoadOrCreate(string path, Material source)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            material = source != null ? new Material(source) : new Material(Shader.Find("GasSafari/SG_Toon"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
