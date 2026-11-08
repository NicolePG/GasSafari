using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H29 Cangrejo (docs/05): al acercarse la balsa a su zona sube a bordo, se agarra del animal más cercano y le roba
    // 20 de gas por segundo. Se va tras 3 s, o antes si ese animal se tira un pedo (se lo quita de encima) o si lo espantan
    // (IScareable). Sube una vez por pasada y vuelve a su roca al reaparecer.
    public class CrabHazard : HazardBase, IScareable
    {
        private const float CLIMB_TIME = 0.4f;
        private const float HEIGHT_ON_ANIMAL = 0.9f;

        private RaftController raft;
        private PlayerAvatar victim;
        private PlayerAvatar pending;
        private GasTank victimGas;
        private Vector3 home;
        private Quaternion homeRotation;
        private float attachedTime;
        private bool spent;

        public bool Attached => victim != null;
        public bool Spent => spent;
        public float DrainPerSecond => Config.Get("drain", 20f);
        public float StayTime => Config.Get("stayTime", 3f);
        public float StolenGas { get; private set; }

        private void Awake()
        {
            home = transform.position;
            homeRotation = transform.rotation;
        }

        private void OnEnable()
        {
            GameEvents.OnFart += OnFart;
            GameEvents.OnRespawn += Reset;
        }

        private void OnDisable()
        {
            GameEvents.OnFart -= OnFart;
            GameEvents.OnRespawn -= Reset;
        }

        private void Reset()
        {
            victim = null;
            pending = null;
            victimGas = null;
            spent = false;
            StolenGas = 0f;
            SetPresent(true);
            transform.SetPositionAndRotation(home, homeRotation);
        }

        protected override void OnRaftEnter(RaftController enteringRaft)
        {
            if (spent || Attached || IsWarning)
                return;
            // El animal más cercano a la roca: el cangrejo sube por ese lado.
            var seats = enteringRaft.GetComponent<RaftSeats>().Seats;
            PlayerSeat nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < seats.Count; i++)
            {
                float d = (seats[i].transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = seats[i];
                }
            }
            if (nearest == null)
                return;
            // Aviso de 0.5 s (el cangrejo se alza) y luego sube al animal elegido.
            raft = enteringRaft;
            pending = nearest.Avatar;
            Warn();
        }

        protected override void OnWarningEnded()
        {
            if (pending == null || spent || Attached || !pending.GetComponent<PlayerSeat>().IsSeated)
                return;
            victim = pending;
            pending = null;
            victimGas = victim.GetComponent<GasTank>();
            attachedTime = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (!Attached)
                return;

            attachedTime += Time.deltaTime;
            // Sube a la balsa y se queda sobre el animal mientras le roba el gas.
            var target = victim.transform.position + Vector3.up * HEIGHT_ON_ANIMAL;
            transform.position = Vector3.Lerp(transform.position, target, Mathf.Clamp01(attachedTime / CLIMB_TIME));
            if (attachedTime >= CLIMB_TIME)
                StolenGas += victimGas.Drain(DrainPerSecond * Time.deltaTime);
            if (attachedTime >= StayTime || !victim.GetComponent<PlayerSeat>().IsSeated)
                Leave();
        }

        // Un pedo del animal con el cangrejo encima se lo quita.
        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            if (Attached && avatar == victim)
                Leave();
        }

        public void Scare(float duration, Vector3 source)
        {
            if (Attached)
                Leave();
        }

        private void Leave()
        {
            victim = null;
            victimGas = null;
            spent = true;
            // Sale despedido y desaparece (vuelve a su roca al reaparecer).
            SetPresent(false);
        }

        // Sin SetActive: un objeto inactivo dejaría de escuchar la reaparición.
        private void SetPresent(bool present)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = present;
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = present;
        }
    }
}
