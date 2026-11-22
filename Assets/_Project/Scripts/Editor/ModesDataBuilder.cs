using GasSafari.Modes;
using GasSafari.Progression;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Datos de los modos de M7 (docs/06 §1): Resources/ModesConfig.asset, material del fantasma y tarjetas activadas.
    public static class ModesDataBuilder
    {
        private const string CONFIG_PATH = "Assets/_Project/Resources/ModesConfig.asset";
        private const string GHOST_MATERIAL_PATH = "Assets/_Project/Art/Materials/Modes/M_GhostRaft.mat";
        private const string MODES = "Assets/_Project/Data/Modes/";

        // Modos de M7 que ya tienen su lógica (los demás siguen en "Próximamente").
        public static readonly string[] Implemented = { ProgressionRules.MODE_TIME_TRIAL, ProgressionRules.MODE_MIRROR, ProgressionRules.MODE_MODIFIERS };

        [MenuItem("Gas Safari/Setup/Crear modos de M7")]
        public static void Build()
        {
            var config = HudBuilder.LoadOrCreate<ModesConfig>(CONFIG_PATH);
            config.ghostMaterial = GhostMaterial();
            config.modifiers = BuildModifiers();
            EditorUtility.SetDirty(config);

            foreach (var id in Implemented)
            {
                var mode = AssetDatabase.LoadAssetAtPath<GameModeData>(MODES + "Mode_" + id + ".asset");
                if (mode == null)
                    continue;
                mode.implemented = true;
                EditorUtility.SetDirty(mode);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("M7: ModesConfig listo");
        }

        // docs/06 §1: efecto y estrellas de cada uno. El balanceo de Cámara mareada (6°) es provisional.
        private static ModifierData[] BuildModifiers()
        {
            var defs = new (string id, ModifierKind kind, float value, int stars)[]
            {
                ("soapy", ModifierKind.SoapyRaft, 0.5f, 30),
                ("infinite_gas", ModifierKind.InfiniteGas, 1f, 30),
                ("moon", ModifierKind.LunarGravity, 0.4f, 40),
                ("reverse", ModifierKind.ReversedFarts, -1f, 40),
                ("bigheads", ModifierKind.BigHeads, 2f, 50),
                ("turbo", ModifierKind.TurboCurrent, 1.5f, 50),
                ("dizzy", ModifierKind.DizzyCamera, 6f, 60),
                ("single", ModifierKind.SingleChamber, 1f, 70),
            };
            var result = new ModifierData[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var data = HudBuilder.LoadOrCreate<ModifierData>("Assets/_Project/Data/Modifiers/Modifier_" + defs[i].id + ".asset");
                data.id = defs[i].id;
                data.kind = defs[i].kind;
                data.value = defs[i].value;
                data.starsRequired = defs[i].stars;
                data.order = i + 1;
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            return result;
        }

        // URP Unlit transparente, sin sombras ni escritura de profundidad.
        private static Material GhostMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GHOST_MATERIAL_PATH);
            if (material == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GHOST_MATERIAL_PATH));
                AssetDatabase.Refresh();
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, GHOST_MATERIAL_PATH);
            }
            var color = new Color(0.55f, 0.9f, 1f, 0.35f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
            material.SetColor("_BaseColor", color);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
