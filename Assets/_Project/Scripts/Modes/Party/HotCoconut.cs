using UnityEngine;

namespace GasSafari.Modes
{
    // Coco-bomba de Coco Caliente: flota sobre la cabeza de quien lo tiene, vuela en arco al pasar y parpadea en rojo cada
    // vez más rápido en los últimos segundos de la mecha. Solo se ve: las reglas están en CocoCalienteGame.
    public class HotCoconut : MonoBehaviour
    {
        private const float HEIGHT = 2.1f;
        private const float FLIGHT_SECONDS = 0.3f;
        private const float ARC = 1.2f;
        private const float WARN_SECONDS = 3f;

        [SerializeField] private Renderer shell;
        [SerializeField] private Transform spark;
        [SerializeField] private Color baseColor = new Color(0.45f, 0.28f, 0.14f);
        [SerializeField] private Color hotColor = new Color(1f, 0.15f, 0.08f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;
        private Transform target;
        private Vector3 flightFrom;
        private float flightTime = -1f;
        private float blinkPhase;
        private float warmth;

        public Transform Target => target;
        public bool InFlight => flightTime >= 0f;

        public void Configure(Renderer shellRenderer, Transform sparkPoint)
        {
            shell = shellRenderer;
            spark = sparkPoint;
        }

        public void Follow(Transform holder, bool thrown)
        {
            flightFrom = transform.position;
            target = holder;
            flightTime = thrown ? 0f : -1f;
            if (!thrown)
                transform.position = Above(holder);
            SetColor(0f);
        }

        // 'fraction' 1 = mecha nueva, 0 = explota; 'seconds' lo que queda.
        public void SetFuse(float fraction, float seconds)
        {
            if (seconds > WARN_SECONDS)
            {
                warmth = 0.15f * (1f - fraction);
                return;
            }
            float rate = Mathf.Lerp(8f, 2f, seconds / WARN_SECONDS);
            blinkPhase += Time.deltaTime * rate;
            warmth = Mathf.PingPong(blinkPhase, 1f);
        }

        public void Explode()
        {
            target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            var end = Above(target);
            if (flightTime >= 0f)
            {
                flightTime += Time.deltaTime;
                float t = Mathf.Clamp01(flightTime / FLIGHT_SECONDS);
                transform.position = Vector3.Lerp(flightFrom, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * ARC);
                if (t >= 1f)
                    flightTime = -1f;
            }
            else
            {
                transform.position = end;
            }
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            SetColor(warmth);
            if (spark != null)
                spark.localScale = Vector3.one * (0.18f + 0.08f * Mathf.Sin(Time.time * 30f));
        }

        private static Vector3 Above(Transform holder) => holder.position + Vector3.up * HEIGHT;

        private void SetColor(float heat)
        {
            if (shell == null)
                return;
            if (block == null)
                block = new MaterialPropertyBlock();
            shell.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(baseColor, hotColor, heat));
            shell.SetPropertyBlock(block);
        }
    }
}
