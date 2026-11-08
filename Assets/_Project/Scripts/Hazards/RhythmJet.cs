using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Chorro al ritmo (docs/04 W3-5 S2 «plataformas y chorros al ritmo»): brota en los pulsos marcados de un compás de
    // 'cycleBeats' pulsos (por defecto el 1 y el 3) y, mientras brota, acelera la balsa hacia transform.right ('accel'
    // m/s²). No daña. Lee de RhythmClock, que sigue a la música, así que cambia de ritmo con el cambio de canción.
    public class RhythmJet : HazardBase
    {
        [SerializeField] private Transform column;
        [SerializeField] private int cycleBeats = 4;
        [Tooltip("Bits de los pulsos en que brota: bit 0 = pulso 1 del compás")]
        [SerializeField] private int activeMask = 0b0101;

        public bool IsActive { get; private set; }
        public float Accel => Config.Get("accel", 4f);

        public void Configure(Transform jetColumn, int beatsPerCycle, int mask)
        {
            column = jetColumn;
            cycleBeats = beatsPerCycle;
            activeMask = mask;
        }

        protected override void Update()
        {
            base.Update();
            var clock = RhythmClock.Current;
            IsActive = false;
            if (clock != null)
            {
                int beat = Mathf.Min(cycleBeats - 1, (int)(clock.CyclePhase(cycleBeats) * cycleBeats));
                IsActive = ((activeMask >> beat) & 1) != 0;
            }
            if (column != null)
            {
                float target = IsActive ? 1f : 0.08f;
                float k = Mathf.MoveTowards(column.localScale.y, target, Time.deltaTime * 8f);
                column.localScale = new Vector3(1f, k, 1f);
            }
        }

        protected override void OnRaftStay(RaftController raft)
        {
            if (!IsActive)
                return;
            var direction = transform.right;
            direction.y = 0f;
            direction.Normalize();
            var body = raft.Body;
            PushRaft(raft, direction * (Accel * body.mass * Time.fixedDeltaTime), body.worldCenterOfMass);
        }
    }
}
