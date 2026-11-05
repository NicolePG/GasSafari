using GasSafari.Core;
using GasSafari.Hazards;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // Cebolla (P06): nube apestosa alrededor de la balsa; la fauna dentro del radio huye mientras dure.
    public class StinkCloud : TimedEffect
    {
        private const int MAX_HITS = 24;
        private const float TICK_SECONDS = 0.2f;

        private static readonly Collider[] Hits = new Collider[MAX_HITS];

        private float tickTimer;

        public float Radius { get; set; }

        protected override void OnTick(float deltaTime)
        {
            tickTimer -= deltaTime;
            if (tickTimer > 0f)
                return;
            tickTimer = TICK_SECONDS;

            var center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, Radius, Hits, 1 << GameLayers.Hazard, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var tags = Hits[i].GetComponentInParent<HazardTags>();
                if (tags != null && tags.Has(HazardTag.Fauna) && tags.TryGetComponent<IScareable>(out var fauna))
                    fauna.Scare(Remaining, center);
            }
        }
    }
}
