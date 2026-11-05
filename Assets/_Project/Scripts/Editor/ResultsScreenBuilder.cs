using GasSafari.Level;
using GasSafari.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.Editor
{
    // Pantalla de resultados de la plantilla de nivel (docs/07 §2), con arte provisional.
    public static class ResultsScreenBuilder
    {
        private const string SPRITE_BOX = "UI/Skin/UISprite.psd";
        private const string SPRITE_CIRCLE = "UI/Skin/Knob.psd";
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color Panel = new Color(0.05f, 0.08f, 0.15f, 0.96f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.75f);

        public static void Populate(ResultsController controller, LevelManager level)
        {
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(HudBuilder.STRINGS_PATH);
            var root = new GameObject("ResultsScreen");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var panel = HudBuilder.Rect("Panel", root.transform);
            HudBuilder.Stretch(panel);
            var dim = HudBuilder.Img("Dim", panel, SPRITE_BOX, Dim);
            HudBuilder.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var box = HudBuilder.Img("Box", panel, SPRITE_BOX, Panel);
            HudBuilder.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 900f), Vector2.zero);

            var time = HudBuilder.Text("Time", box.transform, 110, TextAlignmentOptions.Center);
            HudBuilder.Place(time.rectTransform, new Vector2(0f, 1f), new Vector2(640f, 130f), new Vector2(40f, -30f));

            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = HudBuilder.Img("Star" + i, box.transform, SPRITE_CIRCLE, Color.gray);
                HudBuilder.Place(stars[i].rectTransform, new Vector2(0f, 1f), new Vector2(120f, 120f), new Vector2(110f + i * 150f, -190f));
            }

            var mangos = HudBuilder.Text("Mangos", box.transform, 48, TextAlignmentOptions.Center);
            mangos.color = new Color(1f, 0.82f, 0.1f);
            HudBuilder.Place(mangos.rectTransform, new Vector2(0f, 1f), new Vector2(640f, 60f), new Vector2(40f, -340f));
            var beans = HudBuilder.Text("Beans", box.transform, 48, TextAlignmentOptions.Center);
            HudBuilder.Place(beans.rectTransform, new Vector2(0f, 1f), new Vector2(640f, 60f), new Vector2(40f, -410f));

            var awards = new TMP_Text[3];
            for (int i = 0; i < awards.Length; i++)
            {
                awards[i] = HudBuilder.Text("Award" + i, box.transform, 44, TextAlignmentOptions.Left);
                HudBuilder.Place(awards[i].rectTransform, new Vector2(1f, 1f), new Vector2(740f, 70f), new Vector2(-40f, -70f - i * 90f));
            }

            var replayTitle = HudBuilder.Text("ReplayTitle", box.transform, 48, TextAlignmentOptions.Center);
            HudBuilder.Place(replayTitle.rectTransform, new Vector2(1f, 1f), new Vector2(740f, 60f), new Vector2(-40f, -360f));
            var replayObject = HudBuilder.Rect("Replay", box.transform);
            HudBuilder.Place(replayObject, new Vector2(1f, 1f), new Vector2(640f, 360f), new Vector2(-90f, -430f));
            var rawImage = replayObject.gameObject.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            var replay = replayObject.gameObject.AddComponent<ReplayPlayer>();
            HudBuilder.SetRef(replay, "view", rawImage);

            var next = MakeButton("Next", box.transform, strings.Get("results.next"), -440f);
            var retry = MakeButton("Retry", box.transform, strings.Get("results.retry"), 0f);
            var map = MakeButton("Map", box.transform, strings.Get("results.map"), 440f);

            var screen = root.AddComponent<ResultsScreen>();
            HudBuilder.SetRef(screen, "controller", controller);
            HudBuilder.SetRef(screen, "level", level);
            HudBuilder.SetRef(screen, "strings", strings);
            HudBuilder.SetRef(screen, "panel", panel.gameObject);
            HudBuilder.SetRef(screen, "timeText", time);
            HudBuilder.SetRef(screen, "mangoText", mangos);
            HudBuilder.SetRef(screen, "beansText", beans);
            HudBuilder.SetRef(screen, "replayTitle", replayTitle);
            HudBuilder.SetRef(screen, "replay", replay);
            HudBuilder.SetRef(screen, "next", next);
            HudBuilder.SetRef(screen, "retry", retry);
            HudBuilder.SetRef(screen, "map", map);
            SetArray(screen, "stars", stars);
            SetArray(screen, "awardTexts", awards);
        }

        private static void SetArray(Object target, string field, Object[] elements)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = elements.Length;
            for (int i = 0; i < elements.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button MakeButton(string name, Transform parent, string label, float x)
        {
            var image = HudBuilder.Img(name, parent, SPRITE_BOX, ButtonColor);
            image.raycastTarget = true;
            HudBuilder.Place(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(380f, 80f), new Vector2(x, 40f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.selectedColor = new Color(1f, 0.85f, 0.3f);
            colors.highlightedColor = new Color(0.4f, 0.65f, 0.95f);
            button.colors = colors;
            var text = HudBuilder.Text("Label", image.transform, 40, TextAlignmentOptions.Center);
            text.SetText(label);
            HudBuilder.Stretch(text.rectTransform);
            return button;
        }
    }
}
