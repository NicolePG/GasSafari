using UnityEngine;

namespace GasSafari.Abilities
{
    // Panzazo de Gloria (docs/03): salta y al caer de panza estabiliza la balsa y la empuja con la corriente.
    [CreateAssetMenu(menuName = "Gas Safari/Abilities/Belly Flop", fileName = "BellyFlop")]
    public class BellyFlopAbility : AbilityBase
    {
        [Tooltip("Segundos en el aire antes de caer")] public float jumpTime;
        public float angularDragMultiplier;
        [Tooltip("Segundos de estabilidad y de inmunidad a remolinos")] public float effectDuration;
        [Tooltip("Impulso en la dirección de la corriente")] public float currentImpulse;

        public override bool CanActivate(AbilityContext context) => context.Raft != null;

        public override void Activate(AbilityContext context)
        {
            context.Runner.Cast(jumpTime);
            BellyFlopLanding.Begin(context, this, jumpTime);
        }

        public void Land(AbilityContext context)
        {
            var raft = context.Raft;
            raft.StopRotation();
            context.Effects.ScaleAngularDrag(angularDragMultiplier, effectDuration);
            context.Effects.GrantWhirlpoolImmunity(effectDuration);

            var direction = context.Flow.CurrentDirection;
            if (direction.sqrMagnitude > 0f)
                raft.ApplyThrust(direction * currentImpulse, raft.Body.worldCenterOfMass, ForceMode.Impulse);
        }
    }
}
