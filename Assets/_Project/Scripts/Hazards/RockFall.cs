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

        private const float DROP_HEIGHT = 12f;
        private const float DROP_TIME = 0.3f;

        private RaftController raft;
        private Transform fallingRock;
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
            AnimateRock();
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

        // La roca baja desde 12 m sobre el punto durante el último 0.3 s del aviso.
        private void AnimateRock()
        {
            if (!IsWarning || current < 0)
            {
                if (fallingRock != null)
                    fallingRock.gameObject.SetActive(false);
                return;
            }
            if (fallingRock == null)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(rock.GetComponent<Collider>());
                rock.name = "Roca";
                rock.transform.localScale = Vector3.one * 2f;
                fallingRock = rock.transform;
                fallingRock.SetParent(transform, true);
            }
            float remaining = Config.warningTime * (1f - WarningProgress);
            fallingRock.gameObject.SetActive(remaining <= DROP_TIME);
            float k = Mathf.Clamp01(remaining / DROP_TIME);
            fallingRock.position = targets[current].position + Vector3.up * (1f + DROP_HEIGHT * k);
        }

        protected override void OnWarningEnded()
        {
            if (fallingRock != null)
                fallingRock.gameObject.SetActive(false);
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
