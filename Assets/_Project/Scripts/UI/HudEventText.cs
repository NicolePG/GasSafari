using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GasSafari.UI
{
    // Textos de evento en el centro (docs/07 §3): escala 0 → 1.2 → 1 en 0.25 s, se sostiene y sale con fade de 0.4 s.
    // Todo dura menos de 1 s para no tapar la balsa. Los textos que llegan a la vez hacen cola.
    public class HudEventText : MonoBehaviour
    {
        private const float POP_TIME = 0.25f;
        private const float HOLD_TIME = 0.3f;
        private const float FADE_TIME = 0.4f;
        private const float OVERSHOOT = 1.2f;
        private const int MAX_QUEUE = 4;

        [SerializeField] private TMP_Text label;

        private readonly Queue<string> queue = new Queue<string>(MAX_QUEUE);
        private float elapsed;
        private bool showing;

        public bool IsShowing => showing;
        public string Current => label.text;

        private void Awake()
        {
            label.gameObject.SetActive(false);
        }

        // interrupt: reemplaza el texto actual (la cuenta 3-2-1 va más rápido que la animación).
        public void Show(string text, bool interrupt = false)
        {
            if (!showing || interrupt)
                Begin(text);
            else if (queue.Count < MAX_QUEUE)
                queue.Enqueue(text);
        }

        private void Begin(string text)
        {
            showing = true;
            elapsed = 0f;
            label.SetText(text);
            label.alpha = 1f;
            label.transform.localScale = Vector3.zero;
            label.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!showing)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < POP_TIME)
            {
                float t = elapsed / POP_TIME;
                // 0 a 1.2 en el primer 60 %, 1.2 a 1 en el resto.
                float scale = t < 0.6f ? Mathf.Lerp(0f, OVERSHOOT, t / 0.6f) : Mathf.Lerp(OVERSHOOT, 1f, (t - 0.6f) / 0.4f);
                label.transform.localScale = Vector3.one * scale;
                return;
            }

            label.transform.localScale = Vector3.one;
            float fadeStart = POP_TIME + HOLD_TIME;
            if (elapsed > fadeStart)
                label.alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / FADE_TIME);
            if (elapsed < fadeStart + FADE_TIME)
                return;

            showing = false;
            label.gameObject.SetActive(false);
            if (queue.Count > 0)
                Begin(queue.Dequeue());
        }
    }
}
