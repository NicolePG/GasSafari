using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Agua abierta sin paredes (docs/04 W2-1): fuera del carril (más de halfWidth del cauce) una corriente devuelve la
    // balsa hacia él, más fuerte cuanto más se aleja.
    [RequireComponent(typeof(FlowSpline))]
    public class CourseBounds : MonoBehaviour
    {
        [Tooltip("m desde el centro del cauce en que empieza a devolver")] [SerializeField] private float halfWidth = 9f;
        [Tooltip("m/s² por metro fuera del carril (provisional)")] [SerializeField] private float pullPerMeter = 0.6f;
        [Tooltip("m/s² máximos")] [SerializeField] private float maxPull = 4f;

        private FlowSpline flow;
        private int hint;

        public void Configure(float half, float perMeter, float max)
        {
            halfWidth = half;
            pullPerMeter = perMeter;
            maxPull = max;
        }

        private void Awake() => flow = GetComponent<FlowSpline>();

        private void FixedUpdate()
        {
            var manager = LevelManager.Current;
            var raft = manager != null ? manager.Raft : null;
            if (raft == null || raft.Buoyancy.IsAirborne)
                return;
            var position = raft.Body.worldCenterOfMass;
            var center = flow.GetPointAhead(position, ref hint, 0f);
            var outward = position - center;
            outward.y = 0f;
            float outside = outward.magnitude - halfWidth;
            if (outside <= 0f)
                return;
            float pull = Mathf.Min(maxPull, outside * pullPerMeter);
            raft.ApplyThrust(-outward.normalized * pull, position, ForceMode.Acceleration);
        }
    }
}
