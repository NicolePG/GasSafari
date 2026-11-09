using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Un elemento enfocable que desplaza una lista con arriba/abajo (mando o teclado) y con la rueda del ratón.
    public class ScrollPad : Selectable, IMoveHandler
    {
        public ScrollRect Target { get; set; }
        public float Step { get; set; } = 0.12f;

        public override void OnMove(AxisEventData eventData)
        {
            if (Target != null && (eventData.moveDir == MoveDirection.Up || eventData.moveDir == MoveDirection.Down))
            {
                float delta = eventData.moveDir == MoveDirection.Up ? Step : -Step;
                float before = Target.verticalNormalizedPosition;
                float after = Mathf.Clamp01(before + delta);
                // En el borde el movimiento sigue hacia el botón vecino.
                if (!Mathf.Approximately(before, after))
                {
                    Target.verticalNormalizedPosition = after;
                    eventData.Use();
                    return;
                }
            }
            base.OnMove(eventData);
        }
    }

    // Créditos (docs/07 §2): se arman con el texto de CREDITS.md (modelos, sonidos propios y packs externos con su licencia).
    public class CreditsScreen : MenuScreen
    {
        private ScrollPad pad;
        private Button back;
        private TextMeshProUGUI body;
        private ScrollRect scroll;

        public override Selectable FirstSelected => pad;
        public string BodyText => body != null ? body.text : string.Empty;
        public ScrollRect Scroll => scroll;

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "credits.title", 74f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 110f), new Vector2(0f, -30f));

            var panel = UiFactory.Panel(Root, "Panel", "Panel_Light");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1640f, 770f), new Vector2(0f, -20f));

            scroll = UiFactory.Scroll(panel.transform, "Scroll", out var content);
            UiFactory.Stretch((RectTransform)scroll.transform, 52f);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textObject = UiFactory.Rect("Body", content);
            UiFactory.Stretch(textObject);
            body = textObject.gameObject.AddComponent<TextMeshProUGUI>();
            if (UiSkin.Instance != null && UiSkin.Instance.Font != null)
                body.font = UiSkin.Instance.Font;
            body.fontSize = 30f;
            body.color = UiFactory.Ink;
            body.richText = true;
            body.enableWordWrapping = true;
            body.raycastTarget = false;
            body.alignment = TextAlignmentOptions.TopLeft;
            // El texto crece con su contenido: la lista de abajo se estira con él.
            var textFitter = textObject.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            textObject.anchorMin = new Vector2(0f, 1f);
            textObject.anchorMax = new Vector2(1f, 1f);
            textObject.pivot = new Vector2(0.5f, 1f);

            pad = scroll.gameObject.AddComponent<ScrollPad>();
            pad.Target = scroll;
            pad.targetGraphic = null;
            pad.transition = Selectable.Transition.None;

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(300f, 92f), () => App.Back(), 40f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(300f, 92f), new Vector2(40f, 24f));
            var padNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = back };
            pad.navigation = padNav;
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = pad };
        }

        public override void OnShow()
        {
            Rebuild();
            Core.Localization.Changed -= Rebuild;
            Core.Localization.Changed += Rebuild;
            LocalizedText.TextScaleChanged -= Rebuild;
            LocalizedText.TextScaleChanged += Rebuild;
            scroll.verticalNormalizedPosition = 1f;
        }

        public override void OnHide()
        {
            LocalizedText.TextScaleChanged -= Rebuild;
            Core.Localization.Changed -= Rebuild;
        }

        protected override void Teardown()
        {
            LocalizedText.TextScaleChanged -= Rebuild;
            Core.Localization.Changed -= Rebuild;
        }

        private void Rebuild()
        {
            body.fontSize = 30f * LocalizedText.TextScale;
            body.text = Compose(LoadSource());
        }

        private static string LoadSource()
        {
            var skin = UiSkin.Instance;
            return skin != null && skin.Credits != null ? skin.Credits.text : string.Empty;
        }

        // Convierte CREDITS.md en texto de pantalla: las secciones propias (herramientas) y los packs externos (assets y licencias).
        public static string Compose(string markdown)
        {
            var tools = new StringBuilder();
            var assets = new StringBuilder();
            StringBuilder current = tools;
            string heading = null;
            var lines = markdown.Replace("\r", string.Empty).Split('\n');
            bool headerRowSeen = false;
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.StartsWith("## "))
                {
                    heading = Clean(line.Substring(3));
                    current = heading.IndexOf("externos", System.StringComparison.OrdinalIgnoreCase) >= 0 ? assets : tools;
                    current.Append("\n<b>").Append(heading).Append("</b>\n");
                    headerRowSeen = false;
                    continue;
                }
                if (!line.StartsWith("|"))
                    continue;
                if (line.Contains("---"))
                    continue;
                if (!headerRowSeen)
                {
                    headerRowSeen = true;
                    continue;
                }
                var cells = new List<string>();
                foreach (var cell in line.Trim('|').Split('|'))
                    cells.Add(Clean(cell));
                current.Append("• ").Append(string.Join(" · ", cells)).Append('\n');
            }

            var table = Core.Localization.Language == Core.Localization.ENGLISH ? "en" : "es";
            var strings = LocalizedText.Table;
            var builder = new StringBuilder();
            builder.Append("<size=120%><b>").Append(strings != null ? strings.Get("credits.tools", table) : "credits.tools").Append("</b></size>\n").Append(tools);
            builder.Append("\n<size=120%><b>").Append(strings != null ? strings.Get("credits.assets", table) : "credits.assets").Append("</b></size>\n").Append(assets);
            return builder.ToString();
        }

        private static string Clean(string text) => text.Replace("`", string.Empty).Replace("**", string.Empty).Trim();
    }
}
