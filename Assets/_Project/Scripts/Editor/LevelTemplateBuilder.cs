using Cinemachine;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.PowerUps;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // Crea Scenes/Pruebas/TEST_LevelTemplate.unity (docs/10 §3). Se ejecuta en una escena aditiva: no toca la escena abierta.
    public static class LevelTemplateBuilder
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";
        private const string PROFILE_PATH = "Assets/_Project/Settings/Volume_Default.asset";
        private const string BALANCE_PATH = "Assets/_Project/Data/Balance/BalanceConfig.asset";
        private const string RAFT_PREFAB = "Assets/_Project/Prefabs/Raft/Raft.prefab";
        private const string AVATAR_PREFAB = "Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab";
        private const string CHECKPOINT_PREFAB = "Assets/_Project/Prefabs/Level/Checkpoint.prefab";
        private const string CAMERA_PREFAB = "Assets/_Project/Prefabs/Level/RaftCameraRig.prefab";
        private const float CHANNEL_LENGTH = 80f;
        private const float CHANNEL_WIDTH = 10f;
        private const float WATER_SIZE = 20f;

        [MenuItem("Gas Safari/Setup/Crear plantilla de nivel")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BALANCE_PATH);
                var water = CreateWater();
                var start = CreateGate<StartGate>("StartGate", Vector3.zero);
                var finish = CreateFinish();
                var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CHECKPOINT_PREFAB));
                checkpoint.name = "Checkpoint_0";
                checkpoint.transform.position = start.transform.position;

                var spawner = new GameObject("RaftSpawner").AddComponent<RaftSpawner>();
                SetField(spawner, "raftPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(RAFT_PREFAB));
                SetField(spawner, "fallbackAvatarPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(AVATAR_PREFAB));
                SetField(spawner, "startGate", start);

                var manager = new GameObject("LevelManager");
                var levelManager = manager.AddComponent<LevelManager>();
                var checkpoints = manager.AddComponent<CheckpointSystem>();
                SetField(checkpoints, "balance", balance);
                SetArray(checkpoints, "checkpoints", checkpoint.GetComponent<Checkpoint>());
                SetField(manager.AddComponent<PowerUpRespawner>(), "checkpoints", checkpoints);
                SetField(levelManager, "balance", balance);
                SetField(levelManager, "spawner", spawner);
                SetField(levelManager, "finishGate", finish);
                SetField(levelManager, "checkpoints", checkpoints);

                var pause = manager.AddComponent<PauseController>();
                SetField(pause, "level", levelManager);
                SetField(pause, "checkpoints", checkpoints);
                var stats = manager.AddComponent<LevelStats>();
                var recorder = manager.AddComponent<ReplayRecorder>();
                var results = manager.AddComponent<ResultsController>();
                SetField(results, "level", levelManager);
                SetField(results, "stats", stats);
                SetField(results, "recorder", recorder);
                CreateFlow();
                CreateCamera();
                CreateLight();
                CreateVolume();
                CreateHud();
                CreateEventSystem();
                PauseMenuBuilder.Populate(pause);
                ResultsScreenBuilder.Populate(results, levelManager);
                water.transform.SetAsLastSibling();

                EnsureFolder("Assets/_Project/Scenes/Levels");
                EditorSceneManager.SaveScene(scene, SCENE_PATH);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject CreateWater()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.layer = GameLayers.Water;
            water.transform.position = new Vector3(0f, 0f, CHANNEL_LENGTH * 0.5f);
            water.transform.localScale = new Vector3(WATER_SIZE, 1f, WATER_SIZE);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ToonMaterialBuilder.WATER_PATH);
            if (material != null)
                water.GetComponent<MeshRenderer>().sharedMaterial = material;
            return water;
        }

        private static T CreateGate<T>(string name, Vector3 position) where T : Component
        {
            var go = new GameObject(name);
            go.transform.position = position;
            return go.AddComponent<T>();
        }

        private static FinishGate CreateFinish()
        {
            var go = new GameObject("FinishGate") { layer = GameLayers.Trigger };
            go.transform.position = new Vector3(0f, 0f, CHANNEL_LENGTH);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(CHANNEL_WIDTH, 6f, 2f);
            return go.AddComponent<FinishGate>();
        }

        private static void CreateFlow()
        {
            var go = new GameObject("Flow");
            var container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Add(new BezierKnot(new float3(0f, 0f, 0f)), TangentMode.Linear);
            spline.Add(new BezierKnot(new float3(0f, 0f, CHANNEL_LENGTH)), TangentMode.Linear);
            var flow = go.AddComponent<FlowSpline>();
            SetField(flow, "width", CHANNEL_WIDTH);
            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
            var so = new SerializedObject(flow);
            so.FindProperty("intensity").animationCurveValue = curve;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera()
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            cam.AddComponent<CinemachineBrain>();
            cam.transform.position = new Vector3(0f, 12f, -12f);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CAMERA_PREFAB));
        }

        private static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // Perfil vacío: cada nivel lo reemplaza por el del mundo (docs/04 Mundos).
        private static void CreateVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PROFILE_PATH);
            }
            var volume = new GameObject("Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void CreateHud()
        {
            var go = new GameObject("HUD");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            HudBuilder.Populate(go);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object element)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = 1;
            array.GetArrayElementAtIndex(0).objectReferenceValue = element;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Levels");
        }
    }
}
