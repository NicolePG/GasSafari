using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H35 Plataforma que se hunde (docs/05): flota hasta que la balsa la toca; 1 s después empieza a hundirse y en 6 s
    // queda bajo el agua (y se apaga). Vuelve a su sitio al reaparecer en el checkpoint.
    [RequireComponent(typeof(Rigidbody))]
    public class SinkingPlatform : HazardBase
    {
        private Rigidbody body;
        private Vector3 startPosition;
        private bool hasStart;
        private float sinkDepth;
        private float timer = -1f;

        public float SinkDelay => Config.Get("sinkDelay", 1f);
        public float SinkTime => Config.Get("sinkTime", 6f);
        public bool IsSinking => timer >= SinkDelay;
        public bool IsGone { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var c in GetComponentsInChildren<Collider>())
                bounds.Encapsulate(c.bounds);
            sinkDepth = bounds.size.y + 1f;
        }

        private void OnEnable() => GameEvents.OnRespawn += ResetPlatform;
        private void OnDisable() => GameEvents.OnRespawn -= ResetPlatform;

        protected override void OnRaftEnter(RaftController raft)
        {
            if (timer < 0f && !IsGone)
            {
                // La posición de reposo se toma al primer toque: el nivel puede moverla después de Awake.
                if (!hasStart)
                {
                    startPosition = transform.position;
                    hasStart = true;
                }
                timer = 0f;
                Warn();
            }
        }

        private void FixedUpdate()
        {
            if (timer < 0f || IsGone)
                return;
            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01((timer - SinkDelay) / SinkTime);
            body.MovePosition(startPosition + Vector3.down * (sinkDepth * t));
            if (t >= 1f)
                SetGone(true);
        }

        public void ResetPlatform()
        {
            timer = -1f;
            CancelWarning();
            if (hasStart)
            {
                transform.position = startPosition;
                body.position = startPosition;
            }
            SetGone(false);
        }

        private void SetGone(bool gone)
        {
            IsGone = gone;
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = !gone;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = !gone;
        }
    }
}
