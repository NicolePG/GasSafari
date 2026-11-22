using System;
using GasSafari.Save;
using UnityEngine;

namespace GasSafari.Modes
{
    // Graba la pose de la balsa a ritmo fijo (docs/06 §1: 10 Hz) en búferes prealocados y el tiempo de cada checkpoint.
    public sealed class GhostRecorder
    {
        private const int MAX_SPLITS = 64;

        private readonly GhostData data;
        private readonly float[] splits = new float[MAX_SPLITS];
        private int splitCount;

        public GhostData Data => data;
        public int Count => data.Count;

        // expectedSeconds: para dimensionar el búfer (crece solo si la carrera dura más).
        public GhostRecorder(string levelId, float sampleRate, float expectedSeconds)
        {
            data = new GhostData { LevelId = levelId, SampleRate = Mathf.Max(1f, sampleRate) };
            int capacity = Mathf.CeilToInt(Mathf.Max(60f, expectedSeconds) * data.SampleRate) + 1;
            data.Positions = new Vector3[capacity];
            data.Rotations = new Quaternion[capacity];
            for (int i = 0; i < MAX_SPLITS; i++)
                splits[i] = -1f;
        }

        // runTime: segundos de carrera (LevelTimer.RunTime). Rellena las muestras que faltan si un cuadro se las saltó.
        public void Tick(float runTime, Vector3 position, Quaternion rotation)
        {
            while (data.Count <= runTime * data.SampleRate)
            {
                if (data.Count == data.Positions.Length)
                    Grow();
                data.Positions[data.Count] = position;
                data.Rotations[data.Count] = rotation;
                data.Count++;
            }
        }

        // Solo la primera vez que se llega a cada checkpoint.
        public void Split(int checkpoint, float total)
        {
            if (checkpoint < 0 || checkpoint >= MAX_SPLITS || splits[checkpoint] >= 0f)
                return;
            splits[checkpoint] = total;
            splitCount = Mathf.Max(splitCount, checkpoint + 1);
        }

        public GhostData Finish(float total)
        {
            data.Time = total;
            data.Splits = new float[splitCount];
            Array.Copy(splits, data.Splits, splitCount);
            return data;
        }

        private void Grow()
        {
            int size = data.Positions.Length * 2;
            Array.Resize(ref data.Positions, size);
            Array.Resize(ref data.Rotations, size);
        }
    }
}
