using UnityEditor;
using UnityEngine;
#if GS_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace GasSafari.Editor
{
    // Aplica la configuración de docs/10 §1. Se puede ejecutar varias veces.
    public static class ProjectSetup
    {
        private const int INPUT_HANDLER_NEW = 1;
        private const float FIXED_TIMESTEP = 0.02f;
        private const int SOLVER_ITERATIONS = 8;
        private const string SETTINGS_FOLDER = "Assets/_Project/Settings";
        private const string URP_ASSET_PATH = SETTINGS_FOLDER + "/GasSafari_URP.asset";
        private const string URP_RENDERER_PATH = SETTINGS_FOLDER + "/GasSafari_URP_Renderer.asset";
        private const string URP_PACKAGE_PATH = "Packages/com.unity.render-pipelines.universal";

        [MenuItem("Gas Safari/Setup/Aplicar configuración del proyecto")]
        public static void Apply()
        {
            // Se fija antes de instalar el Input System para que el paquete no abra su diálogo modal.
            SetSerialized("PlayerSettings", "activeInputHandler", INPUT_HANDLER_NEW);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            SetSerialized("TimeManager", "Fixed Timestep", FIXED_TIMESTEP);
            Physics.defaultSolverIterations = SOLVER_ITERATIONS;
            // Las pruebas PlayMode por MCP lo dejan activo; el proyecto recarga el dominio al entrar a Play.
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EnsureLayer(Core.GameLayers.TEAM_CAMERA_A, 16);
            EnsureLayer(Core.GameLayers.TEAM_CAMERA_B, 17);
#if GS_URP
            SetupUrp();
#endif
            AssetDatabase.SaveAssets();
        }

        // Capas sin objetos que separan las dos cámaras virtuales de la pantalla dividida (Carrera por Equipos, M7-11):
        // con Cinemachine 2.x cada CinemachineBrain solo usa las cámaras virtuales cuya capa ve.
        private static void EnsureLayer(string name, int preferred)
        {
            if (LayerMask.NameToLayer(name) >= 0)
                return;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            int slot = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferred).stringValue) ? preferred : -1;
            for (int i = 8; slot < 0 && i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    slot = i;
            }
            if (slot < 0)
            {
                Debug.LogError("No queda capa libre para " + name);
                return;
            }
            layers.GetArrayElementAtIndex(slot).stringValue = name;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerialized(string settingsName, string property, float value)
        {
            var settings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton(settingsName));
            var prop = settings.FindProperty(property);
            if (prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = (int)value;
            else
                prop.floatValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

#if GS_URP
        private static void SetupUrp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URP_ASSET_PATH);
            if (asset == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    URP_PACKAGE_PATH + "/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(renderer, URP_RENDERER_PATH);
                ResourceReloader.ReloadAllNullIn(renderer, URP_PACKAGE_PATH);

                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, URP_ASSET_PATH);
            }

            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;
            EditorUtility.SetDirty(asset);
            GraphicsSettings.renderPipelineAsset = asset;
        }
#endif
    }
}
