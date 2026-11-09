using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GasSafari.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace GasSafari.Editor
{
    // M5-39: viñetas de historia entre mundos (docs/01). Para cada momento de Tools/Export/Story (Intro, W1_End ... W5_End,
    // W6_Secret): copia sus imágenes a Art/Story como sprites, arma la línea de tiempo (una pista de activación por imagen), el lienzo
    // con sus pies de texto (claves story.<momento>.<n> de strings.csv) y el StoryVignetteData; la StoryLibrary los reúne.
    public static class StoryBuilder
    {
        private const string SOURCE = "Tools/Export/Story";
        private const string ART = "Assets/_Project/Art/Story/";
        private const string DATA = "Assets/_Project/Data/Story/";
        private const string PREFABS = "Assets/_Project/Prefabs/UI/Story/";
        private const string CSV = "Tools/Export/Localization/strings.csv";
        public const string STRINGS_PATH = StringsImporter.TABLE_PATH;
        public const string LIBRARY_PATH = DATA + "StoryLibrary.asset";
        private const float SECONDS_PER_PANEL = 5f;

        private static readonly string[] Moments = { "Intro", "W1_End", "W2_End", "W3_End", "W4_End", "W5_End", "W6_Secret" };

        [MenuItem("Gas Safari/Setup/Crear viñetas de historia (M5-39)")]
        public static void Build()
        {
            BlenderAssetImporter.EnsureFolder(ART.TrimEnd('/'));
            BlenderAssetImporter.EnsureFolder(DATA.TrimEnd('/'));
            BlenderAssetImporter.EnsureFolder(PREFABS.TrimEnd('/'));

            var strings = BuildStrings();
            var vignettes = new List<StoryVignetteData>();
            foreach (var moment in Moments)
            {
                var sprites = ImportSprites(moment);
                if (sprites.Length == 0)
                {
                    Debug.LogWarning($"M5-39 {moment}: no hay imágenes en {SOURCE}/{moment}");
                    continue;
                }
                vignettes.Add(BuildVignette(moment, sprites, strings));
            }

            var library = AssetDatabase.LoadAssetAtPath<StoryLibrary>(LIBRARY_PATH);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<StoryLibrary>();
                AssetDatabase.CreateAsset(library, LIBRARY_PATH);
            }
            library.vignettes = vignettes.ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            int captions = vignettes.Sum(v => Enumerable.Range(0, v.panels.Length).Count(i => v.CaptionFor(i).Length > 0));
            int panels = vignettes.Sum(v => v.panels.Length);
            Debug.Log($"M5-39 viñetas listas: {vignettes.Count} momentos, {panels} imágenes, {captions} con pie de texto");
        }

        // ---------------------------------------------------------------- imágenes
        private static Sprite[] ImportSprites(string moment)
        {
            string sourceFolder = $"{SOURCE}/{moment}";
            if (!Directory.Exists(sourceFolder))
                return new Sprite[0];
            string folder = ART + moment;
            BlenderAssetImporter.EnsureFolder(folder);
            var paths = new List<string>();
            foreach (var file in Directory.GetFiles(sourceFolder, "*.png").OrderBy(f => f))
            {
                string path = $"{folder}/{Path.GetFileName(file)}";
                if (!File.Exists(path) || new FileInfo(path).Length != new FileInfo(file).Length)
                    File.Copy(file, path, true);
                paths.Add(path);
            }
            AssetDatabase.Refresh();
            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return paths.Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray();
        }

        // ---------------------------------------------------------------- textos
        // Pies de las viñetas: las filas story.* del CSV de localización (columna es). Una clave sin texto queda vacía.
        private static UIStrings BuildStrings()
        {
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(STRINGS_PATH);
            if (strings == null)
            {
                StringsImporter.Import();
                strings = AssetDatabase.LoadAssetAtPath<UIStrings>(STRINGS_PATH);
            }
            return strings;
        }

        // CSV con comillas ("" = comilla) y saltos de línea dentro de campos entre comillas.
        public static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }
                if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }

        // ---------------------------------------------------------------- viñeta
        private static StoryVignetteData BuildVignette(string moment, Sprite[] sprites, UIStrings strings)
        {
            string key = moment.ToLowerInvariant();
            var panelData = new StoryVignetteData.Panel[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                panelData[i] = new StoryVignetteData.Panel { image = sprites[i], captionKey = $"story.{key}.{i + 1}" };

            var timeline = BuildTimeline(moment, sprites.Length, out var tracks);
            var prefab = BuildPrefab(moment, sprites, timeline, tracks);

            string dataPath = $"{DATA}Story_{moment}.asset";
            var data = AssetDatabase.LoadAssetAtPath<StoryVignetteData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StoryVignetteData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }
            data.id = moment;
            data.panels = panelData;
            data.secondsPerPanel = SECONDS_PER_PANEL;
            data.strings = strings;
            data.prefab = prefab;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static TimelineAsset BuildTimeline(string moment, int panels, out ActivationTrack[] tracks)
        {
            string path = $"{DATA}Story_{moment}.playable";
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, path);
            }
            foreach (var track in timeline.GetOutputTracks().ToList())
                timeline.DeleteTrack(track);
            tracks = new ActivationTrack[panels];
            for (int i = 0; i < panels; i++)
            {
                tracks[i] = timeline.CreateTrack<ActivationTrack>(null, $"Imagen_{i + 1:00}");
                var clip = tracks[i].CreateDefaultClip();
                clip.start = i * SECONDS_PER_PANEL;
                clip.duration = SECONDS_PER_PANEL;
                clip.displayName = $"Imagen {i + 1}";
            }
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        // Lienzo a pantalla completa: fondo negro, una imagen por panel con su franja de pie de texto y la ayuda para saltar.
        private static GameObject BuildPrefab(string moment, Sprite[] sprites, TimelineAsset timeline, ActivationTrack[] tracks)
        {
            var root = new GameObject($"Story_{moment}", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var back = Child("Fondo", root.transform);
            Stretch(back);
            var backImage = back.gameObject.AddComponent<Image>();
            backImage.color = Color.black;
            backImage.raycastTarget = false;

            var groups = new CanvasGroup[sprites.Length];
            var captions = new TMP_Text[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                var panel = Child($"Imagen_{i + 1:00}", root.transform);
                Stretch(panel);
                groups[i] = panel.gameObject.AddComponent<CanvasGroup>();
                groups[i].alpha = 0f;
                groups[i].interactable = false;
                groups[i].blocksRaycasts = false;

                var picture = Child("Dibujo", panel);
                Stretch(picture);
                var image = picture.gameObject.AddComponent<Image>();
                image.sprite = sprites[i];
                image.preserveAspect = true;
                image.raycastTarget = false;

                var bar = Child("Pie", panel);
                bar.anchorMin = new Vector2(0f, 0f);
                bar.anchorMax = new Vector2(1f, 0f);
                bar.pivot = new Vector2(0.5f, 0f);
                bar.sizeDelta = new Vector2(0f, 150f);
                bar.anchoredPosition = Vector2.zero;
                var barImage = bar.gameObject.AddComponent<Image>();
                barImage.color = new Color(0.04f, 0.02f, 0.08f, 0.78f);
                barImage.raycastTarget = false;

                var text = Child("Texto", bar);
                Stretch(text);
                text.offsetMin = new Vector2(80f, 10f);
                text.offsetMax = new Vector2(-80f, -10f);
                var label = text.gameObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = 46f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
                captions[i] = label;
                panel.gameObject.SetActive(false);
            }

            var hint = Child("AyudaSaltar", root.transform);
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(1f, 1f);
            hint.sizeDelta = new Vector2(640f, 56f);
            hint.anchoredPosition = new Vector2(-30f, -24f);
            var hintLabel = hint.gameObject.AddComponent<TextMeshProUGUI>();
            hintLabel.text = "Pulsa cualquier botón para saltar"; // por defecto; en juego sale de story.skip
            hintLabel.fontSize = 26f;
            hintLabel.alignment = TextAlignmentOptions.Right;
            hintLabel.color = new Color(1f, 1f, 1f, 0.7f);
            hintLabel.raycastTarget = false;

            var director = root.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            for (int i = 0; i < tracks.Length; i++)
                director.SetGenericBinding(tracks[i], root.transform.Find($"Imagen_{i + 1:00}").gameObject);

            var player = root.AddComponent<StoryPlayer>();
            var so = new SerializedObject(player);
            so.FindProperty("director").objectReferenceValue = director;
            var panelsProperty = so.FindProperty("panels");
            panelsProperty.arraySize = groups.Length;
            var captionsProperty = so.FindProperty("captions");
            captionsProperty.arraySize = captions.Length;
            for (int i = 0; i < groups.Length; i++)
            {
                panelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = groups[i];
                captionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = captions[i];
            }
            so.FindProperty("skipHint").objectReferenceValue = hint.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PREFABS}Story_{moment}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
