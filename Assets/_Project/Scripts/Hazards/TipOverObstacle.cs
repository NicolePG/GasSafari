using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Puesto que se vuelca al agua al pasar la balsa (W1-4, "H07 caen como obstáculo"): la balsa entra en la zona,
    // el puesto avisa (se tambalea warningTime) y suelta su caja H07 hacia el centro del cauce. Vuelve al reaparecer.
    public class TipOverObstacle : HazardBase
    {
        [Tooltip("Caja H07 (con Rigidbody cinemático) que cae al agua")] [SerializeField] private Rigidbody load;
        [Tooltip("Impulso por kg hacia el cauce (transform.forward)")] [SerializeField] private float throwSpeed = 3f;

        private Vector3 loadPosition;
        private Quaternion loadRotation;
        private bool tipped;

        public bool Tipped => tipped;

        public void Configure(Rigidbody fallingLoad, float speed)
        {
            load = fallingLoad;
            throwSpeed = speed;
        }

        private void Awake()
        {
            if (load != null)
            {
                loadPosition = load.transform.position;
                loadRotation = load.transform.rotation;
            }
        }

        private void OnEnable() => GameEvents.OnRespawn += ResetLoad;
        private void OnDisable() => GameEvents.OnRespawn -= ResetLoad;

        protected override void OnRaftEnter(RaftController raft)
        {
            if (!tipped && !IsWarning)
                Warn();
        }

        protected override void OnWarn()
        {
            if (load != null)
                load.transform.rotation = loadRotation * Quaternion.Euler(0f, 0f, 8f); // se tambalea
        }

        protected override void OnWarningEnded()
        {
            if (tipped || load == null)
                return;
            tipped = true;
            load.isKinematic = false;
            load.AddForce((transform.forward + Vector3.up * 0.5f) * throwSpeed, ForceMode.VelocityChange);
        }

        public void ResetLoad()
        {
            tipped = false;
            CancelWarning();
            if (load == null)
                return;
            load.isKinematic = true;
            load.transform.SetPositionAndRotation(loadPosition, loadRotation);
            if (load.TryGetComponent<BreakableCrate>(out var crate))
                crate.Restore();
        }
    }
}
