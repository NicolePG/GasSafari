using UnityEditor;

namespace GasSafari.Editor
{
    // Importa los recursos esenciales de TextMeshPro sin el diálogo modal del menú de Window.
    public static class TmpEssentialsImporter
    {
        private const string PACKAGE = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
        private const string SETTINGS = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Gas Safari/Setup/Importar recursos de TextMeshPro")]
        public static void Import()
        {
            if (AssetDatabase.LoadMainAssetAtPath(SETTINGS) != null)
                return;
            AssetDatabase.ImportPackage(PACKAGE, false);
        }
    }
}
