using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Pedo en el pulso (docs/04 W3-5): un pedo a ±0.1 s del pulso de RhythmClock cuenta como sync y suma al medidor de
    // equipo 'gain' (provisional: docs/04 no da la cifra; usa la ganancia de sync de dos jugadores).
    public class RhythmSyncBonus : MonoBehaviour
    {
        [SerializeField] private float gain = 10f;

        public int OnBeatFarts { get; private set; }

        public void Configure(float meterGain) => gain = meterGain;

        private void OnEnable() => GameEvents.OnFart += OnFart;
        private void OnDisable() => GameEvents.OnFart -= OnFart;

        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            var clock = RhythmClock.Current;
            if (clock == null || !clock.IsOnBeat())
                return;
            var raft = avatar.GetComponentInParent<RaftController>();
            if (raft == null || !raft.TryGetComponent<TeamMeter>(out var meter))
                return;
            OnBeatFarts++;
            meter.Add(gain);
        }
    }
}
