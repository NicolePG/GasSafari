using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M5-26: HazardConfig y prefabs de los obstáculos de W5 (docs/05): H21 Rápidos, H23 Cocodrilo gigante y las compuertas de
    // represa (id H37, provisional: docs/04 las pide sin id).
    public static class W5HazardBuilder
    {
        private const string DATA = "Assets/_Project/Data/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string FAUNA = "Assets/_Project/Art/Fauna/";
        private const string MATS = "Assets/_Project/Art/Materials/Hazards/";

        [MenuItem("Gas Safari/Setup/Crear obstáculos de W5 (M5-26)")]
        public static void Build()
        {
            var h21 = Config("H21_Rapids", "H21", "Rápidos", 0.5f, false, ("accel", 8f));
            var h23 = Config("H23_GiantCroc", "H23", "Cocodrilo gigante", 0f, true, ("speedFactor", 0.9f), ("scareSeconds", 2f));
            var h37 = Config("H37_DamGate", "H37", "Compuerta de represa", 1.5f, false,
                ("closedSeconds", 6f), ("openSeconds", 4f), ("surge", 10f), ("surgeSeconds", 3f));
            AssetDatabase.SaveAssets();

            Prefab("H21_Rapids", () =>
            {
                var root = new GameObject("H21_Rapids");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(14f, 4f, 10f);
                box.center = new Vector3(0f, 1.5f, 0f);
                // Espuma blanca (decorado): rayas largas a lo largo de la corriente.
                var foam = Toon("M_H21_Foam", new Color(0.97f, 0.93f, 0.85f));
                for (int i = 0; i < 6; i++)
                {
                    var streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(streak.GetComponent<Collider>());
                    streak.name = "Espuma";
                    streak.transform.SetParent(root.transform, false);
                    streak.transform.localPosition = new Vector3(-5.5f + i * 2.2f, 0.12f, (i % 2) * 1.5f - 0.75f);
                    streak.transform.localScale = new Vector3(0.35f, 0.04f, 4.5f);
                    streak.GetComponent<MeshRenderer>().sharedMaterial = foam;
                }
                root.AddComponent<RapidsZone>().Configure(h21);
                return root;
            });

            Prefab("H37_DamGate", () =>
            {
                var root = new GameObject("H37_DamGate");
                // Zona de la oleada: aguas abajo de la puerta (z local hacia adelante).
                var zone = root.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.size = new Vector3(14f, 5f, 24f);
                zone.center = new Vector3(0f, 2.5f, 13f);
                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "Puerta";
                door.transform.SetParent(root.transform, false);
                door.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                door.transform.localScale = new Vector3(14f, 5f, 1.2f);
                door.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H37_Door", new Color(0.45f, 0.3f, 0.2f));
                var gate = root.AddComponent<DamGate>();
                gate.Configure(h37);
                gate.Configure(door.transform, door.GetComponent<Collider>(), 0f, 1f);
                return root;
            });

            Prefab("H23_GiantCroc", () =>
            {
                // La raíz es la zona de arranque de la persecución; el cuerpo (con su collider para el Rugido) se mueve solo.
                var root = new GameObject("H23_GiantCroc");
                var zone = root.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.size = new Vector3(14f, 6f, 3f);
                zone.center = new Vector3(0f, 2f, 0f);
                var body = Model(FAUNA, "GiantCroc", "Cocodrilo");
                body.transform.SetParent(root.transform, false);
                var scare = body.AddComponent<BoxCollider>();
                scare.isTrigger = true;
                scare.center = new Vector3(0f, 1.5f, 0f);
                scare.size = new Vector3(6f, 3f, 12f);
                var animator = body.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FAUNA + "Controllers/GiantCroc.controller");
                    animator.applyRootMotion = false;
                }
                var chaser = root.AddComponent<GiantCrocChaser>();
                chaser.Configure(h23);
                chaser.Configure(body.transform, new[] { body.transform.position }, 0.9f, 5f);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fauna);
                return root;
            });
            Debug.Log("M5-26 obstáculos de W5 listos");
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
