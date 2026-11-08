using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H18 Zona oscura (docs/05): dentro de la zona la visibilidad baja a 'visibility' m (niebla negra); las luciérnagas
    // (luces) iluminan su entorno. Con 'blackoutSeconds' > 0, al entrar la balsa se apagan las luciérnagas ese tiempo.
    [RequireComponent(typeof(BoxCollider))]
    public class DarkZone : MonoBehaviour
    {
        [SerializeField] private float visibility = 6f;
        [SerializeField] private Light[] fireflies;
        [SerializeField] private float blackoutSeconds;

        // Zonas contiguas: la niebla original se guarda al entrar en la primera y se restaura al salir de la última.
        private static int occupied;
        private static bool savedFog;
        private static FogMode savedMode;
        private static float savedStart, savedEnd;
        private static Color savedColor;

        private bool inside;
        private float blackoutUntil = -1f;

        public bool Inside => inside;
        public bool LightsOut => Time.time < blackoutUntil;

        public void Configure(float visibleMeters, Light[] lights, float lightsOutSeconds)
        {
            visibility = visibleMeters;
            fireflies = lights;
            blackoutSeconds = lightsOutSeconds;
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable()
        {
            GameEvents.OnRespawn -= Reset;
            if (inside)
                Restore();
        }

        private void Reset()
        {
            if (inside)
                Restore();
            blackoutUntil = -1f;
            SetLights(true);
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
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = visibility;
            if (blackoutSeconds > 0f)
            {
                blackoutUntil = Time.time + blackoutSeconds;
                SetLights(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (inside && other.GetComponentInParent<RaftController>() != null)
                Restore();
        }

        private void Update()
        {
            if (blackoutUntil > 0f && Time.time >= blackoutUntil)
            {
                blackoutUntil = -1f;
                SetLights(true);
            }
        }

        private void Restore()
        {
            inside = false;
            if (--occupied > 0)
                return;
            occupied = 0;
            RenderSettings.fog = savedFog;
            RenderSettings.fogMode = savedMode;
            RenderSettings.fogColor = savedColor;
            RenderSettings.fogStartDistance = savedStart;
            RenderSettings.fogEndDistance = savedEnd;
        }

        private void SetLights(bool on)
        {
            if (fireflies == null)
                return;
            foreach (var light in fireflies)
            {
                if (light != null)
                    light.enabled = on;
            }
        }
    }
}
