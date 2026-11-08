using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;
using UnityEngine.Playables;

namespace GasSafari.Level
{
    // Momento estrella por zona (docs/04): cuando la balsa entra en la zona, una toma de Timeline que la sigue
    // (y, si se pide, la cámara lenta del Súper Pedo). Una vez por intento: se rearma al reaparecer.
    // Para los momentos que no los dispara un trampolín (surfear la ola, escapar del remolino, el pasillo del jefe...).
    [RequireComponent(typeof(BoxCollider))]
    public class StarMomentZone : MonoBehaviour
    {
        [SerializeField] private PlayableDirector shot;
        [SerializeField] private Cinemachine.CinemachineVirtualCamera shotCamera;
        [SerializeField] private bool slowMotion;
        [Tooltip("La zona está en la ruta principal: cualquier balsa que termine el nivel la cruza (las pruebas lo comprueban)")]
        [SerializeField] private bool onMainPath = true;

        private bool played;

        public bool Played => played;
        public bool OnMainPath => onMainPath;

        public void Configure(PlayableDirector director, Cinemachine.CinemachineVirtualCamera camera, bool withSlowMotion, bool mainPath)
        {
            shot = director;
            shotCamera = camera;
            slowMotion = withSlowMotion;
            onMainPath = mainPath;
        }

        private void OnEnable() => GameEvents.OnRespawn += Rearm;
        private void OnDisable() => GameEvents.OnRespawn -= Rearm;

        // Una vez por intento: no se rearma si el checkpoint de reaparición queda después de la zona (no se vuelve a pasar por ella).
        private void Rearm()
        {
            if (played && RespawnIsPast(transform.position))
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

        private void OnTriggerEnter(Collider other)
        {
            if (played)
                return;
            var raft = other.GetComponentInParent<RaftController>();
            if (raft == null)
                return;

            played = true;
            if (slowMotion && Services.IsReady && Services.TimeController != null)
            {
                var balance = raft.Balance;
                Services.TimeController.SlowMotion(balance.superFartSlowMoScale, balance.superFartSlowMoDuration);
            }
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
