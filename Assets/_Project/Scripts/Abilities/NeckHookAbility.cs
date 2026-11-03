using GasSafari.Characters;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Cuello Gancho de Melman (docs/03). Prioridad 1: atraer a un compañero en el agua.
    // Prioridad 2: engancharse al ancla más cercana.
    [CreateAssetMenu(menuName = "Gas Safari/Abilities/Neck Hook", fileName = "NeckHook")]
    public class NeckHookAbility : AbilityBase
    {
        [Tooltip("m")] public float range;
        [Tooltip("Segundos para traer al caído")] public float pullTime;
        [Tooltip("Segundos máximos enganchado al ancla")] public float maxHookTime;

        public override bool CanActivate(AbilityContext context)
        {
            if (context.Raft == null)
                return false;
            return FindSwimmer(context) != null || Anchor.FindNearest(context.Seat.WorldPoint, range) != null;
        }

        public override void Activate(AbilityContext context)
        {
            var swimmer = FindSwimmer(context);
            if (swimmer != null)
            {
                HookPull.Begin(swimmer, context.Avatar, pullTime);
                return;
            }
            var anchor = Anchor.FindNearest(context.Seat.WorldPoint, range);
            HookTether.Attach(context.Raft, anchor, context.Avatar, maxHookTime);
        }

        private AvatarFall FindSwimmer(AbilityContext context)
        {
            var crew = context.Seats.Crew;
            var from = context.Seat.WorldPoint;
            AvatarFall best = null;
            float bestDistance = range * range;
            for (int i = 0; i < crew.Count; i++)
            {
                var fall = crew[i].GetComponent<AvatarFall>();
                if (!fall.IsOverboard || fall.GetComponent<HookPull>() != null)
                    continue;
                float d = (fall.transform.position - from).sqrMagnitude;
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = fall;
                }
            }
            return best;
        }
    }
}
