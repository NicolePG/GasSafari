using GasSafari.Characters;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H34 Chorro de manguera o fuente (docs/05): empuje lateral constante (6 m/s², hacia transform.right) en un tramo de
    // 4 m de ancho (el trigger). Tras 1.5 s dentro, tira al agua a todo animal que no esté agarrado.
    public class HoseJet : HazardBase
    {
        private RaftController inside;
        private float insideTime;
        private float lastStayTime = -1f;

        public float Acceleration => Config.Get("accel", 6f);
        public float KnockTime => Config.Get("knockTime", 1.5f);
        public bool RaftInside => inside != null;
        public Vector3 PushDirection
        {
            get
            {
                var d = transform.right;
                d.y = 0f;
                return d.normalized;
            }
        }

        protected override void OnRaftEnter(RaftController raft)
        {
            inside = raft;
            insideTime = 0f;
        }

        protected override void OnRaftExit(RaftController raft)
        {
            if (raft == inside)
                inside = null;
        }

        protected override void OnRaftStay(RaftController raft)
        {
            // Varios colliders de la balsa: un empuje por paso físico.
            if (Mathf.Approximately(lastStayTime, Time.fixedTime))
                return;
            lastStayTime = Time.fixedTime;
            inside = raft;

            var body = raft.Body;
            PushRaft(raft, PushDirection * (Acceleration * body.mass * Time.fixedDeltaTime), body.worldCenterOfMass);

            insideTime += Time.fixedDeltaTime;
            if (insideTime >= KnockTime)
                KnockLoose(raft);
        }

        private void KnockLoose(RaftController raft)
        {
            var seats = raft.GetComponent<RaftSeats>().Seats;
            for (int i = seats.Count - 1; i >= 0; i--)
            {
                var seat = seats[i];
                if (seat.TryGetComponent<AvatarStateMachine>(out var states) && states.IsGrabbing)
                    continue;
                if (seat.TryGetComponent<AvatarFall>(out var fall))
                    fall.Fall(PushDirection * 2f);
            }
        }
    }
}
