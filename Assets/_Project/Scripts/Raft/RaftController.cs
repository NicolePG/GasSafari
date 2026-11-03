using System.Collections.Generic;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Cuerpo físico de la balsa (docs/02 §1). Toda fuerza externa entra por ApplyThrust.
    [RequireComponent(typeof(Rigidbody))]
    public class RaftController : MonoBehaviour
    {
        private struct WeightPoint
        {
            public Transform Point;
            public float Weight;
        }

        [SerializeField] private BalanceConfig balance;

        private readonly List<WeightPoint> weights = new List<WeightPoint>();
        private Rigidbody rb;

        public Rigidbody Body => rb;
        public RaftBuoyancy Buoyancy { get; private set; }
        public BalanceConfig Balance => balance;
        public float OnboardWeight { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            Buoyancy = GetComponent<RaftBuoyancy>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.centerOfMass = balance.centerOfMassOffset;
            UpdateMass();
        }

        public void ApplyThrust(Vector3 force, Vector3 point, ForceMode mode)
        {
            rb.AddForceAtPosition(force, point, mode);
        }

        // Un animal a bordo: suma su peso a la masa e inclina la balsa hacia su asiento.
        public void AddWeight(Transform point, float weight)
        {
            weights.Add(new WeightPoint { Point = point, Weight = weight });
            OnboardWeight += weight;
            UpdateMass();
        }

        public void RemoveWeight(Transform point)
        {
            for (int i = weights.Count - 1; i >= 0; i--)
            {
                if (weights[i].Point != point)
                    continue;
                OnboardWeight -= weights[i].Weight;
                weights.RemoveAt(i);
            }
            UpdateMass();
        }

        private void FixedUpdate()
        {
            float tilt = Physics.gravity.magnitude * balance.seatTiltFactor;
            for (int i = 0; i < weights.Count; i++)
            {
                var w = weights[i];
                rb.AddForceAtPosition(Vector3.down * (w.Weight * tilt), w.Point.position, ForceMode.Force);
            }
        }

        private void UpdateMass()
        {
            rb.mass = balance.raftBaseMass + OnboardWeight;
        }
    }
}
