using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Cambio de canción de W3-5 (docs/04): al cruzar la zona, RhythmClock pasa de 110 a 128 BPM y los obstáculos
    // rítmicos cambian de patrón sin saltos. Se rearma al reaparecer (vuelve al tempo que tenía el checkpoint).
    [RequireComponent(typeof(BoxCollider))]
    public class RhythmTempoZone : MonoBehaviour
    {
        [SerializeField] private float newBpm = 128f;

        private float previousBpm;
        private bool changed;

        public bool Changed => changed;

        public void Configure(float bpm) => newBpm = bpm;

        private void OnEnable() => GameEvents.OnRespawn += Rearm;
        private void OnDisable() => GameEvents.OnRespawn -= Rearm;

        private void Rearm()
        {
            // Si el checkpoint de reaparición queda antes de la zona, se vuelve al tempo anterior.
            if (changed && RhythmClock.Current != null)
                RhythmClock.Current.SetBpm(previousBpm);
            changed = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (changed || other.GetComponentInParent<RaftController>() == null || RhythmClock.Current == null)
                return;
            changed = true;
            previousBpm = RhythmClock.Current.Bpm;
            RhythmClock.Current.SetBpm(newBpm);
        }
    }
}
