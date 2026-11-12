using System;
using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H18 Zona oscura (docs/05): dentro de la zona la visibilidad baja a 'visibility' m (niebla negra); las luciérnagas
    // (luces) iluminan su entorno. Con 'blackoutSeconds' > 0, al entrar la balsa se produce un APAGÓN (W4-2): las luciérnagas
    // se apagan una tras otra con parpadeos y un soplo, la visibilidad cae a 'blackoutVisibility' m y la luz ambiente se
    // atenúa; pasado el tiempo vuelven con un destello y un sonido. Es un cambio gradual (FADE_SECONDS), no un corte.
    [RequireComponent(typeof(BoxCollider))]
    public class DarkZone : MonoBehaviour
    {
        public const float FADE_SECONDS = 1.5f;
        // El cielo (SG_SkyClouds) se oscurece con la zona: la niebla negra ya no deja siluetas contra un cielo claro.
        private static readonly int SkyDark = Shader.PropertyToID("_GS_SkyDark");
        private const float AMBIENT_AT_BLACKOUT = 0.2f;

        [SerializeField] private float visibility = 6f;
        [SerializeField] private Light[] fireflies;
        [SerializeField] private float blackoutSeconds;
        [Tooltip("Visibilidad (m) durante el apagón: más oscuro que el resto de la zona")]
        [SerializeField] private float blackoutVisibility = 3f;

        // Zonas contiguas: la niebla original se guarda al entrar en la primera y se restaura al salir de la última.
        private static int occupied;
        private static bool savedFog;
        private static FogMode savedMode;
        private static float savedStart, savedEnd;
        private static Color savedColor, savedSky, savedEquator, savedGround;

        private bool inside;
        private bool blackoutActive;
        private float blackoutUntil = -1f;
        private float darkness;
        private float[] baseIntensity;

        public static event Action BlackoutStarted;
        public static event Action BlackoutEnded;

        public bool Inside => inside;
        public bool LightsOut => blackoutActive;
        // 0 = luz normal, 1 = apagón completo (sube y baja en FADE_SECONDS).
        public float Darkness => darkness;
        public float BlackoutVisibility => blackoutVisibility;

        public void Configure(float visibleMeters, Light[] lights, float lightsOutSeconds)
        {
            visibility = visibleMeters;
            fireflies = lights;
            blackoutSeconds = lightsOutSeconds;
        }

        private void Awake()
        {
            CaptureIntensities();
        }

        private void CaptureIntensities()
        {
            if (fireflies == null)
                return;
            baseIntensity = new float[fireflies.Length];
            for (int i = 0; i < fireflies.Length; i++)
                baseIntensity[i] = fireflies[i] != null ? fireflies[i].intensity : 0f;
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable()
        {
            GameEvents.OnRespawn -= Reset;
            if (inside)
                Restore();
            darkness = 0f;
            blackoutActive = false;
            ApplyLights();
        }

        private void Reset()
        {
            if (inside)
                Restore();
            blackoutUntil = -1f;
            blackoutActive = false;
            darkness = 0f;
            ApplyLights();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (inside || other.GetComponentInParent<RaftController>() == null)
                return;
            inside = true;
            if (occupied++ == 0)
            {
                savedFog = RenderSettings.fog;
                savedMode = RenderSettings.fogMode;
                savedStart = RenderSettings.fogStartDistance;
                savedEnd = RenderSettings.fogEndDistance;
                savedColor = RenderSettings.fogColor;
                savedSky = RenderSettings.ambientSkyColor;
                savedEquator = RenderSettings.ambientEquatorColor;
                savedGround = RenderSettings.ambientGroundColor;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = visibility;
            Shader.SetGlobalFloat(SkyDark, 1f);
            if (blackoutSeconds > 0f && !blackoutActive)
            {
                blackoutActive = true;
                blackoutUntil = Time.time + blackoutSeconds;
                Play(SoundId.LightsOut);
                BlackoutStarted?.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (inside && other.GetComponentInParent<RaftController>() != null)
                Restore();
        }

        private void Update()
        {
            if (blackoutActive && Time.time >= blackoutUntil)
            {
                blackoutActive = false;
                blackoutUntil = -1f;
                Play(SoundId.LightsOn);
                BlackoutEnded?.Invoke();
            }

            darkness = Mathf.MoveTowards(darkness, blackoutActive ? 1f : 0f, Time.deltaTime / FADE_SECONDS);
            ApplyLights();
            if (inside)
            {
                RenderSettings.fogEndDistance = Mathf.Lerp(visibility, Mathf.Min(blackoutVisibility, visibility), darkness);
                float ambient = Mathf.Lerp(1f, AMBIENT_AT_BLACKOUT, darkness);
                RenderSettings.ambientSkyColor = savedSky * ambient;
                RenderSettings.ambientEquatorColor = savedEquator * ambient;
                RenderSettings.ambientGroundColor = savedGround * ambient;
            }
        }

        // Cada luciérnaga se apaga y se enciende a su turno (escalonado por índice) y parpadea mientras cambia.
        private void ApplyLights()
        {
            if (fireflies == null)
                return;
            if (baseIntensity == null || baseIntensity.Length != fireflies.Length)
                CaptureIntensities();
            int n = fireflies.Length;
            for (int i = 0; i < n; i++)
            {
                var light = fireflies[i];
                if (light == null)
                    continue;
                float stagger = n > 1 ? i / (float)(n - 1) * 0.5f : 0f;
                float level = 1f - Mathf.Clamp01((darkness - stagger) / 0.5f);
                float flicker = level > 0.01f && level < 0.99f ? 0.55f + 0.45f * Mathf.PerlinNoise(Time.time * 25f, i * 7.3f) : 1f;
                light.intensity = baseIntensity[i] * level * flicker;
                light.enabled = level > 0.01f;
            }
        }

        private void Restore()
        {
            inside = false;
            if (--occupied > 0)
                return;
            occupied = 0;
            Shader.SetGlobalFloat(SkyDark, 0f);
            RenderSettings.fog = savedFog;
            RenderSettings.fogMode = savedMode;
            RenderSettings.fogColor = savedColor;
            RenderSettings.fogStartDistance = savedStart;
            RenderSettings.fogEndDistance = savedEnd;
            RenderSettings.ambientSkyColor = savedSky;
            RenderSettings.ambientEquatorColor = savedEquator;
            RenderSettings.ambientGroundColor = savedGround;
        }

        private void Play(SoundId id)
        {
            if (Services.IsReady && Services.Audio != null)
                Services.Audio.Play(id, transform.position);
        }
    }
}
