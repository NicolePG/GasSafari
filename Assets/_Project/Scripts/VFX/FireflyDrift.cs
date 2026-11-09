using UnityEngine;

namespace GasSafari.VFX
{
    // Luciérnaga móvil (W6-2, docs/04): el objeto vaga alrededor de su posición inicial describiendo una curva suave, con fase
    // propia. Lleva la luz y, en la luciérnaga gigante, también el mango dorado como hijo.
    public class FireflyDrift : MonoBehaviour
    {
        [Tooltip("Radio de la deriva en metros: x e z horizontales, y vertical")]
        [SerializeField] private Vector3 radius = new Vector3(2.5f, 0.5f, 2.5f);
        [SerializeField] private float speed = 0.45f;
        [SerializeField] private float phase;

        private Vector3 origin;

        public Vector3 Origin => origin;
        public Vector3 Radius => radius;

        public void Configure(Vector3 driftRadius, float driftSpeed, float startPhase)
        {
            radius = driftRadius;
            speed = driftSpeed;
            phase = startPhase;
        }

        private void Awake()
        {
            origin = transform.position;
        }

        public Vector3 PositionAt(float time)
        {
            float t = time * speed + phase;
            return origin + new Vector3(Mathf.Sin(t) * radius.x, Mathf.Sin(t * 1.7f) * radius.y, Mathf.Cos(t * 0.8f) * radius.z);
        }

        private void Update()
        {
            transform.position = PositionAt(Time.time);
        }
    }
}
