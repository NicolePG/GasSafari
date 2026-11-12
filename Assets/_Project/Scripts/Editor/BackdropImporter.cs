using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Importa los fondos de Tools/Export/Backdrops/W1..W6 (capas media, lejana y suelo de relleno, README de esa carpeta)
    // a Art/Environment/Backdrops/W<N>/ con materiales SG_Toon por mundo (los E_* llevan emisión para el Bloom).
    public static class BackdropImporter
    {
        public const string TARGET = "Environment/Backdrops/";

        private static readonly Color[] Shadow =
        {
            new Color(0.30f, 0.33f, 0.58f), new Color(0.45f, 0.48f, 0.65f), new Color(0.55f, 0.36f, 0.45f),
            new Color(0.28f, 0.24f, 0.46f), new Color(0.55f, 0.40f, 0.38f), new Color(0.34f, 0.14f, 0.42f),
        };

        [MenuItem("Gas Safari/Visual/Importar fondos (Backdrops)")]
        public static void ImportAll()
        {
            for (int w = 1; w <= 6; w++)
            {
                var paths = BlenderAssetImporter.ImportStaticModels("Backdrops/W" + w, TARGET + "W" + w, Shadow[w - 1]);
                string materials = "Assets/_Project/Art/" + TARGET + "W" + w + "/Materials";
                int count = 0;
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { materials }))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    m.SetFloat("_RimStrength", 0f);
                    EditorUtility.SetDirty(m);
                    count++;
                }
                Debug.Log($"BACKDROP W{w}: {paths.Length} piezas, {count} materiales");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
