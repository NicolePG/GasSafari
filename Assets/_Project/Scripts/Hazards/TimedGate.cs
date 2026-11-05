using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H03 Compuerta temporizada (docs/05): abre y cierra en ciclo (2-3 s, abierta 50 %). Cerrada empuja hacia atrás.
    // El ciclo es una función del reloj del peligro (determinista, multiplicado por la velocidad de la dificultad);
    // 'phase' desfasa unas compuertas de otras. Avisa warningTime antes de cerrar (sonido + evento).
    public class TimedGate : HazardBase
    {
        private const float MOVE_TIME = 0.25f;

        [SerializeField] private Transform door;
        [SerializeField] private Collider doorCollider;
        [Tooltip("m que sube la puerta al abrir")] [SerializeField] private float openHeight = 3.5f;
        [Tooltip("s de desfase respecto al reloj del nivel")] [SerializeField] private float phase;
        [Tooltip("0 = usar 'cycle' del HazardConfig")] [SerializeField] private float cycleOverride;

        private float clock;
        private float closedHeight;
        private float openness;

        public bool IsClosed { get; private set; }
        public float Cycle => cycleOverride > 0f ? cycleOverride : Config.Get("cycle", 2.5f);
        public float OpenFraction => Config.Get("openFraction", 0.5f);

        // Estado puro del ciclo (pruebas): cerrada en la segunda parte del ciclo.
        public static bool ClosedAt(float time, float cycle, float openFraction)
        {
            return Mathf.Repeat(time, cycle) >= cycle * openFraction;
        }

        public void Configure(Transform doorTransform, Collider collider, float height)
        {
            door = doorTransform;
            doorCollider = collider;
            openHeight = height;
        }

        public void SetPhase(float seconds) => phase = seconds;

        private void Awake()
        {
            if (door != null)
                closedHeight = door.localPosition.y;
        }

        protected override void Update()
        {
            base.Update();
            clock += Time.deltaTime * HazardSpeed;

            float openDuration = Cycle * OpenFraction;
            float t = Mathf.Repeat(clock + phase, Cycle);
            bool closed = t >= openDuration;

            // Aviso antes de cerrar.
            if (!closed && !IsWarning && t >= openDuration - Config.warningTime)
                Warn();

            if (closed != IsClosed)
            {
                IsClosed = closed;
                if (!closed && Services.IsReady)
                    Services.Audio.Play(SoundId.GateOpen, transform.position);
            }

            if (door == null)
                return;
            openness = Mathf.MoveTowards(openness, closed ? 0f : 1f, Time.deltaTime / MOVE_TIME);
            var local = door.localPosition;
            local.y = closedHeight + openHeight * openness;
            door.localPosition = local;
            if (doorCollider != null)
                doorCollider.enabled = openness < 0.5f;
        }

        // Cerrada: empuja a la balsa hacia el lado del que venía.
        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            if (!IsClosed)
                return;
            float side = Mathf.Sign(Vector3.Dot(raft.transform.position - transform.position, transform.forward));
            if (side == 0f)
                side = -1f;
            var impulse = transform.forward * (side * Config.Get("pushBack", 12f));
            PushRaft(raft, impulse, raft.Body.worldCenterOfMass);
        }
    }
}
