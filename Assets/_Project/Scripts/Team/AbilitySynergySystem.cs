using System.Collections.Generic;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Team
{
    // Sinergias entre personajes (docs/03). Escucha los eventos de su balsa y aplica el efecto.
    [RequireComponent(typeof(RaftSeats), typeof(SyncDetector), typeof(RaftEffects))]
    public class AbilitySynergySystem : MonoBehaviour
    {
        public const string SLING = "honda_selva";
        public const string EARTHQUAKE = "terremoto";
        public const string PARTY = "fiesta_total";
        public const string HEAVY_DUO = "duo_pesado";
        public const string ROYAL_RESCUE = "rescate_real";

        private const string ALEX = "Alex";
        private const string MELMAN = "Melman";
        private const string GLORIA = "Gloria";
        private const string JULIEN = "Julien";
        private const string ROAR = "roar";
        private const string GALLOP = "gallop";
        private const string BELLY_FLOP = "belly_flop";
        private const int MAX_HITS = 64;

        private static readonly Collider[] Hits = new Collider[MAX_HITS];
        private static readonly List<HazardTags> Done = new List<HazardTags>(MAX_HITS);

        [SerializeField] private BalanceConfig balance;

        private readonly HashSet<string> achieved = new HashSet<string>();
        private RaftSeats seats;
        private SyncDetector sync;
        private RaftEffects effects;
        private RaftController raft;
        private float lastRoar = float.NegativeInfinity;
        private float lastFlop = float.NegativeInfinity;

        public bool HasAchieved(string id) => achieved.Contains(id);

        private void Awake()
        {
            seats = GetComponent<RaftSeats>();
            sync = GetComponent<SyncDetector>();
            effects = GetComponent<RaftEffects>();
            raft = GetComponent<RaftController>();
        }

        private void OnEnable()
        {
            GameEvents.OnAbilityUsed += OnAbilityUsed;
            GameEvents.OnSync += OnSync;
            GameEvents.OnPlayerRescued += OnPlayerRescued;
        }

        private void OnDisable()
        {
            GameEvents.OnAbilityUsed -= OnAbilityUsed;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnPlayerRescued -= OnPlayerRescued;
        }

        // Fiesta Total: lo llama SuperFartController al lanzar. Devuelve la potencia final.
        public float ApplySuperFart(float power)
        {
            if (!sync.IsPartyActive)
                return power;
            Achieve(PARTY);
            return power * balance.partySuperFartPower;
        }

        private bool IsMine(PlayerAvatar avatar)
        {
            var seat = avatar.GetComponent<PlayerSeat>();
            return seat.Raft == seats || avatar.GetComponent<AvatarFall>().HomeRaft == seats;
        }

        private void OnAbilityUsed(PlayerAvatar avatar, string id)
        {
            if (!IsMine(avatar))
                return;
            switch (id)
            {
                case GALLOP:
                    TrySling();
                    break;
                case ROAR:
                    lastRoar = Time.time;
                    TryEarthquake();
                    break;
                case BELLY_FLOP:
                    lastFlop = Time.time;
                    TryEarthquake();
                    break;
            }
        }

        // Honda de la Selva: Galope de Marty durante el Cuello Gancho de Melman.
        private void TrySling()
        {
            if (!TryGetComponent<HookTether>(out var tether))
                return;
            tether.ReleaseSpeedMultiplier = balance.slingReleaseMultiplier;
            Achieve(SLING);
        }

        // Terremoto: Rugido y Panzazo casi a la vez rompen lo Heavy alrededor.
        private void TryEarthquake()
        {
            if (Mathf.Abs(lastRoar - lastFlop) > balance.earthquakeWindow)
                return;
            lastRoar = float.NegativeInfinity;
            lastFlop = float.NegativeInfinity;

            var origin = raft.Body.worldCenterOfMass;
            int mask = (1 << GameLayers.Hazard) | (1 << GameLayers.Environment);
            int count = Physics.OverlapSphereNonAlloc(origin, balance.earthquakeRadius, Hits, mask, QueryTriggerInteraction.Collide);
            Done.Clear();
            for (int i = 0; i < count; i++)
            {
                var tags = Hits[i].GetComponentInParent<HazardTags>();
                if (tags == null || !tags.Has(HazardTag.Heavy) || Done.Contains(tags))
                    continue;
                Done.Add(tags);
                if (tags.TryGetComponent<IBreakable>(out var breakable))
                    breakable.Break(origin);
                else
                    tags.gameObject.SetActive(false);
            }
            Achieve(EARTHQUAKE);
        }

        // Dúo Pesado: Alex y Gloria en el mismo grupo de sync con los asientos cerca.
        private void OnSync(int players, float multiplier)
        {
            PlayerSeat alex = null;
            PlayerSeat gloria = null;
            var list = seats.Seats;
            for (int i = 0; i < list.Count; i++)
            {
                var id = list[i].Avatar.Character.id;
                if (id == ALEX)
                    alex = list[i];
                else if (id == GLORIA)
                    gloria = list[i];
            }
            if (alex == null || gloria == null)
                return;
            if (!sync.IsInGroup(alex.GetComponent<FartThruster>()) || !sync.IsInGroup(gloria.GetComponent<FartThruster>()))
                return;
            if (Mathf.Abs(Mathf.DeltaAngle(alex.Angle, gloria.Angle)) >= balance.heavyDuoMaxSeatAngle)
                return;

            effects.BoostFartForce(balance.heavyDuoForceMultiplier, balance.heavyDuoDuration);
            effects.SuppressWeightTilt(balance.heavyDuoDuration);
            Achieve(HEAVY_DUO);
        }

        // Rescate Real: Melman rescata a Julien con el Cuello Gancho.
        private void OnPlayerRescued(PlayerAvatar rescued, PlayerAvatar rescuer)
        {
            if (rescuer == null || rescuer.Character.id != MELMAN || rescued.Character.id != JULIEN || !IsMine(rescued))
                return;
            rescued.Gas.SetFraction(1f);
            Achieve(ROYAL_RESCUE);
        }

        private void Achieve(string id)
        {
            GameEvents.RaiseSynergy(id, achieved.Add(id));
        }
    }
}
