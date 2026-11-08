using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Compuerta de represa (docs/04 W5-4; id H37, provisional): ciclo de 'closedSeconds' cerrada (la puerta sólida bloquea el
    // cauce) y 'openSeconds' abierta. Avisa 'warningTime' antes de abrir (la puerta tiembla). Al abrir libera una oleada: durante
    // 'surgeSeconds' acelera hacia transform.forward 'surge' m/s² a la balsa que está en la zona de aguas abajo: se sube con la ola
    // o se espera. El tamaño pequeño (esclusa, S2) usa una oleada débil.
    [RequireComponent(typeof(BoxCollider))]
    public class DamGate : HazardBase
    {
        public enum State : byte { Closed, Open }

        [SerializeField] private Transform door;
        [SerializeField] private Collider blocker;
        [SerializeField] private float phase;
        [SerializeField] private float surgeScale = 1f;
        [SerializeField] private float doorLift = 3.6f;

        private float timer;
        private float openTimer;
        private RaftController raft;
        private Vector3 doorClosedPosition;

        public State Current { get; private set; }
        public float ClosedSeconds => Config.Get("closedSeconds", 6f);
        public float OpenSeconds => Config.Get("openSeconds", 4f);
        public float SurgeSeconds => Config.Get("surgeSeconds", 3f);
        public float Surge => Config.Get("surge", 10f) * surgeScale;
        public int Openings { get; private set; }

        public void Configure(Transform doorTransform, Collider blockingCollider, float startPhase, float surgeFactor)
        {
            door = doorTransform;
            blocker = blockingCollider;
            phase = startPhase;
            surgeScale = surgeFactor;
        }

        private void Awake()
        {
            if (door != null)
                doorClosedPosition = door.localPosition;
            Reset();
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable() => GameEvents.OnRespawn -= Reset;

        private void Reset()
        {
            Current = State.Closed;
            timer = phase;
            openTimer = 0f;
            CancelWarning();
            Apply();
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime * HazardSpeed;
            if (Current == State.Closed)
            {
                timer += dt;
                if (!IsWarning && timer >= ClosedSeconds - Config.warningTime)
                    Warn();
                if (timer >= ClosedSeconds)
                    Open();
            }
            else
            {
                openTimer += dt;
                if (openTimer >= OpenSeconds && !RaftInDoorway())
                {
                    Current = State.Closed;
                    timer = 0f;
                    Apply();
                }
            }
            AnimateDoor();
        }

        // No cierra sobre la balsa: si está en el paso (3 m antes o 4 m después de la puerta), espera a que salga.
        private bool RaftInDoorway()
        {
            if (raft == null)
                raft = FindFirstObjectByType<RaftController>();
            if (raft == null)
                return false;
            var local = transform.InverseTransformPoint(raft.transform.position);
            return Mathf.Abs(local.x) < 8f && local.z > -3f && local.z < 4f;
        }

        private void Open()
        {
            Current = State.Open;
            openTimer = 0f;
            timer = 0f;
            Openings++;
            CancelWarning();
            Apply();
        }

        private void Apply()
        {
            if (blocker != null)
                blocker.enabled = Current == State.Closed;
        }

        private void AnimateDoor()
        {
            if (door == null)
                return;
            float lift = Current == State.Open ? doorLift : 0f;
            // Aviso: la puerta tiembla antes de subir.
            float shake = IsWarning ? Mathf.Sin(Time.time * 40f) * 0.08f : 0f;
            var target = doorClosedPosition + Vector3.up * (lift + shake);
            door.localPosition = Vector3.MoveTowards(door.localPosition, target, Time.deltaTime * 6f);
        }

        // Oleada: solo los primeros 'surgeSeconds' tras abrir.
        protected override void OnRaftStay(RaftController raft)
        {
            if (Current != State.Open || openTimer > SurgeSeconds)
                return;
            var direction = transform.forward;
            direction.y = 0f;
            direction.Normalize();
            var body = raft.Body;
            PushRaft(raft, direction * (Surge * body.mass * Time.fixedDeltaTime), body.worldCenterOfMass);
        }
    }
}
