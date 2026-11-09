using System;
using System.Globalization;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Audio
{
    // Música por mundo con capas (docs/08 §5): cada pista tiene base e intensidad, que suenan a la vez y sincronizadas.
    // La capa de intensidad sube (1 s) cuando la balsa supera 8 m/s o en niveles de jefe, y baja al bajar de 7 m/s.
    // Si la pista trae la tabla de pulsos (jefe de W3: 110 -> 128 BPM), el RhythmClock del nivel se pega a la música.
    public class MusicDirector : MonoBehaviour
    {
        public const float FADE_SECONDS = 1f;
        public const float SPEED_ON = 8f;
        public const float SPEED_OFF = 7f;

        [SerializeField] private MusicLibrary library;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        private AudioSource baseSource;
        private AudioSource intensitySource;
        private float fadeIn = 1f;
        private float fadeOut = 1f;
        private bool stopping;
        private bool intensityWanted;
        private double[] beatTimes;

        // Origen de la velocidad de la balsa (m/s). Por defecto, la balsa del nivel; las pruebas ponen la suya.
        public Func<float> SpeedSource { get; set; }

        public string CurrentId { get; private set; }
        public bool IsBossTrack { get; private set; }
        public bool IsPlaying => baseSource != null && baseSource.isPlaying;
        public bool HasIntensityLayer => intensitySource != null && intensitySource.clip != null;
        // Volumen de la capa de intensidad, 0-1 (sin el volumen general).
        public float IntensityLevel { get; private set; }
        public bool HasBeats => beatTimes != null && beatTimes.Length > 1;

        public void Configure(MusicLibrary musicLibrary) => library = musicLibrary;

        private void Awake()
        {
            baseSource = CreateSource("MusicBase");
            intensitySource = CreateSource("MusicIntensity");
        }

        private AudioSource CreateSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.volume = 0f;
            return source;
        }

        // Música de un nivel: LevelData.musicId; en niveles de jefe la capa de intensidad queda arriba todo el tiempo.
        public void PlayLevel(string id, bool boss)
        {
            Play(id, boss);
        }

        public void Play(string id, bool boss = false)
        {
            if (string.IsNullOrEmpty(id) || library == null)
                return;
            if (CurrentId == id && IsPlaying && !stopping)
            {
                IsBossTrack = boss;
                return;
            }

            var track = library.Get(id);
            if (track == null || track.baseClip == null)
            {
                Debug.LogWarning($"MusicDirector: no hay pista '{id}'");
                return;
            }

            CurrentId = id;
            IsBossTrack = boss;
            stopping = false;
            fadeIn = 0f;
            fadeOut = 1f;
            IntensityLevel = 0f;
            intensityWanted = boss;
            beatTimes = ParseBeats(track.beats);

            // Las dos capas arrancan en el mismo instante del reloj de audio: quedan sincronizadas.
            baseSource.Stop();
            intensitySource.Stop();
            baseSource.clip = track.baseClip;
            intensitySource.clip = track.intensityClip;
            baseSource.volume = 0f;
            intensitySource.volume = 0f;
            double start = AudioSettings.dspTime + 0.1;
            baseSource.PlayScheduled(start);
            if (track.intensityClip != null)
                intensitySource.PlayScheduled(start);
        }

        public void Stop()
        {
            stopping = true;
        }

        private void Update()
        {
            if (CurrentId == null)
                return;

            float dt = Time.unscaledDeltaTime;
            fadeIn = Mathf.MoveTowards(fadeIn, 1f, dt / FADE_SECONDS);
            if (stopping)
            {
                fadeOut = Mathf.MoveTowards(fadeOut, 0f, dt / FADE_SECONDS);
                if (fadeOut <= 0f)
                {
                    baseSource.Stop();
                    intensitySource.Stop();
                    CurrentId = null;
                    stopping = false;
                    return;
                }
            }

            float speed = ReadSpeed();
            if (IsBossTrack)
                intensityWanted = true;
            else if (speed > SPEED_ON)
                intensityWanted = true;
            else if (speed < SPEED_OFF)
                intensityWanted = false;
            IntensityLevel = Mathf.MoveTowards(IntensityLevel, HasIntensityLayer && intensityWanted ? 1f : 0f, dt / FADE_SECONDS);

            float master = volume * fadeIn * fadeOut * Progression.GameVolumes.Music;
            baseSource.volume = master;
            intensitySource.volume = master * IntensityLevel;
        }

        private float ReadSpeed()
        {
            if (SpeedSource != null)
                return SpeedSource();
            var level = LevelManager.Current;
            if (level == null || level.Raft == null)
                return 0f;
            var v = level.Raft.Body.velocity;
            return new Vector2(v.x, v.z).magnitude;
        }

        // ---------------------------------------------------------------- pulsos de la pista
        // Posición en pulsos (con decimales) de la reproducción actual; false si la pista no trae tabla o no suena.
        public bool TryGetBeatPosition(out double beats)
        {
            beats = 0.0;
            if (!HasBeats || !IsPlaying)
                return false;
            double t = baseSource.time;
            int index = Array.BinarySearch(beatTimes, t);
            if (index < 0)
                index = ~index - 1;
            if (index < 0)
                return true;
            double next = index + 1 < beatTimes.Length ? beatTimes[index + 1] : beatTimes[index] + (beatTimes[index] - beatTimes[index - 1]);
            beats = index + (t - beatTimes[index]) / Math.Max(1e-4, next - beatTimes[index]);
            return true;
        }

        // BPM del tramo en reproducción (según el intervalo entre pulsos).
        public float CurrentBpm()
        {
            if (!HasBeats || !TryGetBeatPosition(out double beats))
                return 0f;
            int i = Mathf.Clamp((int)beats, 0, beatTimes.Length - 2);
            return (float)(60.0 / (beatTimes[i + 1] - beatTimes[i]));
        }

        // Cambio de canción de W3-5: salta al primer pulso del tramo rápido si la música aún no llegó a él.
        // Devuelve si se saltó.
        public bool SeekToTempoChange()
        {
            if (!HasBeats || !IsPlaying)
                return false;
            double first = beatTimes[1] - beatTimes[0];
            for (int i = 2; i < beatTimes.Length; i++)
            {
                if (beatTimes[i] - beatTimes[i - 1] < first * 0.9)
                {
                    if (baseSource.time >= beatTimes[i - 1])
                        return false;
                    baseSource.time = (float)beatTimes[i - 1];
                    if (intensitySource.isPlaying)
                        intensitySource.time = (float)beatTimes[i - 1];
                    return true;
                }
            }
            return false;
        }

        private static double[] ParseBeats(TextAsset asset)
        {
            if (asset == null)
                return null;
            string text = asset.text.Trim().TrimStart('[').TrimEnd(']');
            var parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                list[i] = double.Parse(parts[i], CultureInfo.InvariantCulture);
            return list;
        }
    }
}
