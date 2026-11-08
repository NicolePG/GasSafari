using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M5-14: HazardConfig y prefabs de los obstáculos de W3 (docs/05): H11, H12, H13, H14, H15, H16 y H29.
    public static class W3HazardBuilder
    {
        private const string DATA = "Assets/_Project/Data/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W3/";
        private const string FAUNA = "Assets/_Project/Art/Fauna/";
        private const string MATS = "Assets/_Project/Art/Materials/Hazards/";

        [MenuItem("Gas Safari/Setup/Crear obstáculos de W3 (M5-14)")]
        public static void Build()
        {
            var h11 = Config("H11_SandBar", "H11", "Banco de arena", 0.5f, false, ("minSpeed", 3f), ("groundTime", 2f));
            var h12 = Config("H12_Vine", "H12", "Liana", 0.5f, false, ("burnTime", 0.5f));
            var h13 = Config("H13_SpinningLog", "H13", "Tronco giratorio", 0.5f, false, ("spin", 90f), ("hit", 25f));
            var h14 = Config("H14_Pit", "H14", "Fosa", 1f, false, ("kick", 3f));
            var h15 = Config("H15_Crocodile", "H15", "Cocodrilo", 1f, true, ("wakeDistance", 8f), ("speed", 3f), ("biteDistance", 2.5f), ("retreatTime", 4f));
            var h16 = Config("H16_Rhythm", "H16", "Obstáculo rítmico", 0.5f, false, ("hit", 20f), ("bpmA", 110f), ("bpmB", 128f));
            var h16jet = Config("H16_Jet", "H16", "Chorro rítmico", 0f, false, ("accel", 4f));
            var h29 = Config("H29_Crab", "H29", "Cangrejo", 0.5f, false, ("drain", 20f), ("stayTime", 3f));
            AssetDatabase.SaveAssets();

            Prefab("H11_SandBar", () =>
            {
                var root = new GameObject("H11_SandBar");
                var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(disk.GetComponent<Collider>());
                disk.name = "Sand";
                disk.transform.SetParent(root.transform, false);
                disk.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                disk.transform.localScale = new Vector3(8f, 0.12f, 8f);
                disk.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H11_Sand", new Color(0.93f, 0.8f, 0.5f));
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(8f, 2f, 8f);
                root.AddComponent<SandBar>().Configure(h11);
                return root;
            });

            Prefab("H12_Vine", () =>
            {
                var root = Model(ENV, "W3_Liana", "H12_Vine");
                Box(root, false);
                root.AddComponent<BurnableWall>().Configure(h12);
                root.AddComponent<HazardTags>().Configure(HazardTag.Burnable);
                return root;
            });

            Prefab("H13_SpinningLog", () =>
            {
                var root = Model(ENV, "W3_Log_A", "H13_SpinningLog");
                Box(root, false);
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                root.AddComponent<SpinningLog>().Configure(h13);
                return root;
            });

            Prefab("H14_Pit", () =>
            {
                var root = new GameObject("H14_Pit");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(8f, 4f, 8f);
                box.center = new Vector3(0f, 1f, 0f);
                var bush = Model(ENV, "W3_Fern", "Arbusto");
                bush.transform.SetParent(root.transform, false);
                bush.transform.localScale = Vector3.one * 1.6f;
                var fossa = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FAUNA + "Fossa.fbx"), root.transform);
                fossa.name = "Fosa";
                fossa.transform.localPosition = new Vector3(0f, -0.2f, 0f);
                var pit = root.AddComponent<PitAmbush>();
                pit.Configure(h14);
                pit.Configure(bush.transform);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
                return root;
            });

            Prefab("H15_Crocodile", () =>
            {
                var root = Model(FAUNA, "Croc", "H15_Crocodile");
                Box(root, false);
                root.AddComponent<Crocodile>().Configure(h15);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
                return root;
            });

            Prefab("H16_Rhythm", () =>
            {
                var root = new GameObject("H16_Rhythm");
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(block.GetComponent<Collider>());
                block.name = "Bloque";
                block.transform.SetParent(root.transform, false);
                block.transform.localScale = new Vector3(3f, 1.4f, 1.2f);
                block.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H16_Beat", new Color(0.97f, 0.5f, 0f));
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(3f, 1.4f, 1.2f);
                box.isTrigger = true;
                root.AddComponent<Rigidbody>().isKinematic = true;
                root.AddComponent<RhythmObstacle>().Configure(h16);
                return root;
            });

            // W3-5: tambor que late con la música (decorado) y chorro que brota en los pulsos marcados.
            Prefab("H16_Drum", () =>
            {
                var root = new GameObject("H16_Drum");
                var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.name = "Cuerpo";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(1.8f, 0.9f, 1.8f);
                body.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H16_Drum", new Color(0.78f, 0.35f, 0.1f));
                var skin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(skin.GetComponent<Collider>());
                skin.name = "Parche";
                skin.transform.SetParent(body.transform, false);
                skin.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                skin.transform.localScale = new Vector3(0.92f, 0.05f, 0.92f);
                skin.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H16_DrumSkin", new Color(0.98f, 0.9f, 0.7f));
                root.AddComponent<RhythmDrum>().Configure(body.transform);
                return root;
            });

            Prefab("H16_Jet", () =>
            {
                var root = new GameObject("H16_Jet");
                var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(basin.GetComponent<Collider>());
                basin.name = "Base";
                basin.transform.SetParent(root.transform, false);
                basin.transform.localScale = new Vector3(2.4f, 0.4f, 2.4f);
                basin.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                basin.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H16_Beat", new Color(0.97f, 0.5f, 0f));
                var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(column.GetComponent<Collider>());
                column.name = "Chorro";
                column.transform.SetParent(root.transform, false);
                column.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                column.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
                column.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H16_JetColumn", new Color(0.97f, 0.35f, 0.7f));
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(3.5f, 1f, 0f);
                trigger.size = new Vector3(6f, 3f, 6f);
                var jet = root.AddComponent<RhythmJet>();
                jet.Configure(h16jet);
                jet.Configure(column.transform, 4, 5);
                return root;
            });

            Prefab("H29_Crab", () =>
            {
                var root = Model(FAUNA, "Crab", "H29_Crab");
                var sphere = root.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 3.5f;
                root.AddComponent<CrabHazard>().Configure(h29);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
                return root;
            });
            Debug.Log("M5-14 obstáculos de W3 listos");
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
