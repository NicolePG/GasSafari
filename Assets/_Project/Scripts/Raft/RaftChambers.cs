using System;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Las 4 cámaras de aire (docs/02 §8). Índices como RaftBuoyancy: 0 NE, 1 NO, 2 SE, 3 SO.
    [RequireComponent(typeof(RaftBuoyancy))]
    public class RaftChambers : MonoBehaviour
    {
        public const int CHAMBER_COUNT = 4;
        private const int ALL_INTACT = (1 << CHAMBER_COUNT) - 1;

        [SerializeField] private BalanceConfig balance;

        private RaftBuoyancy buoyancy;
        private int intactMask = ALL_INTACT;
        private float sinkTimer;

        public bool HasShield { get; private set; }
        public bool IsSinking { get; private set; }
        public int IntactMask => intactMask;
        public int IntactCount => CountBits(intactMask);

        public event Action ShieldConsumed;

        private void Awake()
        {
            buoyancy = GetComponent<RaftBuoyancy>();
        }

        public bool IsIntact(int chamber) => (intactMask & (1 << chamber)) != 0;

        public int ChamberAt(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            bool east = local.x >= 0f;
            bool north = local.z >= 0f;
            if (north)
                return east ? 0 : 1;
            return east ? 2 : 3;
        }

        // Daño en un punto: revienta la cámara de ese cuadrante. Devuelve si se reventó.
        public bool PopChamberAt(Vector3 worldPoint)
        {
            return PopChamber(ChamberAt(worldPoint));
        }

        public bool PopChamber(int chamber)
        {
            if (IsSinking || !IsIntact(chamber))
                return false;

            if (HasShield)
            {
                HasShield = false;
                ShieldConsumed?.Invoke();
                return false;
            }

            intactMask &= ~(1 << chamber);
            buoyancy.SetChamberFactor(chamber, balance.poppedFactor);
            GameEvents.RaiseChamberPopped(chamber);

            if (intactMask == 0)
                StartSinking();
            return true;
        }

        public void Repair(int chamber)
        {
            if (IsIntact(chamber))
                return;

            intactMask |= 1 << chamber;
            buoyancy.SetChamberFactor(chamber, 1f);
            GameEvents.RaiseChamberRepaired(chamber);
        }

        public void GiveShield()
        {
            HasShield = true;
        }

        // Restaura un estado guardado (checkpoint). Cancela el hundimiento.
        public void SetState(int mask)
        {
            IsSinking = false;
            for (int i = 0; i < CHAMBER_COUNT; i++)
            {
                bool intact = (mask & (1 << i)) != 0;
                if (intact && !IsIntact(i))
                    GameEvents.RaiseChamberRepaired(i);
                buoyancy.SetChamberFactor(i, intact ? 1f : balance.poppedFactor);
            }
            intactMask = mask & ALL_INTACT;
        }

        private void StartSinking()
        {
            IsSinking = true;
            sinkTimer = balance.sinkDuration;
            for (int i = 0; i < CHAMBER_COUNT; i++)
                buoyancy.SetChamberFactor(i, 0f);
        }

        private void Update()
        {
            if (!IsSinking || sinkTimer <= 0f)
                return;

            sinkTimer -= Time.deltaTime;
            if (sinkTimer <= 0f)
                GameEvents.RaiseRaftSunk();
        }

        private void OnCollisionEnter(Collision collision)
        {
            int layer = collision.gameObject.layer;
            if (layer != GameLayers.Environment && layer != GameLayers.Hazard && layer != GameLayers.Raft)
                return;

            var difficulty = Services.Session.Difficulty;
            // Cuenta la velocidad con que la balsa golpea la superficie (como knockoffSpeed), no la fuerza del contacto:
            // así no depende de la masa de la balsa ni de cuántos animales lleva.
            if (!difficulty.impactsDamageChambers)
                return;

            float impact = RaftKnockoff.NormalImpactSpeed(collision);
            if (impact <= difficulty.chamberDamageSpeed)
                return;

            PopChamberAt(collision.GetContact(0).point);
        }

        private static int CountBits(int mask)
        {
            int count = 0;
            for (; mask != 0; mask &= mask - 1)
                count++;
            return count;
        }
    }
}
