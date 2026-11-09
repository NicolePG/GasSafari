using System.Collections.Generic;
using System.IO;
using GasSafari.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // M6: copia el arte de Tools/Export/UI a Assets/_Project/Art/UI como Sprites (9-slice del kit según el README) y lo reúne en
    // Resources/UiSkin.asset. Se puede correr las veces que haga falta: solo copia lo que cambió.
    public static class MenuArtImporter
    {
        public const string SKIN_PATH = "Assets/_Project/Resources/UiSkin.asset";
        private const string SOURCE = "Tools/Export/UI/";
        private const string TARGET = "Assets/_Project/Art/UI/";
        private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static readonly string[] Folders = { "Kit", "Icons", "Portraits", "CharacterSelect", "Logo", "WorldMap", "Loading" };

        // Bordes 9-slice del README (px, orden L / T / R / B).
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "Panel_Light", new Vector4(58, 58, 58, 58) },
            { "Panel_Dark", new Vector4(58, 58, 58, 58) },
            { "Panel_Inset", new Vector4(50, 50, 50, 50) },
            { "Button_", new Vector4(52, 52, 52, 52) },
            { "Meter_Back", new Vector4(40, 8, 40, 8) },
            { "Meter_Fill", new Vector4(40, 8, 40, 8) },
            { "Meter_Glow", new Vector4(90, 60, 90, 60) },
            { "Event_Banner_", new Vector4(72, 40, 72, 40) },
        };

        [MenuItem("Gas Safari/Setup/Importar arte de menús (M6)")]
        public static void Import()
        {
            var imported = new List<string>();
            foreach (string folder in Folders)
                CopyFolder(SOURCE + folder, TARGET + folder, imported);
            AssetDatabase.Refresh();

            foreach (string path in imported)
                Configure(path);
            AssetDatabase.SaveAssets();

            var sprites = new List<Sprite>();
            foreach (string path in imported)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SKIN_PATH);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UiSkin>();
                AssetDatabase.CreateAsset(skin, SKIN_PATH);
            }
            skin.Set(sprites.ToArray(), AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH),
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/CREDITS.md"));
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log($"MenuArtImporter: {sprites.Count} sprites en {SKIN_PATH}");
        }

        private static void CopyFolder(string from, string to, List<string> imported)
        {
            if (!Directory.Exists(from))
            {
                Debug.LogWarning("MenuArtImporter: no encuentro " + from);
                return;
            }
            foreach (string file in Directory.GetFiles(from, "*.png", SearchOption.AllDirectories))
            {
                string relative = file.Replace('\\', '/').Substring(from.Length).TrimStart('/');
                string target = to + "/" + relative;
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                var source = new FileInfo(file);
                var copy = new FileInfo(target);
                if (!copy.Exists || copy.Length != source.Length || copy.LastWriteTimeUtc < source.LastWriteTimeUtc)
                    File.Copy(file, target, true);
                imported.Add(target);
            }
        }

        private static void Configure(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            string name = Path.GetFileNameWithoutExtension(path);
            var border = Vector4.zero;
            foreach (var pair in Borders)
            {
                if (name.StartsWith(pair.Key))
                    border = pair.Value;
            }

            bool background = path.Contains("/Loading/") || path.Contains("/WorldMap/");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            // Los fondos opacos se comprimen; los iconos y el kit no (README: sin compresión con pérdida).
            importer.textureCompression = background ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            // Vector4 de Unity: x = izquierda, y = abajo, z = derecha, w = arriba.
            importer.spriteBorder = new Vector4(border.x, border.w, border.z, border.y);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
