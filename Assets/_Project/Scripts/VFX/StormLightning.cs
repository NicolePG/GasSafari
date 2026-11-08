using UnityEngine;

namespace GasSafari.VFX
{
    // Rayos de W2-2 (docs/04): cada 'interval' s un destello sube la luz del sol y deja ver las rocas ocultas
    // ('revealed', con su collider siempre activo) durante 'flash' s. Patrón fijo (docs/05: nada aleatorio en tiempo real).
    public class StormLightning : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Renderer[] revealed = System.Array.Empty<Renderer>();
        [SerializeField] private float interval = 6f;
        [SerializeField] private float flash = 0.45f;
        [SerializeField] private float flashIntensity = 3f;

        private float baseIntensity;
        private float clock;

        public bool IsFlashing { get; private set; }

        public void Configure(Light sunLight, Renderer[] hidden, float every, float seconds)
        {
            sun = sunLight;
            revealed = hidden;
            interval = every;
            flash = seconds;
        }

        private void Start()
        {
            if (sun != null)
                baseIntensity = sun.intensity;
            SetFlash(false);
        }

        private void Update()
        {
            clock += Time.deltaTime;
            bool on = clock % interval < flash;
            if (on != IsFlashing)
                SetFlash(on);
        }

        private void SetFlash(bool on)
        {
            IsFlashing = on;
            if (sun != null)
                sun.intensity = on ? flashIntensity : baseIntensity;
            for (int i = 0; i < revealed.Length; i++)
                revealed[i].enabled = on;
        }
    }
}
