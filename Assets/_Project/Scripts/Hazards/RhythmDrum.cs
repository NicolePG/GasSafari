using UnityEngine;

namespace GasSafari.Hazards
{
    // Tambor de W3-5 (docs/04 S1 «entrada con tambores»): late en cada pulso del RhythmClock, que sigue a la música.
    // Es decorado: no tiene collider ni daña.
    public class RhythmDrum : MonoBehaviour
    {
        [SerializeField] private Transform pulse;
        [SerializeField] private float amount = 0.25f;

        private Vector3 baseScale = Vector3.one;

        // 0-1: 1 justo en el pulso y decae hasta el siguiente.
        public float PulseLevel { get; private set; }

        public void Configure(Transform pulsingPart, float pulseAmount = 0.25f)
        {
            pulse = pulsingPart;
            amount = pulseAmount;
        }

        private void Awake()
        {
            if (pulse != null)
                baseScale = pulse.localScale;
        }

        private void Update()
        {
            var clock = RhythmClock.Current;
            if (clock == null)
                return;
            float phase = clock.CyclePhase(1f);
            PulseLevel = Mathf.Pow(1f - phase, 3f);
            if (pulse != null)
                pulse.localScale = baseScale * (1f + amount * PulseLevel);
        }
    }
}
