using UnityEngine;

namespace GasSafari.Core
{
    // Único dueño de Time.timeScale (docs/10 §4). La duración de la cámara lenta es en tiempo real.
    public class TimeController : MonoBehaviour
    {
        private float baseFixedDeltaTime;
        private float slowTimer;
        private float slowScale = 1f;
        private bool paused;

        public bool IsSlowMotion => slowTimer > 0f;
        public bool IsPaused => paused;

        private void Awake()
        {
            baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        public void SlowMotion(float scale, float duration)
        {
            // Accesibilidad: la cámara lenta se puede apagar.
            if (!Progression.GameSettings.SlowMotion)
                return;
            slowTimer = duration;
            slowScale = scale;
            if (!paused)
                SetScale(scale);
        }

        // Pausa del nivel (docs/07 §4): congela el tiempo y al reanudar vuelve a la cámara lenta si sigue activa.
        public void SetPaused(bool value)
        {
            paused = value;
            SetScale(paused ? 0f : IsSlowMotion ? slowScale : 1f);
        }

        private void Update()
        {
            if (slowTimer <= 0f)
                return;

            slowTimer -= Time.unscaledDeltaTime;
            if (slowTimer <= 0f && !paused)
                SetScale(1f);
        }

        private void OnDestroy()
        {
            SetScale(1f);
        }

        private void SetScale(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = baseFixedDeltaTime * scale;
        }
    }
}
