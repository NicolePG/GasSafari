using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Team
{
    // Medidor de equipo 0-teamMeterMax (docs/02 §6).
    public class TeamMeter : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        public float Value { get; private set; }
        public float Max => balance.teamMeterMax;
        public float Normalized => Value / Max;
        public bool IsFull => Value >= Max;
        // Fiesta Real de Julien lo duplica mientras dura.
        public float GainMultiplier { get; set; } = 1f;

        public void Add(float amount)
        {
            Set(Value + amount * GainMultiplier);
        }

        // El Súper Pedo gasta el medidor completo.
        public void Consume()
        {
            Set(0f);
        }

        public void Set(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, Max);
            if (Mathf.Approximately(clamped, Value))
                return;
            Value = clamped;
            GameEvents.RaiseTeamMeterChanged(Value);
        }
    }
}
