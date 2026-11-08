using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H11 Banco de arena (docs/05): si la balsa entra a menos de 3 m/s (velocidad horizontal), encalla 'groundTime' (2 s):
    // se frena en seco hasta que pasa el tiempo. Con velocidad suficiente lo cruza sin problema. Cada entrada se evalúa aparte.
    [RequireComponent(typeof(Collider))]
    public class SandBar : HazardBase
    {
        // 1/s: lo rápido que la balsa pierde su velocidad horizontal mientras está encallada.
        private const float BRAKE_GAIN = 12f;

        private float groundedUntil = -1f;
        private float lastStep = -1f;

        public float MinSpeed => Config.Get("minSpeed", 3f);
        public float GroundTime => Config.Get("groundTime", 2f);
        public bool IsGrounding => Time.time < groundedUntil;

        protected override void OnRaftEnter(RaftController raft)
        {
            var v = raft.Body.velocity;
            v.y = 0f;
            if (v.magnitude < MinSpeed && !IsGrounding)
                groundedUntil = Time.time + GroundTime;
        }

        protected override void OnRaftStay(RaftController raft)
        {
            if (!IsGrounding || Mathf.Approximately(lastStep, Time.fixedTime))
                return;
            lastStep = Time.fixedTime;
            var body = raft.Body;
            var v = body.velocity;
            body.velocity = new Vector3(v.x * (1f - Mathf.Min(1f, BRAKE_GAIN * Time.fixedDeltaTime)), v.y,
                v.z * (1f - Mathf.Min(1f, BRAKE_GAIN * Time.fixedDeltaTime)));
        }

        protected override void OnRaftExit(RaftController raft) => groundedUntil = -1f;
    }
}
