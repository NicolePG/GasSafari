using UnityEngine;
using UnityEngine.Rendering;

namespace GasSafari.Modes
{
    // Modificador Cámara mareada (docs/06 §1): la cámara principal se balancea (giro sobre su eje y un poco de lado a lado).
    // Se aplica justo antes de dibujar y se deshace al terminar, así no se suma a lo que mueve Cinemachine.
    public class DizzyCamera : MonoBehaviour
    {
        private float amplitude = 6f;
        private float period = 4f;
        private Camera target;
        private Quaternion saved;
        private bool applied;

        public float CurrentRoll { get; private set; }

        public void Setup(float degrees, float seconds)
        {
            amplitude = degrees;
            period = Mathf.Max(0.5f, seconds);
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBegin;
            RenderPipelineManager.endCameraRendering += OnEnd;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBegin;
            RenderPipelineManager.endCameraRendering -= OnEnd;
            Restore();
        }

        // Balanceo en el instante t (grados de giro y de lado).
        public static Vector2 Sway(float t, float degrees, float seconds)
        {
            float phase = t * 2f * Mathf.PI / seconds;
            return new Vector2(Mathf.Sin(phase) * degrees, Mathf.Sin(phase * 0.5f) * degrees * 0.35f);
        }

        private void OnBegin(ScriptableRenderContext context, Camera camera)
        {
            if (applied || camera == null || !camera.CompareTag("MainCamera"))
                return;
            target = camera;
            saved = camera.transform.rotation;
            var sway = Sway(Time.time, amplitude, period);
            CurrentRoll = sway.x;
            camera.transform.rotation = saved * Quaternion.Euler(0f, sway.y, sway.x);
            applied = true;
        }

        private void OnEnd(ScriptableRenderContext context, Camera camera)
        {
            if (camera == target)
                Restore();
        }

        private void Restore()
        {
            if (applied && target != null)
                target.transform.rotation = saved;
            applied = false;
        }
    }
}
