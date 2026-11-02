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
#if GS_URP
            SetupUrp();
#endif
            AssetDatabase.SaveAssets();
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
