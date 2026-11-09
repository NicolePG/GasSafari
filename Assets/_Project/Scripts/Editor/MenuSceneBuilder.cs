using System.IO;
using System.Linq;
using GasSafari.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M6-01: importa la escena 3D del título (Tools/Export/Menu/TitleScene.fbx) y arma la escena 01_Title: el fondo animado
    // (balsa con los cinco animales en la piscina del zoológico, 12 s en bucle), su cámara y el MenuApp que dibuja los menús.
    public static class MenuSceneBuilder
    {
        public const string SCENE_PATH = "Assets/_Project/Scenes/Menus/01_Title.unity";
        public const string BOOT_PATH = "Assets/_Project/Scenes/Boot/00_Boot.unity";
        private const string MODEL_SOURCE = "Tools/Export/Menu/TitleScene.fbx";
        private const string MODEL_PATH = "Assets/_Project/Art/Menu/TitleScene.fbx";
        private const string CONTROLLER_PATH = "Assets/_Project/Art/Menu/TitleScene.controller";
        private const string MATERIALS = "Assets/_Project/Art/Menu/Materials";

        [MenuItem("Gas Safari/Setup/Crear escena de título y menús (M6-01)")]
        public static void Build()
        {
            ImportModel();
            BuildScene();
            RegisterInBuildSettings();
        }

        private static void ImportModel()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MODEL_PATH));
            Directory.CreateDirectory(MATERIALS);
            var from = new FileInfo(MODEL_SOURCE);
            var to = new FileInfo(MODEL_PATH);
            if (!to.Exists || to.Length != from.Length)
                File.Copy(MODEL_SOURCE, MODEL_PATH, true);
            AssetDatabase.Refresh();

            var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL_PATH);
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.importCameras = true;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.optimizeGameObjects = false;
            importer.generateSecondaryUV = false;
            importer.SaveAndReimport();
            BlenderAssetImporter.RemapToonMaterials(MODEL_PATH, MATERIALS + "/", new Color(0.45f, 0.48f, 0.65f));

            importer = (ModelImporter)AssetImporter.GetAtPath(MODEL_PATH);
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = "TitleLoop";
                clip.loopTime = true;
                clip.loopPose = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void BuildScene()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            var clip = AssetDatabase.LoadAllAssetsAtPath(MODEL_PATH).OfType<AnimationClip>().FirstOrDefault(c => c.name == "TitleLoop");
            AssetDatabase.DeleteAsset(CONTROLLER_PATH);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            if (clip != null)
            {
                var state = controller.layers[0].stateMachine.AddState("TitleLoop");
                state.motion = clip;
                controller.layers[0].stateMachine.defaultState = state;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var backdrop = (GameObject)PrefabUtility.InstantiatePrefab(model);
            backdrop.name = "TitleBackdrop";
            var animator = backdrop.GetComponent<Animator>();
            if (animator == null)
                animator = backdrop.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // La cámara viene del FBX (TitleCamera, 32 mm) con su movimiento; si no llegó, se crea una igual de útil.
            var camera = backdrop.GetComponentInChildren<Camera>(true);
            if (camera == null)
            {
                var cameraObject = new GameObject("TitleCamera");
                cameraObject.transform.position = new Vector3(0f, 3.2f, -9f);
                cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.gameObject.SetActive(true);
            camera.enabled = true;
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
            if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.8f);

            new GameObject("Menus").AddComponent<MenuApp>();
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("M6-01 escena de título guardada: " + SCENE_PATH);
        }

        // 00_Boot primero (crea los servicios), luego 01_Title y los niveles.
        public static void RegisterInBuildSettings()
        {
            var current = EditorBuildSettings.scenes.Where(s => s.path != BOOT_PATH && s.path != SCENE_PATH).ToList();
            current.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));
            current.Insert(0, new EditorBuildSettingsScene(BOOT_PATH, true));
            EditorBuildSettings.scenes = current.ToArray();
        }
    }
}
