using UnityEngine;

namespace GasSafari.Abilities
{
    // Galope Turbo de Marty (docs/03): impulso en la dirección de su empuje y menos arrastre en el agua.
    [CreateAssetMenu(menuName = "Gas Safari/Abilities/Gallop", fileName = "Gallop")]
    public class GallopAbility : AbilityBase
    {
        public float impulse;
        [Tooltip("Multiplica waterDrag mientras dura")] public float waterDragMultiplier;
        [Tooltip("Segundos")] public float duration;

        public override bool CanActivate(AbilityContext context) => context.Raft != null;

        // Sin costo de gas.
        public override void Activate(AbilityContext context)
        {
            var push = context.Thruster.ThrustDirection * impulse;
            context.Raft.ApplyThrust(push, context.Raft.SeatThrustPoint(context.Seat.WorldPoint), ForceMode.Impulse);
            context.Effects.ScaleWaterDrag(waterDragMultiplier, duration);
        }
    }
}
