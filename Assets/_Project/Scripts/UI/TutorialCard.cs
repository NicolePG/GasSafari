using System.Collections.Generic;
using System.Text;
using GasSafari.Core;
using GasSafari.Level;
using TMPro;
using UnityEngine;

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
            title.text = step.title;
            body.text = step.text;
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
            showing = true;
            animTime = 0f;
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
