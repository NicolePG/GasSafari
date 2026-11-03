using UnityEngine;

namespace GasSafari.Characters
{
    // Lógica del tanque de gas (docs/02 §4), sin dependencias de Unity para poder probarla en EditMode.
    public class GasTankState
    {
        private readonly float regenPerSecond;
        private readonly float regenDelay;
        private float sinceLastUse;

        public float Capacity { get; }
        public float Gas { get; private set; }
        public float RegenMultiplier { get; set; }
        public float Normalized => Capacity > 0f ? Gas / Capacity : 0f;
        public bool IsEmpty => Gas <= 0f;

        public GasTankState(float capacity, float regenPerSecond, float regenDelay, float regenMultiplier)
        {
            Capacity = capacity;
            this.regenPerSecond = regenPerSecond;
            this.regenDelay = regenDelay;
            RegenMultiplier = regenMultiplier;
            Gas = capacity;
        }

        // Todo o nada: toque y cargado.
        public bool TrySpend(float amount)
        {
            if (Gas < amount)
                return false;
            Gas -= amount;
            sinceLastUse = 0f;
            return true;
        }

        // Lo que haya, hasta amount: ráfaga. Devuelve lo gastado.
        public float Drain(float amount)
        {
            float used = Mathf.Min(amount, Gas);
            Gas -= used;
            sinceLastUse = 0f;
            return used;
        }

        public void SetFraction(float fraction)
        {
            Gas = Capacity * Mathf.Clamp01(fraction);
        }

        public void Tick(float deltaTime)
        {
            sinceLastUse += deltaTime;
            // Solo recarga la parte del intervalo posterior al retardo.
            float regenTime = Mathf.Min(deltaTime, sinceLastUse - regenDelay);
            if (regenTime > 0f)
                Gas = Mathf.Min(Capacity, Gas + regenPerSecond * RegenMultiplier * regenTime);
        }
    }
}
