using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H30 Coco que cae (docs/05): sombra de aviso 0.8 s en un punto de 'targets', luego cae del cielo y, si la balsa está a menos
    // de 'radius' (2.5 m) del punto, la empuja 'push' (20) hacia afuera. Sin daño. Sigue un patrón fijo (un punto cada 'interval' s)
    // y solo actúa con la balsa a menos de 'range' m; se reinicia al reaparecer. Con 'aimAtRaft' (W6-4) el punto cae 'aimLead' s por
    // delante de la balsa, desplazado a los lados según un patrón fijo: llueve sobre el equipo, pero la sombra da tiempo de esquivar.
    public class CoconutFall : HazardBase
    {
        [SerializeField] private Transform[] targets;
        [SerializeField] private float interval = 2f;
        [SerializeField] private float range = 45f;
        [SerializeField] private float phase;
        [SerializeField] private Transform shadow;
        [Tooltip("true = los cocos caen por delante de la balsa (aimLead s) en vez de en los puntos fijos")]
        [SerializeField] private bool aimAtRaft;
        [SerializeField] private float aimLead = 0.8f;
        [Tooltip("Coco que cae (desactivado); si falta, una esfera")]
        [SerializeField] private GameObject coconutTemplate;

        private const float DROP_HEIGHT = 14f;
        private const float DROP_TIME = 0.3f;

        private RaftController raft;
        private Transform falling;
        private float timer;
        private int next;
        private int current = -1;
        private Vector3 strikePoint;

        // Desplazamientos laterales (m) del patrón de lluvia sobre la balsa; solo ~1 de cada 3 cae a menos de 'radius'.
        private static readonly float[] AimOffsets = { -4.2f, 3.1f, -1.4f, 5.0f, 0.6f, -5.2f, 2.2f };

        public int Strikes { get; private set; }
        public int Hits { get; private set; }
        public float Radius => Config.Get("radius", 2.5f);
        public float Push => Config.Get("push", 20f);
        public Transform[] Targets => targets;
        public bool AimsAtRaft => aimAtRaft;
        public Vector3 StrikePoint => strikePoint;

        public void Configure(Transform[] points, float secondsBetween, Transform shadowDisc, float startPhase = 0f)
        {
            targets = points;
            interval = secondsBetween;
            shadow = shadowDisc;
            phase = startPhase;
            timer = phase;
        }

        public void SetTemplate(GameObject template) => coconutTemplate = template;

        // Lluvia sobre la balsa: sin puntos fijos, cada coco cae 'lead' s por delante de ella.
        public void ConfigureRain(float secondsBetween, Transform shadowDisc, float lead = 0.8f, float startPhase = 0f)
        {
            aimAtRaft = true;
            aimLead = lead;
            interval = secondsBetween;
            shadow = shadowDisc;
            phase = startPhase;
            timer = phase;
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable() => GameEvents.OnRespawn -= Reset;

        private void Reset()
        {
            timer = phase;
            next = 0;
            current = -1;
            CancelWarning();
            if (shadow != null)
                shadow.gameObject.SetActive(false);
            if (falling != null)
                falling.gameObject.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();
            AnimateCoconut();
            if (raft == null)
                raft = FindFirstObjectByType<RaftController>();
            if (raft == null || (!aimAtRaft && (targets == null || targets.Length == 0)))
                return;
            if (Vector3.Distance(raft.transform.position, transform.position) > range)
                return;

            timer += Time.deltaTime * HazardSpeed;
            if (timer >= interval && !IsWarning)
            {
                timer = 0f;
                current = next;
                if (aimAtRaft)
                {
                    next = (next + 1) % AimOffsets.Length;
                    var velocity = raft.Body.velocity;
                    velocity.y = 0f;
                    var side = velocity.sqrMagnitude > 0.25f ? Vector3.Cross(Vector3.up, velocity.normalized) : transform.right;
                    strikePoint = raft.transform.position + velocity * aimLead + side * AimOffsets[current];
                }
                else
                {
                    next = (next + 1) % targets.Length;
                    strikePoint = targets[current].position;
                }
                if (shadow != null)
                {
                    shadow.position = strikePoint;
                    shadow.gameObject.SetActive(true);
                }
                Warn();
            }
        }

        // El coco baja desde 14 m sobre el punto durante el último 0.3 s del aviso.
        private void AnimateCoconut()
        {
            if (!IsWarning || current < 0)
            {
                if (falling != null)
                    falling.gameObject.SetActive(false);
                return;
            }
            if (falling == null)
            {
                GameObject coco;
                if (coconutTemplate != null)
                {
                    coco = Instantiate(coconutTemplate);
                    coco.SetActive(true);
                }
                else
                {
                    coco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(coco.GetComponent<Collider>());
                    coco.transform.localScale = Vector3.one * 1.1f;
                }
                coco.name = "Coco";
                falling = coco.transform;
                falling.SetParent(transform, true);
            }
            float remaining = Config.warningTime * (1f - WarningProgress);
            falling.gameObject.SetActive(remaining <= DROP_TIME);
            float k = Mathf.Clamp01(remaining / DROP_TIME);
            falling.position = strikePoint + Vector3.up * (0.6f + DROP_HEIGHT * k);
        }

        protected override void OnWarningEnded()
        {
            if (falling != null)
                falling.gameObject.SetActive(false);
            if (shadow != null)
                shadow.gameObject.SetActive(false);
            if (current < 0 || raft == null)
                return;
            Strikes++;
            var point = strikePoint;
            var flat = raft.transform.position - point;
            flat.y = 0f;
            if (flat.magnitude > Radius)
                return;
            // Empuje horizontal alejándose del punto de impacto (si cae justo en el centro, hacia el lado del cauce).
            var away = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.right;
            Hits++;
            PushRaft(raft, away * Push, raft.Body.worldCenterOfMass);
        }
    }
}
