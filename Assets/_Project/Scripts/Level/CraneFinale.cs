using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Raft;
using UnityEngine;
using UnityEngine.Playables;

namespace GasSafari.Level
{
    // Momento estrella de W1-5 (docs/04): la balsa sube por la rampa del barco y el gancho cae detrás destrozando un
    // contenedor. Al entrar en la rampa: golpe dirigido al contenedor, cámara lenta del Súper Pedo y toma de Timeline.
    [RequireComponent(typeof(Collider))]
    public class CraneFinale : MonoBehaviour
    {
        [SerializeField] private CraneBoss crane;
        [SerializeField] private Transform container;
        [SerializeField] private PlayableDirector shot;
        [SerializeField] private Cinemachine.CinemachineVirtualCamera shotCamera;

        private bool played;

        public bool Played => played;

        public void Configure(CraneBoss boss, Transform target, PlayableDirector director, Cinemachine.CinemachineVirtualCamera camera)
        {
            crane = boss;
            container = target;
            shot = director;
            shotCamera = camera;
        }

        private void OnEnable() => GameEvents.OnRespawn += Rearm;
        private void OnDisable() => GameEvents.OnRespawn -= Rearm;

        private void Rearm() => played = false;

        private void OnTriggerEnter(Collider other)
        {
            if (played || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent<RaftController>(out var raft))
                return;
            played = true;
            if (crane != null && container != null)
                crane.StrikeAt(container.position);
            if (Services.IsReady && Services.TimeController != null)
                Services.TimeController.SlowMotion(raft.Balance.superFartSlowMoScale, raft.Balance.superFartSlowMoDuration);
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
