using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H16 Obstáculo rítmico (docs/05): una pieza que se mueve al compás de RhythmClock (110 o 128 BPM). Recorre 'travel'
    // metros a lo largo de su eje local 'axis' en el primer medio ciclo y vuelve en el segundo, en ciclos de
    // 'beatsPerCycle' pulsos: se pasa disparando en el pulso, justo cuando la pieza llega al extremo despejado.
    // Con 'onlyLowerAtBeat' > 0 la pieza (plataforma) solo baja en ese pulso del compás y el resto del ciclo está arriba.
    [RequireComponent(typeof(Rigidbody))]
    public class RhythmObstacle : HazardBase
    {
        [SerializeField] private Vector3 axis = Vector3.right;
        [SerializeField] private float travel = 6f;
        [SerializeField] private float beatsPerCycle = 4f;
        [Tooltip("0 = va y vuelve; N = solo baja en el pulso N del compás (1-4)")]
        [SerializeField] private int onlyLowerAtBeat;

        private Rigidbody body;
        private Vector3 origin;

        public float Hit => Config.Get("hit", 20f);
        public float Displacement { get; private set; }

        public void Configure(Vector3 localAxis, float distance, float cycleBeats, int lowerAtBeat)
        {
            axis = localAxis;
            travel = distance;
            beatsPerCycle = cycleBeats;
            onlyLowerAtBeat = lowerAtBeat;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            origin = transform.position;
        }

        private void FixedUpdate()
        {
            var clock = RhythmClock.Current;
            if (clock == null)
                return;

            float phase = clock.CyclePhase(beatsPerCycle);
            float amount;
            if (onlyLowerAtBeat > 0)
            {
                // Baja durante el pulso elegido y sube el resto: pulso N ocupa [N-1, N) / beatsPerCycle del ciclo.
                float beat = phase * beatsPerCycle;
                amount = beat >= onlyLowerAtBeat - 1 && beat < onlyLowerAtBeat ? Mathf.Sin(Mathf.PI * (beat - (onlyLowerAtBeat - 1))) : 0f;
            }
            else
            {
                amount = phase < 0.5f ? Mathf.SmoothStep(0f, 1f, phase * 2f) : Mathf.SmoothStep(1f, 0f, (phase - 0.5f) * 2f);
            }
            Displacement = amount * travel;
            body.MovePosition(origin + transform.TransformDirection(axis.normalized) * Displacement);
        }

        // Golpe: empuja la balsa alejándola de la pieza.
        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            if (collision.contactCount == 0)
                return;
            var point = collision.GetContact(0).point;
            var away = raft.Body.worldCenterOfMass - transform.position;
            away.y = 0f;
            PushRaft(raft, away.normalized * Hit, point);
        }
    }
}
