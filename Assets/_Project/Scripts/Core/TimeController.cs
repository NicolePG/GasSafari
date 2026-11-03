using UnityEngine;

namespace GasSafari.Core
{
    // Único dueño de Time.timeScale (docs/10 §4). La duración de la cámara lenta es en tiempo real.
    public class TimeController : MonoBehaviour
    {
        private float baseFixedDeltaTime;
        private float slowTimer;

        public bool IsSlowMotion => slowTimer > 0f;

        private void Awake()
        {
            baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        public void SlowMotion(float scale, float duration)
        {
            slowTimer = duration;
            SetScale(scale);
        }

        private void Update()
        {
            if (slowTimer <= 0f)
                return;

            slowTimer -= Time.unscaledDeltaTime;
            if (slowTimer <= 0f)
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
