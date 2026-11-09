using System.Collections.Generic;
using System.Text;
using GasSafari.Core;
using GasSafari.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Cartel grande de la lección (docs/07 §8): título, texto y el botón de cada jugador según su dispositivo.
    // Entra con escala 0 → 1.2 → 1 en 0.25 s y sale con fade en 0.4 s (docs/07 §3).
    public class TutorialCard : MonoBehaviour
    {
        private const float IN_TIME = 0.25f;
        private const float OUT_TIME = 0.4f;

        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text buttons;

        private const float ICON_SIZE = 56f;

        private RectTransform iconRow;
        private readonly StringBuilder builder = new StringBuilder();
        private float animTime;
        private bool showing;

        public bool IsShowing => showing;
        public string Title => title != null ? title.text : string.Empty;
        public string Buttons => buttons != null ? buttons.text : string.Empty;

        public void Configure(RectTransform cardPanel, CanvasGroup cardGroup, TMP_Text cardTitle, TMP_Text cardBody, TMP_Text cardButtons)
        {
            panel = cardPanel;
            group = cardGroup;
            title = cardTitle;
            body = cardBody;
            buttons = cardButtons;
            group.alpha = 0f;
        }

        public void Show(TutorialStep step, IReadOnlyList<PlayerSlot> players)
        {
            var table = LocalizedText.Table;
            bool keyed = !string.IsNullOrEmpty(step.textKey) && table != null;
            title.text = keyed ? table.Get(step.textKey + ".title") : step.title;
            body.text = keyed ? table.Get(step.textKey + ".text") : step.text;
            builder.Clear();
            int shown = 0;
            for (int i = 0; players != null && i < players.Count; i++)
            {
                var slot = players[i];
                if (slot.IsBot)
                    continue;
                if (shown++ > 0)
                    builder.Append("     ");
                builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(slot.Color)).Append(">J").Append(slot.Index + 1).Append("</color> ");
                builder.Append(InputGlyphs.Label(step.action, InputGlyphs.KindOf(slot.Input)));
            }
            if (shown == 0)
                builder.Append(InputGlyphs.Label(step.action, InputDeviceKind.Xbox));
            buttons.text = builder.ToString();
            ShowIcons(step, players);
            showing = true;
            animTime = 0f;
        }

        // Los botones se muestran con los iconos Kenney de cada dispositivo (docs/07 §6); el texto queda de respaldo
        // (y de lectura para pruebas y lectores de pantalla) y se apaga mientras haya iconos.
        private void ShowIcons(TutorialStep step, IReadOnlyList<PlayerSlot> players)
        {
            var set = InputIconSet.Instance;
            if (set == null || set.Count == 0)
            {
                buttons.enabled = true;
                if (iconRow != null)
                    iconRow.gameObject.SetActive(false);
                return;
            }

            EnsureRow();
            for (int i = iconRow.childCount - 1; i >= 0; i--)
                Destroy(iconRow.GetChild(i).gameObject);

            int shown = 0;
            for (int i = 0; players != null && i < players.Count; i++)
            {
                var slot = players[i];
                if (slot.IsBot)
                    continue;
                if (shown++ > 0)
                    AddLabel("   ", Color.white);
                AddLabel("J" + (slot.Index + 1), slot.Color);
                AddIcons(set, step.action, InputGlyphs.KindOf(slot.Input));
            }
            if (shown == 0)
                AddIcons(set, step.action, InputDeviceKind.Xbox);

            buttons.enabled = false;
            iconRow.gameObject.SetActive(true);
        }

        private void AddIcons(InputIconSet set, TutorialAction action, InputDeviceKind kind)
        {
            foreach (var token in InputGlyphs.Icons(action, kind))
            {
                var sprite = set.Get(token);
                if (sprite == null)
                    AddLabel(token == "+" || token == "/" ? " " + token + " " : token, Color.white);
                else
                    AddIcon(sprite);
            }
        }

        private void EnsureRow()
        {
            if (iconRow != null)
                return;
            var go = new GameObject("Icons", typeof(RectTransform));
            iconRow = (RectTransform)go.transform;
            iconRow.SetParent(buttons.transform.parent, false);
            var source = buttons.rectTransform;
            iconRow.anchorMin = source.anchorMin;
            iconRow.anchorMax = source.anchorMax;
            iconRow.pivot = source.pivot;
            iconRow.anchoredPosition = source.anchoredPosition;
            iconRow.sizeDelta = new Vector2(source.sizeDelta.x, ICON_SIZE + 6f);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void AddIcon(Sprite sprite)
        {
            var go = new GameObject(sprite.name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(iconRow, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            float aspect = sprite.rect.width / Mathf.Max(sprite.rect.height, 1f);
            ((RectTransform)go.transform).sizeDelta = new Vector2(ICON_SIZE * aspect, ICON_SIZE);
        }

        private void AddLabel(string text, Color color)
        {
            var label = Instantiate(buttons, iconRow);
            label.enabled = true;
            label.text = text;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.ForceMeshUpdate();
            label.rectTransform.sizeDelta = new Vector2(label.preferredWidth + 4f, ICON_SIZE);
        }

        public void Hide()
        {
            if (!showing)
                return;
            showing = false;
            animTime = 0f;
        }

        private void Update()
        {
            if (group == null)
                return;
            animTime += Time.unscaledDeltaTime;
            if (showing)
            {
                float t = Mathf.Clamp01(animTime / IN_TIME);
                float scale = t < 0.7f ? Mathf.Lerp(0f, 1.2f, t / 0.7f) : Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);
                panel.localScale = Vector3.one * scale;
                group.alpha = 1f;
            }
            else
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime / OUT_TIME);
            }
        }
    }
}
