using System;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Team
{
    // Detecta grupos de sincronía del equipo de una balsa y llena el medidor.
    [RequireComponent(typeof(TeamMeter))]
    public class SyncDetector : MonoBehaviour
    {
        private static readonly int SourceCount = Enum.GetValues(typeof(SyncBonusSource)).Length;

        [SerializeField] private BalanceConfig balance;

        private SyncGroupTracker tracker;
        private TeamMeter meter;
        private float[] bonusAmounts;
        private float[] bonusTimers;

        public TeamMeter Meter => meter;
        public float Window => Services.Session.Difficulty.syncWindow + ExtraWindow;

        public float ExtraWindow
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < SourceCount; i++)
                {
                    if (bonusTimers[i] > 0f)
                        total += bonusAmounts[i];
                }
                return total;
            }
        }

        private void Awake()
        {
            meter = GetComponent<TeamMeter>();
            tracker = new SyncGroupTracker(balance.syncMultipliers, balance.syncMeterGain);
            bonusAmounts = new float[SourceCount];
            bonusTimers = new float[SourceCount];
        }

        // Amplía la ventana durante 'duration' segundos. Repetir la misma fuente la renueva.
        public void SetWindowBonus(SyncBonusSource source, float amount, float duration)
        {
            bonusAmounts[(int)source] = amount;
            bonusTimers[(int)source] = duration;
        }

        private void Update()
        {
            for (int i = 0; i < SourceCount; i++)
            {
                if (bonusTimers[i] > 0f)
                    bonusTimers[i] -= Time.deltaTime;
            }
        }

        // Si el miembro está en el grupo de sync actual (válido justo al recibir OnSync).
        public bool IsInGroup(ISyncMember member) => tracker.Contains(member);

        // Lo llama cada inicio de pedo. Devuelve el multiplicador para ese pedo.
        public float Register(ISyncMember member, FartType type, float baseImpulse)
        {
            var result = tracker.Register(member, type, baseImpulse, Time.time, Window);
            if (result.GroupSize < 2)
                return result.Multiplier;

            meter.Add(result.MeterGain);
            GameEvents.RaiseSync(result.GroupSize, result.Multiplier);
            return result.Multiplier;
        }
    }
}
