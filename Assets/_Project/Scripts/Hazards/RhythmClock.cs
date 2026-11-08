using System;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Reloj de compás del nivel (docs/05 H16): los obstáculos rítmicos y el bono de pedo en el pulso leen de aquí.
    // 110 BPM al empezar; RhythmTempoZone lo sube a 128 a mitad de W3-5. Las pulsaciones se acumulan, así que cambiar
    // de tempo no salta de fase. Corre con el tiempo del juego (la cámara lenta lo estira igual que a los obstáculos).
    public class RhythmClock : MonoBehaviour
    {
        public const float BEAT_WINDOW = 0.1f;

        private static RhythmClock current;

        [SerializeField] private float bpm = 110f;
        [Tooltip("Sigue los pulsos de la música en reproducción (MusicDirector) en vez de contar por su cuenta")]
        [SerializeField] private bool followMusic;

        public static RhythmClock Current => current;
        public float Bpm => bpm;
        // Pulsos transcurridos desde que empezó el nivel (con decimales).
        public double Beats { get; private set; }

        public event Action<int> BeatStarted;

        private int lastBeat;

        // Con la música de la pista rítmica (W3-5), los pulsos salen de su tabla de tiempos y el BPM es el del tramo que suena.
        public bool FollowsMusic => followMusic && Core.Services.IsReady && Core.Services.Music != null && Core.Services.Music.HasBeats;

        public void Configure(float startBpm, bool followsMusic = false)
        {
            bpm = startBpm;
            followMusic = followsMusic;
        }

        // Con la música mandando, el tempo lo da la pista: el cambio de canción lo hace RhythmTempoZone con SeekToTempoChange.
        public void SetBpm(float value)
        {
            if (!FollowsMusic)
                bpm = Mathf.Max(30f, value);
        }

        private void OnEnable() => current = this;

        private void OnDisable()
        {
            if (current == this)
                current = null;
        }

        private void Update()
        {
            if (FollowsMusic && Core.Services.Music.TryGetBeatPosition(out double musicBeats))
            {
                Beats = musicBeats;
                bpm = Core.Services.Music.CurrentBpm();
            }
            else
            {
                Beats += Time.deltaTime * bpm / 60.0;
            }
            int beat = (int)Beats;
            if (beat != lastBeat)
            {
                lastBeat = beat;
                BeatStarted?.Invoke(beat);
            }
        }

        // Segundos hasta el pulso más cercano (siempre >= 0).
        public float SecondsFromBeat()
        {
            double frac = Beats - Math.Floor(Beats);
            double toNearest = Math.Min(frac, 1.0 - frac);
            return (float)(toNearest * 60.0 / bpm);
        }

        public bool IsOnBeat() => SecondsFromBeat() <= BEAT_WINDOW;

        // Posición 0-1 dentro de un ciclo de 'beatsPerCycle' pulsos.
        public float CyclePhase(float beatsPerCycle)
        {
            double cycle = Beats / Mathf.Max(1f, beatsPerCycle);
            return (float)(cycle - Math.Floor(cycle));
        }

        public void ResetClock()
        {
            Beats = 0.0;
            lastBeat = 0;
        }
    }
}
