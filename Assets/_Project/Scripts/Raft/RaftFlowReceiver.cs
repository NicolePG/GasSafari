using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Raft
{
    // Corriente y viento sobre la balsa con ForceMode.Acceleration (docs/02 §1).
    [RequireComponent(typeof(RaftController))]
    public class RaftFlowReceiver : MonoBehaviour
    {
        [SerializeField] private FlowSpline flow;

        private RaftController raft;
        private int flowHint;

        // Modificadores (Turbo corriente).
        public float CurrentMultiplier { get; set; } = 1f;
        // Peso Pesado de Gloria: reduce la componente lateral de corriente y viento.
        public float LateralFactor { get; set; } = 1f;
        // Lo fija GasWindZone cada paso físico, en m/s².
        public Vector3 Wind { get; set; }
        public Vector3 CurrentDirection { get; private set; }
        public float CurrentAcceleration { get; private set; }

        private void Awake()
        {
            raft = GetComponent<RaftController>();
        }

        private void Start()
        {
            if (flow == null)
                flow = FindFirstObjectByType<FlowSpline>();
        }

        public void SetFlow(FlowSpline newFlow)
        {
            flow = newFlow;
            flowHint = 0;
        }

        private void FixedUpdate()
        {
            var body = raft.Body;
            var acceleration = Wind;

            if (flow != null && !raft.Buoyancy.IsAirborne)
            {
                flow.GetFlow(body.worldCenterOfMass, ref flowHint, out var direction, out float current);
                CurrentDirection = direction;
                CurrentAcceleration = current * CurrentMultiplier;
                acceleration += direction * CurrentAcceleration;
            }

            if (acceleration == Vector3.zero)
                return;

            acceleration = ReduceLateral(acceleration, raft.transform.forward, LateralFactor);
            raft.ApplyThrust(acceleration, body.worldCenterOfMass, ForceMode.Acceleration);
        }

        // Escala la componente perpendicular al rumbo (plano XZ).
        public static Vector3 ReduceLateral(Vector3 acceleration, Vector3 heading, float lateralFactor)
        {
            heading.y = 0f;
            if (lateralFactor >= 1f || heading.sqrMagnitude < 1e-6f)
                return acceleration;

            heading.Normalize();
            var along = heading * Vector3.Dot(acceleration, heading);
            var lateral = acceleration - along;
            lateral.y = 0f;
            return along + lateral * lateralFactor + Vector3.up * acceleration.y;
        }
    }
}
