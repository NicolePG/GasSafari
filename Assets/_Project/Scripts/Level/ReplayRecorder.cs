using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Buffer circular de 5 s con las poses de la balsa (docs/10 §5). Al peor choque guarda los últimos 3 s
    // para la repetición "¿Quién fue?" (docs/06 §4).
    public class ReplayRecorder : MonoBehaviour
    {
        private const float BUFFER_SECONDS = 5f;
        private const float CLIP_SECONDS = 3f;

        private readonly Vector3[] positions = new Vector3[(int)(BUFFER_SECONDS * ReplayClip.SAMPLE_RATE)];
        private readonly Quaternion[] rotations = new Quaternion[(int)(BUFFER_SECONDS * ReplayClip.SAMPLE_RATE)];
        private LevelStats stats;
        private RaftController raft;
        private int head;
        private int count;
        private float timer;

        // El choque más grave (a igual gravedad, el último).
        public ReplayClip Worst { get; private set; }

        public void Bind(LevelManager level, LevelStats levelStats)
        {
            raft = level.Raft;
            stats = levelStats;
            stats.Crashed += OnCrashed;
        }

        private void OnDestroy()
        {
            if (stats != null)
                stats.Crashed -= OnCrashed;
        }

        private void Update()
        {
            if (raft == null)
                return;

            timer += Time.deltaTime;
            float step = 1f / ReplayClip.SAMPLE_RATE;
            while (timer >= step)
            {
                timer -= step;
                Sample();
            }
        }

        private void Sample()
        {
            positions[head] = raft.transform.position;
            rotations[head] = raft.transform.rotation;
            head = (head + 1) % positions.Length;
            count = Mathf.Min(count + 1, positions.Length);
        }

        private void OnCrashed(int severity, int culprit)
        {
            if (Worst != null && severity < Worst.Severity)
                return;

            int length = Mathf.Min(count, (int)(CLIP_SECONDS * ReplayClip.SAMPLE_RATE));
            if (length < 2)
                return;

            var clipPositions = new Vector3[length];
            var clipRotations = new Quaternion[length];
            for (int i = 0; i < length; i++)
            {
                int index = (head - length + i + positions.Length) % positions.Length;
                clipPositions[i] = positions[index];
                clipRotations[i] = rotations[index];
            }
            Worst = new ReplayClip(clipPositions, clipRotations, culprit, severity);
        }
    }
}
