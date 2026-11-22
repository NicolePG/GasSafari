using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Base de todo obstáculo (docs/05). Detecta la balsa y los avatares, aplica daño y empujes con las reglas
    // comunes y ofrece el aviso previo: Warn() abre la ventana y OnWarningEnded() es el momento de actuar.
    public abstract class HazardBase : MonoBehaviour
    {
        // Aviso por cercanía: también avisa con la balsa a menos de esto del borde, vaya a la velocidad que vaya.
        private const float APPROACH_MIN_GAP = 3f;
        // Se rearma cuando todas las balsas están más allá de su distancia de aviso más este margen.
        private const float APPROACH_REARM_MARGIN = 10f;

        [SerializeField] private HazardConfig config;

        private float warningTimer;
        private bool approachArmed = true;

        public HazardConfig Config => config;
        public bool IsWarning { get; private set; }
        // 0 al empezar el aviso, 1 cuando el obstáculo actúa.
        public float WarningProgress => IsWarning ? 1f - warningTimer / config.warningTime : 0f;
        // Multiplicador de velocidad de la dificultad (docs/06 §3).
        protected static float HazardSpeed
        {
            get
            {
                var difficulty = Services.IsReady ? Services.Session.Difficulty : null;
                return difficulty != null ? difficulty.hazardSpeed : 1f;
            }
        }

        // Obstáculos quietos sin aviso propio (remolino, pinchos, red): avisan al acercarse la balsa (UpdateApproachWarning).
        protected virtual bool WarnsOnApproach => false;
        // m del centro al borde que la balsa no debe tocar (aviso por cercanía y esquive de los bots).
        public virtual float WarningRadius => 0f;
        // Dónde está el peligro: centro del aviso por cercanía, flecha del HUD y esquive de los bots.
        public virtual Vector3 WarningCenter => transform.position;

        public void Configure(HazardConfig hazardConfig)
        {
            config = hazardConfig;
        }

        // Aviso común: evento para HUD, Melman y bots, y sonido. No hace nada si ya está avisando.
        public void Warn()
        {
            if (IsWarning)
                return;

            IsWarning = true;
            warningTimer = config.warningTime;
            if (config.warningSound != Audio.SoundId.None && Services.Audio != null)
                Services.Audio.Play(config.warningSound, transform.position);
            GameEvents.RaiseHazardWarning(this);
            OnWarn();
        }

        // Cancela el aviso (reaparición en un checkpoint).
        public void CancelWarning()
        {
            IsWarning = false;
        }

        // Sombra, parpadeo o movimiento previo.
        protected virtual void OnWarn() { }
        protected virtual void OnWarningEnded() { }
        protected virtual void OnRaftEnter(RaftController raft) { }
        protected virtual void OnRaftStay(RaftController raft) { }
        protected virtual void OnRaftExit(RaftController raft) { }
        protected virtual void OnAvatarHit(PlayerAvatar avatar) { }
        // Choque físico con la balsa (con su impulso y puntos de contacto); OnRaftEnter se llama también.
        protected virtual void OnRaftCollision(RaftController raft, Collision collision) { }
        // Contacto sostenido (empuje contra el obstáculo): el impulso es el de cada paso de física.
        protected virtual void OnRaftCollisionStay(RaftController raft, Collision collision) { }

        // Revienta la cámara del punto, salvo que la dificultad solo permita pinchos y mordidas.
        protected bool DamageRaft(RaftController raft, Vector3 worldPoint)
        {
            var difficulty = Services.IsReady ? Services.Session.Difficulty : null;
            if (difficulty != null && !difficulty.impactsDamageChambers && !config.alwaysDamagesChambers)
                return false;
            return raft.GetComponent<RaftChambers>().PopChamberAt(worldPoint);
        }

        protected static void PushRaft(RaftController raft, Vector3 impulse, Vector3 worldPoint)
        {
            raft.ApplyThrust(impulse, worldPoint, ForceMode.Impulse);
        }

        protected virtual void Update()
        {
            if (WarnsOnApproach)
                UpdateApproachWarning();
            if (!IsWarning)
                return;

            warningTimer -= Time.deltaTime;
            if (warningTimer > 0f)
                return;
            IsWarning = false;
            OnWarningEnded();
        }

        // docs/05: todo peligro avisa al menos 0.5 s antes. Avisa cuando una balsa, a su velocidad de acercamiento, llegaría al
        // borde en warningTime s, o ya está a APPROACH_MIN_GAP m. Vista Alta (Melman a bordo, docs/03) suma WarningLeadBonus s.
        private void UpdateApproachWarning()
        {
            var rafts = RaftController.Active;
            var center = WarningCenter;
            float radius = WarningRadius;
            bool reach = false;
            bool allFar = true;
            for (int i = 0; i < rafts.Count; i++)
            {
                var body = rafts[i].Body;
                var offset = center - body.worldCenterOfMass;
                offset.y = 0f;
                float distance = offset.magnitude;
                float closing = distance > 0.01f ? Mathf.Max(0f, Vector3.Dot(body.velocity, offset) / distance) : 0f;
                var effects = rafts[i].Effects;
                float lead = config.warningTime + (effects != null ? effects.WarningLeadBonus : 0f);
                float warnGap = APPROACH_MIN_GAP + closing * lead;
                float gap = distance - radius;
                if (gap <= warnGap)
                    reach = true;
                if (gap <= warnGap + APPROACH_REARM_MARGIN)
                    allFar = false;
            }
            if (!approachArmed)
            {
                approachArmed = allFar;
                return;
            }
            if (!reach)
                return;
            approachArmed = false;
            Warn();
        }

        // Centro y radio horizontal de los colliders del obstáculo (para los que no tienen un radio propio).
        protected void MeasureColliders(out Vector3 center, out float radius)
        {
            center = transform.position;
            radius = 0f;
            var colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
                return;
            var bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            center = bounds.center;
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        private void OnTriggerEnter(Collider other) => Dispatch(other.attachedRigidbody, 0);
        private void OnTriggerStay(Collider other) => Dispatch(other.attachedRigidbody, 1);
        private void OnTriggerExit(Collider other) => Dispatch(other.attachedRigidbody, 2);
        private void OnCollisionEnter(Collision collision)
        {
            Dispatch(collision.rigidbody, 0);
            if (collision.rigidbody != null && collision.rigidbody.TryGetComponent<RaftController>(out var raft))
                OnRaftCollision(raft, collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            Dispatch(collision.rigidbody, 1);
            if (collision.rigidbody != null && collision.rigidbody.TryGetComponent<RaftController>(out var raft))
                OnRaftCollisionStay(raft, collision);
        }
        private void OnCollisionExit(Collision collision) => Dispatch(collision.rigidbody, 2);

        // phase: 0 entra, 1 permanece, 2 sale.
        private void Dispatch(Rigidbody body, int phase)
        {
            if (body == null)
                return;
            if (body.TryGetComponent<RaftController>(out var raft))
            {
                if (phase == 0) OnRaftEnter(raft);
                else if (phase == 1) OnRaftStay(raft);
                else OnRaftExit(raft);
            }
            else if (phase == 0 && body.TryGetComponent<PlayerAvatar>(out var avatar))
            {
                OnAvatarHit(avatar);
            }
        }
    }
}
