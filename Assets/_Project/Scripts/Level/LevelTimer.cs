using UnityEngine;

namespace GasSafari.Level
{
    // Cronómetro del nivel (docs/02 §10): arranca en la salida tras el 3-2-1 y se detiene en la meta.
    public class LevelTimer : MonoBehaviour
    {
        public float RunTime { get; private set; }
        public float PenaltyTime { get; private set; }
        public float Total => RunTime + PenaltyTime;
        public bool IsRunning { get; private set; }

        public void StartTimer()
        {
            IsRunning = true;
        }

        public void StopTimer()
        {
            IsRunning = false;
        }

        public void ResetTimer()
        {
            IsRunning = false;
            RunTime = 0f;
            PenaltyTime = 0f;
        }

        public void AddPenalty(float seconds)
        {
            PenaltyTime += seconds;
        }

        private void Update()
        {
            if (IsRunning)
                RunTime += Time.deltaTime;
        }
    }
}
