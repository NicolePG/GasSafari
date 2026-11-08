using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Derrumbe que persigue (docs/04 W4-5): al entrar la balsa en la zona de salida empieza a avanzar por 'path' a 'speed'
    // m/s; si alcanza la balsa (a menos de 'catchDistance' m por detrás), reaparición en el checkpoint.
    [RequireComponent(typeof(BoxCollider))]
    public class CollapseChaser : MonoBehaviour
    {
        [SerializeField] private Transform rubble;
        [SerializeField] private Vector3[] path;
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float catchDistance = 4f;

        private RaftController raft;
        private int index;
        private bool running;

        public bool Running => running;

        public void Configure(Transform wall, Vector3[] waypoints, float metersPerSecond)
        {
            rubble = wall;
            path = waypoints;
            speed = metersPerSecond;
        }

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable() => GameEvents.OnRespawn -= Reset;

        private void Reset()
        {
            running = false;
            index = 0;
            if (rubble != null && path != null && path.Length > 0)
                rubble.position = path[0];
        }

        private void OnTriggerEnter(Collider other)
        {
            var hit = other.GetComponentInParent<RaftController>();
            if (hit == null || running)
                return;
            raft = hit;
            running = true;
            // Aviso: sacudida fuerte de cámara al arrancar el derrumbe.
            var rig = FindFirstObjectByType<GasSafari.VFX.RaftCameraRig>();
            if (rig != null)
                rig.Shake(1f);
        }

        private void Update()
        {
            if (!running || rubble == null || path == null || index >= path.Length - 1)
                return;
            var target = path[index + 1];
            rubble.position = Vector3.MoveTowards(rubble.position, target, speed * Time.deltaTime);
            if ((rubble.position - target).sqrMagnitude < 0.01f)
                index++;
            if (raft != null && Vector3.Distance(rubble.position, raft.transform.position) <= catchDistance)
            {
                running = false;
                var checkpoints = FindFirstObjectByType<CheckpointSystem>();
                if (checkpoints != null)
                    checkpoints.RequestRespawn();
            }
        }
    }
}
