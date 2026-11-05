using GasSafari.Level;
using GasSafari.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.Editor
{
    // Menú de pausa de la plantilla de nivel (docs/07 §4), con arte provisional.
    public static class PauseMenuBuilder
    {
        private const string SPRITE_BOX = "UI/Skin/UISprite.psd";
        private const float BUTTON_WIDTH = 560f;
        private const float BUTTON_HEIGHT = 70f;
        private const float BUTTON_GAP = 14f;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Panel = new Color(0.05f, 0.08f, 0.15f, 0.95f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.75f);

        public static void Populate(PauseController controller)
        {
            var strings = AssetDatabase.LoadAssetAtPath<UIStrings>(HudBuilder.STRINGS_PATH);
            var root = new GameObject("PauseMenu");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
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
            float height = 150f + 6 * (BUTTON_HEIGHT + BUTTON_GAP);
            HudBuilder.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(BUTTON_WIDTH + 80f, height), Vector2.zero);
            var title = HudBuilder.Text("Title", box.transform, 64, TextAlignmentOptions.Center);
            title.SetText(strings.Get("pause.title"));
            HudBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(BUTTON_WIDTH, 90f), new Vector2(0f, -24f));

            string[] keys = { "pause.resume", "pause.restart_checkpoint", "pause.restart_level", "pause.options", "pause.controls", "pause.quit" };
            var buttons = new Button[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                buttons[i] = MakeButton("Button_" + keys[i], box.transform, strings.Get(keys[i]));
                HudBuilder.Place((RectTransform)buttons[i].transform, new Vector2(0.5f, 1f), new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT),
                    new Vector2(0f, -120f - i * (BUTTON_HEIGHT + BUTTON_GAP)));
            }

            var votePanel = HudBuilder.Img("VotePanel", panel, SPRITE_BOX, Panel);
            HudBuilder.Place(votePanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(900f, 200f), new Vector2(0f, 60f));
            var voteText = HudBuilder.Text("VoteText", votePanel.transform, 36, TextAlignmentOptions.Center);
            HudBuilder.Place(voteText.rectTransform, new Vector2(0.5f, 1f), new Vector2(860f, 70f), new Vector2(0f, -16f));
            var yes = MakeButton("VoteYes", votePanel.transform, strings.Get("pause.yes"));
            HudBuilder.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(240f, BUTTON_HEIGHT), new Vector2(-140f, 24f));
            var no = MakeButton("VoteNo", votePanel.transform, strings.Get("pause.no"));
            HudBuilder.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(240f, BUTTON_HEIGHT), new Vector2(140f, 24f));

            var menu = root.AddComponent<PauseMenu>();
            HudBuilder.SetRef(menu, "controller", controller);
            HudBuilder.SetRef(menu, "strings", strings);
            HudBuilder.SetRef(menu, "panel", panel.gameObject);
            HudBuilder.SetRef(menu, "resume", buttons[0]);
            HudBuilder.SetRef(menu, "restartCheckpoint", buttons[1]);
            HudBuilder.SetRef(menu, "restartLevel", buttons[2]);
            HudBuilder.SetRef(menu, "options", buttons[3]);
            HudBuilder.SetRef(menu, "controls", buttons[4]);
            HudBuilder.SetRef(menu, "quit", buttons[5]);
            HudBuilder.SetRef(menu, "votePanel", votePanel.gameObject);
            HudBuilder.SetRef(menu, "voteText", voteText);
            HudBuilder.SetRef(menu, "voteYes", yes);
            HudBuilder.SetRef(menu, "voteNo", no);
        }

        private static Button MakeButton(string name, Transform parent, string label)
        {
            var image = HudBuilder.Img(name, parent, SPRITE_BOX, ButtonColor);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.selectedColor = new Color(1f, 0.85f, 0.3f);
            colors.highlightedColor = new Color(0.4f, 0.65f, 0.95f);
            button.colors = colors;
            var text = HudBuilder.Text("Label", image.transform, 36, TextAlignmentOptions.Center);
            text.SetText(label);
            HudBuilder.Stretch(text.rectTransform);
            return button;
        }
    }
}
