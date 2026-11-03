using UnityEngine;

namespace GasSafari.Abilities
{
    // Fiesta Real de Julien (docs/03): todo inicio de pedo cuenta como grupo completo y el medidor sube más.
    [CreateAssetMenu(menuName = "Gas Safari/Abilities/Royal Party", fileName = "RoyalParty")]
    public class RoyalPartyAbility : AbilityBase
    {
        [Tooltip("Segundos")] public float duration;
        [Tooltip("Multiplica lo que sube el medidor de equipo")] public float meterMultiplier;

        public override bool CanActivate(AbilityContext context) => context.Sync != null;

        public override void Activate(AbilityContext context)
        {
            context.Sync.StartParty(duration, meterMultiplier);
        }
    }
}
