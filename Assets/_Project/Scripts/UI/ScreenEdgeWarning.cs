using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Borde de pantalla que late en un color (rojo en el derrumbe de W4-5, docs/04): una viñeta a pantalla completa que se
    // crea sola, late durante 'seconds' y se apaga con un fundido. Un solo aviso a la vez.
    public class ScreenEdgeWarning : MonoBehaviour
    {
        private const float PULSE_HZ = 2.5f;
        private const float FADE_OUT = 0.6f;

        private static ScreenEdgeWarning current;

        private Image image;
        private float remaining;
        private float fade;
        private float peak = 0.75f;

        public static ScreenEdgeWarning Current => current;
        public static bool IsShowing => current != null && current.remaining > 0f;
        // Opacidad actual de la viñeta (0-1), para las pruebas.
        public float Alpha => image != null ? image.color.a : 0f;

        // Muestra el borde (o lo prolonga si ya hay uno).
        public static ScreenEdgeWarning Show(Color color, float seconds)
        {
            if (current == null)
            {
                var go = new GameObject("ScreenEdgeWarning");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                var imageGo = new GameObject("Borde");
                imageGo.transform.SetParent(go.transform, false);
                var rect = imageGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = imageGo.AddComponent<Image>();
                image.raycastTarget = false;
                image.sprite = VignetteSprite();
                current = go.AddComponent<ScreenEdgeWarning>();
                current.image = image;
            }
            current.remaining = Mathf.Max(current.remaining, seconds);
            current.fade = 1f;
            current.image.color = new Color(color.r, color.g, color.b, 0f);
            return current;
        }

        public static void Hide()
        {
            if (current != null)
                current.remaining = 0f;
        }

        private void Update()
        {
            // Tiempo real: el aviso se ve igual en cámara lenta.
            float dt = Time.unscaledDeltaTime;
            Color c = image.color;
            if (remaining > 0f)
            {
                remaining -= dt;
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * PULSE_HZ * Mathf.PI * 2f);
                c.a = peak * pulse * fade;
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 0f, dt / FADE_OUT);
                c.a = Mathf.MoveTowards(c.a, 0f, dt / FADE_OUT);
                if (c.a <= 0.001f)
                {
                    image.color = c;
                    Destroy(gameObject);
                    return;
                }
            }
            image.color = c;
        }

        private void OnDestroy()
        {
            if (current == this)
                current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => current = null;

        // Viñeta: transparente en el centro y opaca en los bordes (textura de 128 x 128 que se estira).
        private static Sprite vignette;

        private static Sprite VignetteSprite()
        {
            if (vignette != null)
                return vignette;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "EdgeVignette" };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distancia al borde más cercano, normalizada: 0 en el borde, 1 en el centro.
                    float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y)) / (size * 0.5f);
                    float alpha = 1f - Mathf.SmoothStep(0f, 0.45f, edge);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            vignette = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return vignette;
        }
    }
}
