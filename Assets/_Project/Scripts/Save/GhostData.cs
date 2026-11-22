using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Save
{
    // Fantasma del Contrarreloj (docs/06 §1): pose de la balsa a ritmo fijo y el tiempo de cada checkpoint.
    public sealed class GhostData
    {
        public string LevelId;
        // Tiempo total con penalizaciones (el que cuenta para el récord).
        public float Time;
        public float SampleRate = 10f;
        public int Count;
        public Vector3[] Positions = new Vector3[0];
        public Quaternion[] Rotations = new Quaternion[0];
        // Tiempo total al llegar a cada checkpoint (índice del checkpoint); negativo = no llegó.
        public float[] Splits = new float[0];
        public readonly List<string> Characters = new List<string>();

        public float Duration => Count > 0 ? (Count - 1) / SampleRate : 0f;

        public float SplitAt(int index) => index >= 0 && index < Splits.Length ? Splits[index] : -1f;

        // Pose interpolada en el segundo t de carrera (antes del primero o después del último se queda quieto).
        public void Sample(float t, out Vector3 position, out Quaternion rotation)
        {
            if (Count == 0)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }
            float f = Mathf.Clamp(t * SampleRate, 0f, Count - 1);
            int i = Mathf.Min((int)f, Count - 1);
            int j = Mathf.Min(i + 1, Count - 1);
            float k = f - i;
            position = Vector3.LerpUnclamped(Positions[i], Positions[j], k);
            rotation = Quaternion.SlerpUnclamped(Rotations[i], Rotations[j], k);
        }
    }
}
