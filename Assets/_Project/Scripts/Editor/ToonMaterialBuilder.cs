using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Materiales base de SG_Toon y SG_ToonWater (docs/09 §2). Se puede ejecutar varias veces.
    public static class ToonMaterialBuilder
    {
        public const string TOON_PATH = "Assets/_Project/Art/Materials/M_Toon_Default.mat";
        public const string WATER_PATH = "Assets/_Project/Art/Materials/M_ToonWater.mat";
        private const string TOON_SHADER = "GasSafari/SG_Toon";
        private const string WATER_SHADER = "GasSafari/SG_ToonWater";

        [MenuItem("Gas Safari/Setup/Crear materiales toon")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Materials");
            Create(TOON_PATH, TOON_SHADER);
            Create(WATER_PATH, WATER_SHADER);
            AssetDatabase.SaveAssets();
        }

        private static void Create(string path, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }
    }
}
