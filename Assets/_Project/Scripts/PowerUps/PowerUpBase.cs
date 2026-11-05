using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using GasSafari.VFX;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // Base de los power-ups (docs/05): flota con un seno (0.2 m, 1 Hz), gira despacio, se recoge por contacto
    // con la balsa o con un animal fuera de ella (salto de Julien) y reaparece al reiniciar desde un checkpoint
    // previo a su posición (salvo los permanentes).
    [RequireComponent(typeof(Collider))]
    public abstract class PowerUpBase : MonoBehaviour
    {
        private const float BOB_AMPLITUDE = 0.2f;
        private const float BOB_FREQUENCY = 1f;
        private const float SPIN_DEGREES_PER_SECOND = 45f;

        public static readonly List<PowerUpBase> All = new List<PowerUpBase>();

        [SerializeField] private PowerUpData data;

        private Vector3 basePosition;
        private float phase;

        public PowerUpData Data => data;
        public bool IsCollected { get; private set; }
        // Checkpoint activo al recogerlo; al reaparecer en uno igual o anterior, vuelve.
        public int CollectedAtCheckpoint { get; private set; }

        public void Configure(PowerUpData powerUpData)
        {
            data = powerUpData;
        }

        protected abstract void Apply(PowerUpContext context);

        private void Awake()
        {
            All.Add(this);
            basePosition = transform.position;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void OnEnable()
        {
            PowerUpTargets.Add(transform);
        }

        private void OnDisable()
        {
            PowerUpTargets.Remove(transform);
        }

        // Vuelve al mundo (pool o reaparición). Un permanente ya recogido no vuelve.
        public void Respawn()
        {
            if (IsCollected && data.permanent)
                return;
            IsCollected = false;
            gameObject.SetActive(true);
        }

        // Para el pool: vuelve limpio, incluso un permanente, en una nueva posición.
        public void Recycle(Vector3 position)
        {
            IsCollected = false;
            basePosition = position;
            transform.position = position;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            var position = basePosition;
            position.y += Mathf.Sin(phase + Time.time * BOB_FREQUENCY * Mathf.PI * 2f) * BOB_AMPLITUDE;
            transform.SetPositionAndRotation(position,
                transform.rotation * Quaternion.Euler(0f, SPIN_DEGREES_PER_SECOND * Time.deltaTime, 0f));
        }

        private void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || IsCollected)
                return;

            if (body.TryGetComponent<RaftController>(out var raft))
                Collect(raft.GetComponent<RaftSeats>(), null);
            else if (body.TryGetComponent<AvatarFall>(out var fall) && fall.IsOverboard)
                Collect(fall.HomeRaft, fall.GetComponent<PlayerAvatar>());
        }

        // taker nulo: el animal de la balsa más cercano al power-up.
        public void Collect(RaftSeats seats, PlayerAvatar taker)
        {
            if (IsCollected)
                return;
            IsCollected = true;

            if (taker == null)
                taker = NearestCrew(seats);
            Apply(new PowerUpContext(taker, seats));

            var checkpoints = FindFirstObjectByType<CheckpointSystem>();
            CollectedAtCheckpoint = checkpoints != null ? checkpoints.CurrentIndex : 0;
            Feedback(taker);
            GameEvents.RaisePowerUp(data, taker);
            gameObject.SetActive(false);
        }

        private PlayerAvatar NearestCrew(RaftSeats seats)
        {
            PlayerAvatar best = null;
            float bestDistance = float.MaxValue;
            var crew = seats.Crew;
            for (int i = 0; i < crew.Count; i++)
            {
                float d = (crew[i].transform.position - transform.position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = crew[i].Avatar;
                }
            }
            return best;
        }

        // Sonido propio y destello del color del jugador.
        private void Feedback(PlayerAvatar taker)
        {
            if (Services.Audio != null)
                Services.Audio.Play(data.pickupSound, transform.position);
            if (Services.Vfx != null && Services.Vfx.Has(VfxId.Pickup))
                Services.Vfx.Play(VfxId.Pickup, transform.position, Quaternion.identity,
                    taker != null ? taker.PlayerColor : Color.white);
        }
    }
}
