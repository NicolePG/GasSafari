using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using UnityEngine;
using UnityEngine.Playables;

namespace GasSafari.Level
{
    // Momento estrella de un nivel (docs/04): cuando su obstáculo lanza la balsa, cámara lenta (la del Súper Pedo,
    // TimeController) y una toma de Timeline que sigue a la balsa. Una vez por intento: se rearma al reaparecer.
    public class StarMoment : MonoBehaviour
    {
        [SerializeField] private Trampoline launcher;
        [SerializeField] private PlayableDirector shot;
        [SerializeField] private Cinemachine.CinemachineVirtualCamera shotCamera;

        private bool played;

        public bool Played => played;

        public void Configure(Trampoline trigger, PlayableDirector director, Cinemachine.CinemachineVirtualCamera camera)
        {
            launcher = trigger;
            shot = director;
            shotCamera = camera;
        }

        private void OnEnable()
        {
            if (launcher != null)
                launcher.Launched += OnLaunched;
            GameEvents.OnRespawn += Rearm;
        }

        private void OnDisable()
        {
            if (launcher != null)
                launcher.Launched -= OnLaunched;
            GameEvents.OnRespawn -= Rearm;
        }

        // Una vez por intento: no se rearma si el checkpoint de reaparición queda después de la zona (no se vuelve a pasar por ella).
        private void Rearm()
        {
            if (played && RespawnIsPast(launcher != null ? launcher.transform.position : transform.position))
                return;
            played = false;
        }

        private static bool RespawnIsPast(Vector3 position)
        {
            var level = LevelManager.Current;
            var flow = FindFirstObjectByType<FlowSpline>();
            if (level == null || level.Raft == null || flow == null)
                return false;
            int hint = 0;
            float zone = flow.GetProgress(position, ref hint);
            hint = 0;
            float raft = flow.GetProgress(level.Raft.transform.position, ref hint);
            return raft > zone + 0.005f;
        }

        private void OnLaunched(RaftController raft)
        {
            if (played)
                return;
            played = true;
            var balance = raft.Balance;
            if (Services.IsReady && Services.TimeController != null)
                Services.TimeController.SlowMotion(balance.superFartSlowMoScale, balance.superFartSlowMoDuration);
            if (shotCamera != null)
            {
                shotCamera.Follow = raft.transform;
                shotCamera.LookAt = raft.transform;
            }
            if (shot != null)
            {
                shot.time = 0;
                shot.Play();
            }
        }
    }
}
