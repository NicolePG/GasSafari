using UnityEngine;

namespace GasSafari.Abilities
{
    // Efecto temporal del Panzazo: espera el salto y aplica la caída si Gloria sigue a bordo.
    public class BellyFlopLanding : MonoBehaviour
    {
        private BellyFlopAbility ability;
        private AbilityContext context;
        private float timer;

        public static void Begin(AbilityContext context, BellyFlopAbility ability, float delay)
        {
            var landing = context.Avatar.gameObject.AddComponent<BellyFlopLanding>();
            landing.context = context;
            landing.ability = ability;
            landing.timer = delay;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;
            if (context.Seat.IsSeated)
                ability.Land(context);
            Destroy(this);
        }
    }
}
