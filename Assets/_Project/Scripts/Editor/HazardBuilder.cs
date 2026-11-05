using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // M5-02: HazardConfig y prefabs de los obstáculos de W1 (H01, H03, H04, H05, H07, H24, H27, H28) con los modelos de
    // Art/Environment/W1 y Art/Bosses, y la escena Test/Sandbox_Hazards. Los parámetros marcados "prov." no están en
    // docs/05 y se afinan con QA. Se puede repetir.
    public static class HazardBuilder
    {
        private const string DATA = "Assets/_Project/Data/Hazards/";
        private const string PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string MATERIALS = "Assets/_Project/Art/Materials/Hazards/";
        private const string ENV = "Assets/_Project/Art/Environment/W1/";
        private const string SANDBOX = "Assets/_Project/Scenes/Test/Sandbox_Hazards.unity";
        private const string SANDBOX_SOURCE = "Assets/_Project/Scenes/Test/Sandbox_Raft.unity";

        [MenuItem("Gas Safari/Setup/Crear obstáculos de W1 (M5-02)")]
        public static void Build()
        {
            EnsureFolder(DATA.TrimEnd('/'));
            EnsureFolder(PREFABS.TrimEnd('/'));
            EnsureFolder(MATERIALS.TrimEnd('/'));

            var wall = Config("H01_Wall", "H01", "Pared / orilla", 0.5f, SoundId.None, false, ("bounce", 0.3f));
            var gate = Config("H03_TimedGate", "H03", "Compuerta temporizada", 0.5f, SoundId.GateClose, false,
                ("cycle", 2.5f), ("openFraction", 0.5f), ("pushBack", 12f)); // pushBack: prov.
            var wave = Config("H04_Wave", "H04", "Oleaje", 0.5f, SoundId.None, false,
                ("amplitude", 0.6f), ("push", 20f), ("crestSpeed", 4f)); // crestSpeed: prov.
            var blocker = Config("H05_MovingBlocker", "H05", "Bloqueador móvil", 0.5f, SoundId.None, false,
                ("speed", 2f), ("hit", 30f), ("dwell", 1f)); // dwell: prov.
            var crate = Config("H07_Crate", "H07", "Caja rompible", 0.5f, SoundId.None, false, ("impactToBreak", 20f));
            var crane = Config("H24_Crane", "H24", "Grúa con gancho", 1f, SoundId.None, false,
                ("radius", 3f), ("trackSpeed", 14f), ("impulse", 35f)); // radius, trackSpeed, impulse: prov.
            var trampoline = Config("H27_Trampoline", "H27", "Trampolín / toldo / fuente", 0.5f, SoundId.None, false,
                ("launchSpeed", 10f), ("cooldown", 0.5f));
            var boat = Config("H28_CrossingBoat", "H28", "Barco que cruza", 1f, SoundId.None, false,
                ("speed", 3f), ("hit", 30f), ("dwell", 2.5f)); // speed, dwell: prov.

            // W1-1: chorros (sin ID en docs/05): lateral suave y vertical que eleva.
            var jetSide = Config("Jet_Lateral", "jet", "Chorro lateral suave", 0.5f, SoundId.None, false, ("accel", 3f)); // prov.
            var jetUp = Config("H27_Jet", "H27", "Chorro que eleva", 0.5f, SoundId.None, false, ("launchSpeed", 6f), ("cooldown", 0.5f)); // prov.
            var jetMat = Toon("M_Jet", new Color(0.35f, 0.85f, 1f));

            var wallMat = Toon("M_H01_Wall", new Color(0.55f, 0.6f, 0.68f));
            var waveMat = Toon("M_H04_Wave", new Color(0.45f, 0.8f, 0.95f));
            var shadowMat = Unlit("M_H24_HookShadow", new Color(0.9f, 0.1f, 0.1f));

            // H01
            Prefab("H01_Wall", () =>
            {
                var root = new GameObject("H01_Wall");
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Model";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localScale = new Vector3(10f, 2.5f, 1f);
                cube.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                cube.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
                SetRef(root.AddComponent<BounceWall>(), "config", wall);
                return root;
            });

            // H03
            Prefab("H03_TimedGate", () =>
            {
                var root = Model(ENV + "W1_SluiceGate.fbx", "H03_TimedGate");
                var door = root.transform.Find("W1_SluiceGate/Gate") ?? root.transform.Find("Gate");
                var bounds = LocalBounds(door);
                var box = door.gameObject.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
                var frame = (root.transform.Find("W1_SluiceGate/Static") ?? root.transform.Find("Static"));
                if (frame != null)
                    foreach (var filter in frame.GetComponentsInChildren<MeshFilter>())
                        filter.gameObject.AddComponent<MeshCollider>();
                var hazard = root.AddComponent<TimedGate>();
                SetRef(hazard, "config", gate);
                hazard.Configure(door, box, bounds.size.y + 0.2f);
                EditorUtility.SetDirty(hazard);
                return root;
            });

            // H04
            Prefab("H04_Wave", () =>
            {
                var root = new GameObject("H04_Wave");
                var hazard = root.AddComponent<WaveHazard>();
                SetRef(hazard, "config", wave);
                hazard.Configure(10f, 24f, 5f, waveMat);
                return root;
            });

            // H05
            Prefab("H05_MovingBlocker", () =>
            {
                var root = Model(ENV + "W1_Container_A.fbx", "H05_MovingBlocker");
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 1.3f, 0f);
                box.size = new Vector3(2.6f, 2.6f, 6.2f);
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                var hazard = root.AddComponent<MovingBlocker>();
                SetRef(hazard, "config", blocker);
                hazard.Configure(new Vector3(7f, 0f, 0f), 0f);
                root.AddComponent<HazardTags>().Configure(HazardTag.Heavy);
                return root;
            });

            // H07
            Prefab("H07_Crate", () =>
            {
                var root = Model(ENV + "W1_Crate_A.fbx", "H07_Crate");
                root.transform.GetChild(0).localScale = Vector3.one * 1.6f;
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.8f, 0f);
                box.size = Vector3.one * 1.76f;
                SetRef(root.AddComponent<BreakableCrate>(), "config", crate);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fragile);
                return root;
            });

            // H24
            Prefab("H24_Crane", () =>
            {
                var root = new GameObject("H24_Crane");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Bosses/Crane.fbx"), root.transform);
                model.name = "Model";
                var bones = model.GetComponentsInChildren<Transform>(true);
                var hazard = root.AddComponent<CraneBoss>();
                SetRef(hazard, "config", crane);
                hazard.Configure(root.transform, bones.First(t => t.name == "Trolley"), bones.First(t => t.name == "Hook"), shadowMat);
                SetRef(hazard, "animator", model.GetComponentInChildren<Animator>());
                return root;
            });

            // H27: toldo y fuente
            Prefab("H27_Awning", () =>
            {
                var root = Model(ENV + "W1_Awning.fbx", "H27_Awning");
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 0.5f, 0f);
                trigger.size = new Vector3(4.2f, 2f, 3.3f);
                var hazard = root.AddComponent<Trampoline>();
                SetRef(hazard, "config", trampoline);
                hazard.Configure(root.transform.GetChild(0), 0f);
                return root;
            });
            Prefab("H27_Fountain", () =>
            {
                var root = Model(ENV + "W1_Fountain_Launch.fbx", "H27_Fountain");
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 0.5f, 0f);
                trigger.size = new Vector3(7f, 2f, 7f);
                var hazard = root.AddComponent<Trampoline>();
                SetRef(hazard, "config", trampoline);
                hazard.Configure(null, 10f); // lanza hacia el frente: por encima de la reja
                return root;
            });

            Prefab("Jet_Lateral", () =>
            {
                var root = new GameObject("Jet_Lateral");
                var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basin.name = "Model";
                Object.DestroyImmediate(basin.GetComponent<Collider>()); // solo visual: el chorro empuja, no bloquea
                basin.transform.SetParent(root.transform, false);
                basin.transform.localScale = new Vector3(2.4f, 0.8f, 2.4f);
                basin.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                basin.GetComponent<MeshRenderer>().sharedMaterial = jetMat;
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(3.5f, 1f, 0f);
                trigger.size = new Vector3(6f, 3f, 6f);
                SetRef(root.AddComponent<SoftJet>(), "config", jetSide);
                return root;
            });
            Prefab("H27_Jet", () =>
            {
                var root = new GameObject("H27_Jet");
                var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = "Model";
                Object.DestroyImmediate(column.GetComponent<Collider>());
                column.transform.SetParent(root.transform, false);
                column.transform.localScale = new Vector3(1.8f, 1.2f, 1.8f);
                column.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                column.GetComponent<MeshRenderer>().sharedMaterial = jetMat;
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 0.5f, 0f);
                trigger.size = new Vector3(3f, 2f, 3f);
                var hazard = root.AddComponent<Trampoline>();
                SetRef(hazard, "config", jetUp);
                hazard.Configure(column.transform, 0f);
                return root;
            });

            // H28: ferry y lancha
            Prefab("H28_Ferry", () => Boat(boat, wave, waveMat, "W1_Ferry.fbx", "H28_Ferry", new Vector3(7f, 3f, 16.3f), new Vector3(0f, 1.5f, 0f), 24f));
            Prefab("H28_Speedboat", () => Boat(boat, wave, waveMat, "W1_Speedboat.fbx", "H28_Speedboat", new Vector3(2.9f, 1.5f, 6.2f), new Vector3(0f, 0.6f, 0f), 18f));

            AssetDatabase.SaveAssets();
            BuildSandbox();
            Debug.Log("M5-02 obstáculos de W1 creados");
        }

        // Barco con su estela: la cresta baja por el cauce (el hijo 'Wake' mira a la izquierda del barco).
        private static GameObject Boat(HazardConfig boat, HazardConfig wave, Material waveMat, string fbx, string name, Vector3 size, Vector3 center, float crossing)
        {
            var root = Model(ENV + fbx, name);
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var hazard = root.AddComponent<CrossingBoat>();
            SetRef(hazard, "config", boat);
            hazard.Configure(new Vector3(0f, 0f, crossing), 0f);
            root.AddComponent<HazardTags>().Configure(HazardTag.Heavy);

            var wakeGo = new GameObject("Wake");
            wakeGo.transform.SetParent(root.transform, false);
            wakeGo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var wake = wakeGo.AddComponent<WaveHazard>();
            SetRef(wake, "config", wave);
            wake.Configure(10f, 24f, 0f, waveMat);
            hazard.SetWake(wake);
            return root;
        }

        // ---------------------------------------------------------------- escena de pruebas
        private static void BuildSandbox()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SANDBOX) == null)
                AssetDatabase.CopyAsset(SANDBOX_SOURCE, SANDBOX);

            var previous = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(SANDBOX, OpenSceneMode.Additive);
            try
            {
                var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Hazards");
                if (old != null)
                    Object.DestroyImmediate(old);
                var parent = new GameObject("Hazards");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(parent, scene);
                float x = -30f;
                foreach (var name in new[] { "H01_Wall", "H03_TimedGate", "H04_Wave", "H05_MovingBlocker", "H07_Crate", "H27_Awning", "H27_Fountain", "Jet_Lateral", "H27_Jet", "H28_Speedboat", "H28_Ferry", "H24_Crane" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.transform.SetParent(parent.transform, false);
                    instance.transform.position = new Vector3(x, 0f, 25f);
                    x += 12f;
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---------------------------------------------------------------- utilidades
        private static HazardConfig Config(string file, string id, string displayName, float warning, SoundId sound, bool alwaysDamages,
            params (string key, float value)[] parameters)
        {
            var config = ProvisionalFartFeedbackBuilder.LoadOrCreate<HazardConfig>(DATA + file + ".asset");
            config.id = id;
            config.displayName = displayName;
            config.warningTime = warning;
            config.warningSound = sound;
            config.alwaysDamagesChambers = alwaysDamages;
            config.parameters = parameters.Select(p => new HazardConfig.Parameter { key = p.key, value = p.value }).ToArray();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void Prefab(string name, System.Func<GameObject> make)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = make();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            SetLayerRecursive(root, GameLayers.Hazard);
            PrefabUtility.SaveAsPrefabAsset(root, PREFABS + name + ".prefab");
            Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }

        private static GameObject Model(string fbx, string name)
        {
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx), root.transform);
            model.transform.localPosition = Vector3.zero;
            return root;
        }

        private static Bounds LocalBounds(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            // De mundo a local (sin rotación ni escala en los FBX de props).
            return new Bounds(t.InverseTransformPoint(bounds.center), bounds.size);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        private static void SetRef(Component component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"M5-02 {component.GetType().Name} no tiene el campo {field}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Toon(string name, Color color)
        {
            var path = MATERIALS + name + ".mat";
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

        private static Material Unlit(string name, Color color)
        {
            var path = MATERIALS + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
