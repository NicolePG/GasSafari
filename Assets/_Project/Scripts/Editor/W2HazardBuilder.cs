using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M5-08: HazardConfig y prefabs de los obstáculos de W2 (docs/05): H06, H08, H09, H10, H25, H32 y el poste de Anchor.
    public static class W2HazardBuilder
    {
        private const string DATA = "Assets/_Project/Data/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W2/";
        private const string MATS = "Assets/_Project/Art/Materials/Hazards/";

        [MenuItem("Gas Safari/Setup/Crear obstáculos de W2 (M5-08)")]
        public static void Build()
        {
            var h06 = Config("H06_ExplosiveBarrel", "H06", "Barril explosivo", 1.5f, false, ("radius", 4f), ("push", 35f));
            var h08 = Config("H08_Wind", "H08", "Viento", 1f, false, ("accelMin", 3f), ("accelMax", 5f), ("gust", 2f), ("calm", 3f));
            var h09 = Config("H09_Whirlpool", "H09", "Remolino", 0.5f, false, ("attraction", 4f), ("spin", 60f));
            var h10 = Config("H10_Spikes", "H10", "Pinchos", 0.5f, true);
            var h25 = Config("H25_Tentacles", "H25", "Tentáculos", 1f, false, ("hit", 35f), ("radius", 3f));
            var h32 = Config("H32_FishingNet", "H32", "Red de pesca", 0.5f, false, ("dragMultiplier", 3f));
            AssetDatabase.SaveAssets();

            Prefab("H06_ExplosiveBarrel", () =>
            {
                var root = Model("W2_Barrel_A", "H06_ExplosiveBarrel");
                Box(root, false);
                root.AddComponent<ExplosiveBarrel>().Configure(h06);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fragile);
                Tint(root, new Color(0.85f, 0.2f, 0.15f));
                return root;
            });

            Prefab("H08_Wind", () =>
            {
                var root = new GameObject("H08_Wind");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(16f, 6f, 30f);
                box.center = new Vector3(0f, 2f, 0f);
                // Rayas de viento (decorado): señalan hacia dónde empuja (transform.right).
                var material = Toon("M_H08_Streak", new Color(0.9f, 0.95f, 1f));
                for (int i = 0; i < 8; i++)
                {
                    var streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(streak.GetComponent<Collider>());
                    streak.name = "Streak";
                    streak.transform.SetParent(root.transform, false);
                    streak.transform.localPosition = new Vector3(-4f + (i % 3) * 4f, 3f + (i % 2), -12f + i * 3.4f);
                    streak.transform.localScale = new Vector3(3f, 0.06f, 0.06f);
                    streak.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                root.AddComponent<GasWindZone>().Configure(h08);
                return root;
            });

            Prefab("H09_Whirlpool", () =>
            {
                var root = new GameObject("H09_Whirlpool");
                var sphere = root.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 8f;
                var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(disk.GetComponent<Collider>());
                disk.name = "Vortex";
                disk.transform.SetParent(root.transform, false);
                disk.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                disk.transform.localScale = new Vector3(16f, 0.01f, 16f);
                disk.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H09_Vortex", new Color(0.12f, 0.45f, 0.5f));
                var spin = disk.AddComponent<GasSafari.Utils.Spinner>();
                spin.Configure(Vector3.up, 60f, null);
                root.AddComponent<Whirlpool>().Configure(h09);
                return root;
            });

            Prefab("H10_Spikes", () =>
            {
                var root = Model("W2_Coral_A", "H10_Spikes");
                Box(root, false);
                root.AddComponent<SpikeHazard>().Configure(h10);
                return root;
            });

            Prefab("H25_Tentacles", () =>
            {
                var root = new GameObject("H25_Tentacles");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Fauna/Tentacle.fbx");
                var parts = new Transform[4];
                for (int i = 0; i < 4; i++)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    t.name = "Tentacle_" + (i + 1);
                    t.transform.localPosition = new Vector3(i % 2 == 0 ? -3f : 3f, -1.5f, i * 8f);
                    parts[i] = t.transform;
                }
                var boss = root.AddComponent<TentacleBoss>();
                boss.Configure(h25);
                boss.Configure(parts);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
                return root;
            });

            Prefab("H32_FishingNet", () =>
            {
                var root = Model("W2_Net", "H32_FishingNet");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(8f, 2f, 6f);
                box.center = new Vector3(0f, 0.5f, 0f);
                root.AddComponent<FishingNet>().Configure(h32);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fragile);
                return root;
            });

            Prefab("Anchor_Post", () =>
            {
                var root = Model("W2_AnchorPost", "Anchor_Post");
                Box(root, false);
                root.AddComponent<Anchor>();
                root.AddComponent<HazardTags>().Configure(HazardTag.Anchor);
                return root;
            });
            Debug.Log("M5-08 obstáculos de W2 listos");
        }

        private static HazardConfig Config(string file, string id, string displayName, float warning, bool alwaysDamages,
            params (string key, float value)[] parameters)
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
            config.alwaysDamagesChambers = alwaysDamages;
            config.parameters = parameters.Select(p => new HazardConfig.Parameter { key = p.key, value = p.value }).ToArray();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static GameObject Model(string fbx, string name)
        {
            var root = new GameObject(name);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + fbx + ".fbx");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            model.transform.localPosition = Vector3.zero;
            return root;
        }

        private static void Box(GameObject root, bool trigger)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            var box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.6f);
            box.isTrigger = trigger;
        }

        private static void Tint(GameObject root, Color color)
        {
            var material = Toon("M_H06_Barrel", color);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = material;
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
            HazardModelBuilder.ApplyIfMapped(name); // modelo de Blender (README de Tools/Export/Hazards)
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
