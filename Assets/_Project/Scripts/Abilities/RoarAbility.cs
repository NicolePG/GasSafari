using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Hazards;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Rugido de la Selva de Alex (docs/03): cono hacia la proa que rompe lo Fragile, espanta la Fauna y frena.
    [CreateAssetMenu(menuName = "Gas Safari/Abilities/Roar", fileName = "Roar")]
    public class RoarAbility : AbilityBase
    {
        private const int MAX_HITS = 64;
        private static readonly Collider[] Hits = new Collider[MAX_HITS];
        private static readonly List<HazardTags> Done = new List<HazardTags>(MAX_HITS);

        [Tooltip("m")] public float range;
        [Tooltip("Grados de apertura total del cono")] public float coneAngle;
        [Tooltip("Segundos que la fauna retrocede")] public float scareDuration;
        [Tooltip("Impulso hacia atrás")] public float brakeImpulse;

        public override bool CanActivate(AbilityContext context) => context.Raft != null;

        public override void Activate(AbilityContext context)
        {
            var raft = context.Raft;
            var origin = raft.Body.worldCenterOfMass;
            var forward = raft.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            int mask = (1 << GameLayers.Hazard) | (1 << GameLayers.Environment);
            int count = Physics.OverlapSphereNonAlloc(origin, range, Hits, mask, QueryTriggerInteraction.Collide);
            Done.Clear();
            for (int i = 0; i < count; i++)
            {
                var tags = Hits[i].GetComponentInParent<HazardTags>();
                if (tags == null || Done.Contains(tags) || !InCone(origin, forward, Hits[i].bounds.ClosestPoint(origin)))
                    continue;
                Done.Add(tags);
                Affect(tags, origin);
            }

            raft.ApplyThrust(-forward * brakeImpulse, origin, ForceMode.Impulse);
        }

        public bool InCone(Vector3 origin, Vector3 forward, Vector3 point)
        {
            var to = point - origin;
            to.y = 0f;
            return to.sqrMagnitude < 1e-4f || Vector3.Angle(forward, to) <= coneAngle * 0.5f;
        }

        private void Affect(HazardTags tags, Vector3 origin)
        {
            if (tags.Has(HazardTag.Fauna) && tags.TryGetComponent<IScareable>(out var fauna))
                fauna.Scare(scareDuration, origin);

            if (!tags.Has(HazardTag.Fragile))
                return;
            if (tags.TryGetComponent<IBreakable>(out var breakable))
                breakable.Break(origin);
            else
                tags.gameObject.SetActive(false);
        }
    }
}
