using UnityEngine;

namespace GasSafari.Level
{
    // Poses de la balsa a SAMPLE_RATE Hz para la repetición "¿Quién fue?" (docs/06 §4).
    public sealed class ReplayClip
    {
        public const float SAMPLE_RATE = 10f;

        public readonly Vector3[] Positions;
        public readonly Quaternion[] Rotations;
        // Jugador cuyo pedo causó el choque; -1 si no hubo.
        public readonly int Culprit;
        public readonly int Severity;

        public ReplayClip(Vector3[] positions, Quaternion[] rotations, int culprit, int severity)
        {
            Positions = positions;
            Rotations = rotations;
            Culprit = culprit;
            Severity = severity;
        }

        public int Length => Positions.Length;
        public float Duration => Length / SAMPLE_RATE;

        // Pose interpolada en el segundo t del clip.
        public void Evaluate(float t, out Vector3 position, out Quaternion rotation)
        {
            float f = Mathf.Clamp(t * SAMPLE_RATE, 0f, Length - 1f);
            int a = Mathf.FloorToInt(f);
            int b = Mathf.Min(a + 1, Length - 1);
            float k = f - a;
            position = Vector3.Lerp(Positions[a], Positions[b], k);
            rotation = Quaternion.Slerp(Rotations[a], Rotations[b], k);
        }
    }
}
