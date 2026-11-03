using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Team
{
    // Detecta grupos de sincronía del equipo de una balsa y llena el medidor.
    [RequireComponent(typeof(TeamMeter))]
    public class SyncDetector : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private SyncGroupTracker tracker;
        private TeamMeter meter;

        public TeamMeter Meter => meter;
        public float Window => Services.Session.Difficulty.syncWindow + ExtraWindow;
        // Bonos temporales (Estrella del Show, cuenta de pings, Ayuda Real).
        public float ExtraWindow { get; set; }

        private void Awake()
        {
            meter = GetComponent<TeamMeter>();
            tracker = new SyncGroupTracker(balance.syncMultipliers, balance.syncMeterGain);
        }

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
