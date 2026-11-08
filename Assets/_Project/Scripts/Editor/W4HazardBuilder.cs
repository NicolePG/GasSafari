using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M5-20: HazardConfig y prefabs de los obstáculos de W4 (docs/05): H17, H18, H19, H20, H22, H26, H31, H36 (géiser de gas dorado) y los pinchos de cristal.
    public static class W4HazardBuilder
    {
        private const string DATA = "Assets/_Project/Data/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W4/";
        private const string FAUNA = "Assets/_Project/Art/Fauna/";
        private const string MATS = "Assets/_Project/Art/Materials/Hazards/";

        [MenuItem("Gas Safari/Setup/Crear obstáculos de W4 (M5-20)")]
        public static void Build()
        {
            var h17 = Config("H17_Waterfall", "H17", "Cascada", 0.5f, false, ("drop", 3f));
            var h18 = Config("H18_Dark", "H18", "Zona oscura", 0.5f, false, ("visibility", 6f));
            var h19 = Config("H19_Geyser", "H19", "Géiser", 0.5f, false, ("cycle", 3f), ("launch", 12f));
            var h20 = Config("H20_RockFall", "H20", "Roca que cae", 1f, true, ("radius", 2.5f));
            var h22 = Config("H22_Abyss", "H22", "Abismo", 0.5f, false);
            var h36 = Config("H36_GoldGasGeyser", "H36", "Géiser de gas dorado", 0f, false, ("cooldown", 4f));
            var h26 = Config("H26_Bats", "H26", "Murciélagos", 1f, false, ("kick", 3f));
            var h31 = Config("H31_Current", "H31", "Corriente cruzada", 0.5f, false, ("accel", 4f));
            var h10 = AssetDatabase.LoadAssetAtPath<HazardConfig>(DATA + "H10_Spikes.asset");
            AssetDatabase.SaveAssets();

            Prefab("H17_Waterfall", () =>
            {
                var root = new GameObject("H17_Waterfall");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(14f, 5f, 14f);
                root.AddComponent<WaterfallEdge>().Configure(h17);
                return root;
            });

            Prefab("H18_Dark", () =>
            {
                var root = new GameObject("H18_Dark");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(30f, 12f, 60f);
                root.AddComponent<DarkZone>().Configure(6f, new Light[0], 0f);
                return root;
            });

            Prefab("H19_Geyser", () =>
            {
                var root = Model(ENV, "W4_GeyserVent", "H19_Geyser");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(5f, 4f, 5f);
                box.center = new Vector3(0f, 1.5f, 0f);
                var steam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(steam.GetComponent<Collider>());
                steam.name = "Vapor";
                steam.transform.SetParent(root.transform, false);
                steam.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                steam.transform.localScale = new Vector3(1.6f, 2.5f, 1.6f);
                steam.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H19_Steam", new Color(0.9f, 0.95f, 1f));
                var geyser = root.AddComponent<Geyser>();
                geyser.Configure(h19);
                geyser.Configure(steam.transform, 0f);
                return root;
            });

            Prefab("H20_RockFall", () =>
            {
                var root = new GameObject("H20_RockFall");
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(shadow.GetComponent<Collider>());
                shadow.name = "Sombra";
                shadow.transform.SetParent(root.transform, false);
                shadow.transform.localScale = new Vector3(5f, 0.02f, 5f);
                shadow.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H20_Shadow", new Color(0.55f, 0.05f, 0.05f));
                shadow.SetActive(false);
                var rock = root.AddComponent<RockFall>();
                rock.Configure(h20);
                rock.Configure(new Transform[0], 3f, shadow.transform);
                root.AddComponent<HazardTags>().Configure(HazardTag.Heavy);
                return root;
            });

            Prefab("H22_Abyss", () =>
            {
                var root = new GameObject("H22_Abyss");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(40f, 4f, 40f);
                root.AddComponent<Abyss>();
                return root;
            });

            // H36: pozo dorado con una columna de gas; llena el medidor de equipo. Con el modelo de Blender (H36_GoldGasGeyser)
            // lo reemplaza la integración de modelos de obstáculos.
            Prefab("H36_GoldGasGeyser", () =>
            {
                var root = new GameObject("H36_GoldGasGeyser");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(6f, 5f, 6f);
                box.center = new Vector3(0f, 2f, 0f);
                var gold = Toon("M_H36_Gold", new Color(1f, 0.78f, 0.15f));
                gold.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.15f) * 2.5f);
                var well = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(well.GetComponent<Collider>());
                well.name = "Pozo";
                well.transform.SetParent(root.transform, false);
                well.transform.localScale = new Vector3(4f, 0.15f, 4f);
                well.GetComponent<MeshRenderer>().sharedMaterial = gold;
                var jet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(jet.GetComponent<Collider>());
                jet.name = "Chorro";
                jet.transform.SetParent(root.transform, false);
                jet.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                jet.transform.localScale = new Vector3(1.4f, 2.5f, 1.4f);
                jet.GetComponent<MeshRenderer>().sharedMaterial = gold;
                var geyser = root.AddComponent<GoldGasGeyser>();
                geyser.Configure(h36);
                geyser.Configure(jet.transform);
                return root;
            });

            Prefab("H26_Bats", () =>
            {
                var root = new GameObject("H26_Bats");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(8f, 4f, 8f);
                box.center = new Vector3(0f, 2f, 0f);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(FAUNA + "Bat.fbx");
                for (int i = 0; i < 5; i++)
                {
                    var bat = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    bat.name = "Murcielago_" + i;
                    float a = i * Mathf.PI * 2f / 5f;
                    bat.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.6f, 3f + (i % 2) * 0.6f, Mathf.Sin(a) * 1.6f);
                }
                var swarm = root.AddComponent<BatSwarm>();
                swarm.Configure(h26);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
                return root;
            });

            Prefab("H31_Current", () =>
            {
                var root = new GameObject("H31_Current");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(14f, 5f, 14f);
                box.center = new Vector3(0f, 1.5f, 0f);
                root.AddComponent<CurrentZone>().Configure(h31);
                return root;
            });

            Prefab("H10_CrystalSpikes", () =>
            {
                var root = Model(ENV, "W4_Crystal_A", "H10_CrystalSpikes");
                Box(root, false);
                root.AddComponent<SpikeHazard>().Configure(h10);
                return root;
            });
            Debug.Log("M5-20 obstáculos de W4 listos");
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

        private static GameObject Model(string folder, string fbx, string name)
        {
            var root = new GameObject(name);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(folder + fbx + ".fbx");
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
