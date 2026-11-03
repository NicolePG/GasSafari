using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Characters
{
    // Tanque de gas de un animal. La recarga usa el multiplicador de la dificultad activa.
    [RequireComponent(typeof(PlayerAvatar))]
    public class GasTank : MonoBehaviour
    {
        private PlayerAvatar avatar;
        private GasTankState state;

        public float Gas => state.Gas;
        public float Capacity => state.Capacity;
        public float Normalized => state.Normalized;
        public bool IsEmpty => state.IsEmpty;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            var c = avatar.Character;
            state = new GasTankState(c.gasCapacity, c.regenPerSecond, c.regenDelay, CurrentRegenMultiplier());
        }

        public bool TrySpend(float amount)
        {
            if (!state.TrySpend(amount))
                return false;
            CheckEmpty();
            return true;
        }

        public float Drain(float amount)
        {
            float used = state.Drain(amount);
            CheckEmpty();
            return used;
        }

        public void SetFraction(float fraction)
        {
            state.SetFraction(fraction);
        }

        private void Update()
        {
            state.RegenMultiplier = CurrentRegenMultiplier();
            state.Tick(Time.deltaTime);
        }

        private void CheckEmpty()
        {
            if (state.IsEmpty)
                GameEvents.RaiseGasEmpty(avatar);
        }

        private static float CurrentRegenMultiplier()
        {
            return Services.Session.Difficulty.regenMultiplier;
        }
    }
}
