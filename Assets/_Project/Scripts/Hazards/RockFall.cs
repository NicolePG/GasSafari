using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H20 Roca que cae (docs/05): sombra de aviso 1 s en un punto de 'targets', luego cae y, si la balsa está a menos de
    // 'radius' (2.5 m) del punto, revienta 1 cámara. Sigue un patrón fijo (un punto cada 'interval' s). Solo actúa cuando la
    // balsa está a menos de 'range' m; se reinicia al reaparecer.
    public class RockFall : HazardBase
    {
        [SerializeField] private Transform[] targets;
        [SerializeField] private float interval = 3f;
        [SerializeField] private float range = 45f;
        [SerializeField] private Transform shadow;

        private RaftController raft;
        private float timer;
        private int next;
        private int current = -1;

        public int Strikes { get; private set; }
        public float Radius => Config.Get("radius", 2.5f);

        public void Configure(Transform[] points, float secondsBetween, Transform shadowDisc)
        {
            targets = points;
            interval = secondsBetween;
            shadow = shadowDisc;
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable() => GameEvents.OnRespawn -= Reset;

        private void Reset()
        {
            timer = 0f;
            next = 0;
            current = -1;
            CancelWarning();
            if (shadow != null)
                shadow.gameObject.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();
            if (raft == null)
                raft = FindFirstObjectByType<RaftController>();
            if (raft == null || targets == null || targets.Length == 0)
                return;
            if (Vector3.Distance(raft.transform.position, transform.position) > range)
                return;

            timer += Time.deltaTime * HazardSpeed;
            if (timer >= interval && !IsWarning)
            {
                timer = 0f;
                current = next;
                next = (next + 1) % targets.Length;
                if (shadow != null)
                {
                    shadow.position = targets[current].position;
                    shadow.gameObject.SetActive(true);
                }
                Warn();
            }
        }

        protected override void OnWarningEnded()
        {
            if (shadow != null)
                shadow.gameObject.SetActive(false);
            if (current < 0 || raft == null)
                return;
            Strikes++;
            var point = targets[current].position;
            var flat = raft.transform.position - point;
            flat.y = 0f;
            if (flat.magnitude <= Radius)
                DamageRaft(raft, raft.Body.ClosestPointOnBounds(point));
        }
    }
}
