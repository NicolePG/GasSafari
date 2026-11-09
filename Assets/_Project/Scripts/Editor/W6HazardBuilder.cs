using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M5-32: HazardConfig y prefabs de lo nuevo del Mundo 6 (docs/05): H30 Coco que cae y el aviso de abismo de H22
    // (AbyssWarning, 30 m antes de cada hueco).
    public static class W6HazardBuilder
    {
        private const string DATA = "Assets/_Project/Data/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string MATS = "Assets/_Project/Art/Materials/Hazards/";

        [MenuItem("Gas Safari/Setup/Crear obstáculos de W6 (M5-32)")]
        public static void Build()
        {
            // docs/05: sombra de aviso 0.8 s, empuje 20, sin daño. Radio de impacto 2.5 m (como la roca H20).
            var h30 = Config("H30_Coconut", "H30", "Coco que cae", 0.8f, ("radius", 2.5f), ("push", 20f));
            // El aviso de abismo reutiliza la configuración de H22 (0.5 s de aviso).
            var h22 = AssetDatabase.LoadAssetAtPath<HazardConfig>(DATA + "H22_Abyss.asset");
            AssetDatabase.SaveAssets();

            Prefab("H30_Coconut", () =>
            {
                var root = new GameObject("H30_Coconut");
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(shadow.GetComponent<Collider>());
                shadow.name = "Sombra";
                shadow.transform.SetParent(root.transform, false);
                shadow.transform.localScale = new Vector3(5f, 0.02f, 5f);
                shadow.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H30_Shadow", new Color(0.55f, 0.05f, 0.05f));
                shadow.SetActive(false);
                var fall = root.AddComponent<CoconutFall>();
                fall.Configure(h30);
                fall.Configure(new Transform[0], 2f, shadow.transform);
                return root;
            });

            Prefab("H22_AbyssWarning", () =>
            {
                var root = new GameObject("H22_AbyssWarning");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(14f, 6f, 3f);
                root.AddComponent<AbyssWarning>().Configure(h22);
                return root;
            });
            Debug.Log("M5-32 obstáculos de W6 listos: H30 y aviso de H22");
        }

        private static HazardConfig Config(string file, string id, string displayName, float warning, params (string key, float value)[] parameters)
        {
            string path = DATA + file + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<HazardConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<HazardConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            config.id = id;
            config.displayName = displayName;
            config.warningTime = warning;
            config.alwaysDamagesChambers = false;
            config.parameters = parameters.Select(p => new HazardConfig.Parameter { key = p.key, value = p.value }).ToArray();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void Prefab(string name, System.Func<GameObject> make)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = make();
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = GameLayers.Hazard;
            PrefabUtility.SaveAsPrefabAsset(root, PREFABS + name + ".prefab");
            Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
            HazardModelBuilder.ApplyIfMapped(name);
        }

        private static Material Toon(string name, Color color)
        {
            string path = MATS + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("GasSafari/SG_Toon"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
