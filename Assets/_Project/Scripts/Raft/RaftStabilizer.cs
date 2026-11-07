using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Estabilidad a velocidad (docs/02 §1 y §14): par de enderezado suave sobre rightingStartAngle,
    // proporcional a la inclinación que lo supera, y límite de velocidad horizontal en el agua.
    // Ninguno actúa en el aire: los saltos y aterrizajes se resuelven solos (docs/02 §15).
    [RequireComponent(typeof(RaftController), typeof(RaftBuoyancy))]
    public class RaftStabilizer : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private RaftController raft;
        private RaftBuoyancy buoyancy;

        // Aceleración angular aplicada en el último paso (rad/s², mundo). Solo telemetría.
        public Vector3 LastTorque { get; private set; }

        private void Awake()
        {
            raft = GetComponent<RaftController>();
            buoyancy = GetComponent<RaftBuoyancy>();
        }

        private void FixedUpdate()
        {
            LastTorque = Vector3.zero;
            if (buoyancy.IsAirborne)
                return;

            var body = raft.Body;
            ApplyRighting(body);
            LimitSpeed(body);
        }

        private void ApplyRighting(Rigidbody body)
        {
            var up = transform.up;
            float tilt = Vector3.Angle(up, Vector3.up);
            float excess = tilt - balance.rightingStartAngle;
            if (excess <= 0f)
                return;

            var axis = Vector3.Cross(up, Vector3.up);
            if (axis.sqrMagnitude < 1e-6f)
                return;

            // Acceleration: no depende de la masa ni de la inercia (cambia con los animales a bordo).
            var torque = axis.normalized * (excess * Mathf.Deg2Rad * balance.rightingStrength);
            body.AddTorque(torque, ForceMode.Acceleration);
            LastTorque = torque;
        }

        private void LimitSpeed(Rigidbody body)
        {
            var velocity = body.velocity;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            float max = balance.maxWaterSpeed;
            if (max <= 0f || flat.sqrMagnitude <= max * max)
                return;

            flat = flat.normalized * max;
            body.velocity = new Vector3(flat.x, velocity.y, flat.z);
        }
    }
}
