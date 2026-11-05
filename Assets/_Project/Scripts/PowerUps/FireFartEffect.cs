using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // Locoto (P02): los pedos del animal valen x[Multiplier] y queman lo Burnable que tienen detrás.
    [RequireComponent(typeof(FartThruster))]
    public class FireFartEffect : TimedEffect
    {
        private const int MAX_HITS = 16;

        private static readonly Collider[] Hits = new Collider[MAX_HITS];

        private FartThruster thruster;
        private float range;

        protected override void OnStarted()
        {
            if (thruster != null)
                return;
            thruster = GetComponent<FartThruster>();
            thruster.Farted += OnFarted;
        }

        // Fuerza y alcance del data del locoto.
        public void Configure(float multiplier, float range)
        {
            this.range = range;
            thruster.Delivery.Multiplier = multiplier;
        }

        protected override void OnEnded()
        {
            thruster.Farted -= OnFarted;
            thruster.Delivery.Multiplier = 1f;
        }

        // El gas sale hacia afuera: a espaldas de la dirección del empuje.
        private void OnFarted(FartType type, float impulse, float syncMultiplier)
        {
            float half = range * 0.5f;
            var origin = transform.position;
            var center = origin - thruster.ThrustDirection * half;
            int count = Physics.OverlapSphereNonAlloc(center, half, Hits, 1 << GameLayers.Hazard, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var tags = Hits[i].GetComponentInParent<HazardTags>();
                if (tags == null || !tags.Has(HazardTag.Burnable))
                    continue;
                if (tags.TryGetComponent<IBurnable>(out var burnable))
                    burnable.Burn(origin);
                else
                    tags.gameObject.SetActive(false);
            }
        }
    }
}
