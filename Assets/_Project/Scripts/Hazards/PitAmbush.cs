using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H14 Fosa (docs/05): una fosa escondida en un arbusto de la orilla. Cuando la balsa entra en su zona, el arbusto se
    // mueve 1 s (el aviso) y la fosa salta: tira al agua a un animal que no esté agarrado. Se supera agarrándose, con el
    // Rugido o la cebolla (IScareable: se espanta y no ataca). Ataca una vez por pasada y se rearma al reaparecer.
    public class PitAmbush : HazardBase, IScareable
    {
        private const float SHAKE_DEGREES = 12f;
        private const float SHAKE_HZ = 9f;

        [SerializeField] private Transform bush;

        private RaftController target;
        private float scaredUntil = -1f;
        private bool spent;
        private Quaternion bushRotation = Quaternion.identity;

        public bool Spent => spent;
        public float KickSpeed => Config.Get("kick", 3f);

        public void Configure(Transform shakingBush)
        {
            bush = shakingBush;
        }

        private void Awake()
        {
            if (bush != null)
                bushRotation = bush.localRotation;
        }

        private void OnEnable() => GameEvents.OnRespawn += Rearm;
        private void OnDisable() => GameEvents.OnRespawn -= Rearm;

        private void Rearm()
        {
            spent = false;
            target = null;
            scaredUntil = -1f;
            CancelWarning();
            if (bush != null)
                bush.localRotation = bushRotation;
        }

        public void Scare(float duration, Vector3 source)
        {
            scaredUntil = Time.time + duration;
            CancelWarning();
        }

        protected override void OnRaftEnter(RaftController raft)
        {
            if (spent || Time.time < scaredUntil)
                return;
            target = raft;
            Warn();
        }

        protected override void Update()
        {
            base.Update();
            if (bush == null)
                return;
            float shake = IsWarning ? Mathf.Sin(Time.time * SHAKE_HZ * Mathf.PI * 2f) * SHAKE_DEGREES : 0f;
            bush.localRotation = bushRotation * Quaternion.Euler(0f, 0f, shake);
        }

        // El aviso terminó: salta sobre la balsa si sigue cerca y no se espantó.
        protected override void OnWarningEnded()
        {
            if (target == null || spent || Time.time < scaredUntil)
                return;
            spent = true;
            var seats = target.GetComponent<RaftSeats>().Seats;
            PlayerSeat victim = null;
            float best = float.MaxValue;
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i].TryGetComponent<AvatarStateMachine>(out var states) && states.IsGrabbing)
                    continue;
                float distance = (seats[i].transform.position - transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    victim = seats[i];
                }
            }
            if (victim != null && victim.TryGetComponent<AvatarFall>(out var fall))
            {
                // Sale despedido hacia afuera de la balsa (como en un choque), no de vuelta hacia la cubierta.
                var away = victim.transform.position - target.transform.position;
                away.y = 0f;
                fall.Fall(away.normalized * KickSpeed + Vector3.up * 1.5f);
            }
        }
    }
}
