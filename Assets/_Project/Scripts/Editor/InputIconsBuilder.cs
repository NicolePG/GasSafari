using System.Collections.Generic;
using System.IO;
using GasSafari.UI;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Importa los iconos de Art/UI/InputIcons/{Xbox,PlayStation,Keyboard} como sprites y arma Resources/InputIconSet.asset.
    public static class InputIconsBuilder
    {
        private const string ICONS = "Assets/_Project/Art/UI/InputIcons";
        private const string ASSET = "Assets/_Project/Resources/InputIconSet.asset";

        [MenuItem("Gas Safari/Setup/Importar iconos de botones (Kenney)")]
        public static void Build()
        {
            var entries = new List<InputIconSet.Entry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ICONS }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 128;
                importer.SaveAndReimport();

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    entries.Add(new InputIconSet.Entry { id = Path.GetFileNameWithoutExtension(path), sprite = sprite });
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ASSET));
            var set = AssetDatabase.LoadAssetAtPath<InputIconSet>(ASSET);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<InputIconSet>();
                AssetDatabase.CreateAsset(set, ASSET);
            }
            set.SetEntries(entries);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log($"INPUTICONS iconos: {entries.Count}");
        }
    }
}
