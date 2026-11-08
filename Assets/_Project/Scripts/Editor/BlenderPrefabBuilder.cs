using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Prefabs de lo que sale de Blender fuera de los obstáculos: la fauna (modelo + Animator con su controller + collider) y
    // los 8 sombreros, más el HatAnchorSet con el ancla de cada personaje. Se puede repetir sin efectos secundarios.
    public static class BlenderPrefabBuilder
    {
        private const string FAUNA_ART = "Assets/_Project/Art/Fauna/";
        private const string FAUNA_PREFABS = "Assets/_Project/Prefabs/Fauna/";
        private const string HATS_ART = "Assets/_Project/Art/Cosmetics/Hats/";
        private const string HATS_PREFABS = "Assets/_Project/Prefabs/Cosmetics/Hats/";
        private const string ANCHORS = "Assets/_Project/Data/Cosmetics/HatAnchors.asset";

        [MenuItem("Gas Safari/Setup/Crear prefabs de fauna y sombreros (Blender)")]
        public static void Build()
        {
            BlenderAssetImporter.EnsureFolder(FAUNA_PREFABS.TrimEnd('/'));
            BlenderAssetImporter.EnsureFolder(HATS_PREFABS.TrimEnd('/'));
            BlenderAssetImporter.EnsureFolder("Assets/_Project/Data/Cosmetics");

            int fauna = 0;
            foreach (var path in Directory.GetFiles(FAUNA_ART, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                BuildFauna(Path.GetFileNameWithoutExtension(path), path);
                fauna++;
            }
            int hats = 0;
            foreach (var path in Directory.GetFiles(HATS_ART, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                BuildHat(Path.GetFileNameWithoutExtension(path), path);
                hats++;
            }
            int anchors = BuildAnchors();
            AssetDatabase.SaveAssets();
            Debug.Log($"BLENDERPREFABS fauna {fauna}, sombreros {hats}, anclas {anchors}");
        }

        private static void BuildFauna(string name, string fbx)
        {
            var root = new GameObject("Fauna_" + name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
            model.name = "Model";
            var animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FAUNA_ART + "Controllers/" + name + ".controller");
            animator.applyRootMotion = false;
            // Collider sólido con el tamaño del modelo (la fauna no se atraviesa con la balsa).
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            var box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.3f);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = GameLayers.Hazard;
            PrefabUtility.SaveAsPrefabAsset(root, FAUNA_PREFABS + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildHat(string name, string fbx)
        {
            var root = new GameObject("Hat_" + name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
            model.name = "Model";
            PrefabUtility.SaveAsPrefabAsset(root, HATS_PREFABS + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        // hat_anchors.json: {"personajes": {"Alex": {"headTop": [x, y, z], "scale": s}, ...}}
        private static int BuildAnchors()
        {
            string json = File.ReadAllText(HATS_ART + "hat_anchors.json");
            var rx = new Regex("\"(\\w+)\"\\s*:\\s*\\{\\s*\"headTop\"\\s*:\\s*\\[\\s*([-\\d.eE+]+)\\s*,\\s*([-\\d.eE+]+)\\s*,\\s*([-\\d.eE+]+)\\s*\\]\\s*,\\s*\"scale\"\\s*:\\s*([-\\d.eE+]+)");
            var list = new List<HatAnchorSet.Anchor>();
            foreach (Match m in rx.Matches(json))
            {
                float F(int i) => float.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture);
                list.Add(new HatAnchorSet.Anchor { character = m.Groups[1].Value, headTop = new Vector3(F(2), F(3), F(4)), scale = F(5) });
            }
            var set = AssetDatabase.LoadAssetAtPath<HatAnchorSet>(ANCHORS);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<HatAnchorSet>();
                AssetDatabase.CreateAsset(set, ANCHORS);
            }
            set.Set(list.ToArray());
            EditorUtility.SetDirty(set);
            return list.Count;
        }
    }
}
