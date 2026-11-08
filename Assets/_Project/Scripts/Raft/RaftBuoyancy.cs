using System;
using GasSafari.Core;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Raft
{
    // Flotación por 4 puntos y arrastre agua/aire (docs/02 §1). Índices: 0 NE, 1 NO, 2 SE, 3 SO.
    [RequireComponent(typeof(Rigidbody))]
    public class RaftBuoyancy : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;
        [Tooltip("Posiciones locales de los puntos de flotación, una por cámara")]
        [SerializeField] private Vector3[] floatPoints = new Vector3[4];

        private float[] chamberFactors;
        private float[] depths;
        private Rigidbody rb;
        private bool hasState;

        public bool IsAirborne { get; private set; }
        public int SubmergedPoints { get; private set; }
        public int PointCount => floatPoints.Length;
        // Los fija RaftEffects (Galope Turbo, Panzazo).
        public float WaterDragMultiplier { get; set; } = 1f;
        public float AngularDragMultiplier { get; set; } = 1f;
        // Zonas del nivel (red de pesca H32): se multiplica con el de RaftEffects, no lo reemplaza.
        public float ZoneDragMultiplier { get; set; } = 1f;
        // Gaseosa: sube la superficie efectiva para que la balsa flote a esta altura sobre el agua (m).
        public float HoverOffset { get; set; }

        public event Action<bool> AirborneChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            chamberFactors = new float[floatPoints.Length];
            depths = new float[floatPoints.Length];
            for (int i = 0; i < chamberFactors.Length; i++)
                chamberFactors[i] = 1f;
        }

        public void SetChamberFactor(int index, float factor)
        {
            chamberFactors[index] = factor;
        }

        // Profundidad del punto en el último paso físico (negativa = fuera del agua; NaN = sin agua debajo).
        public float GetDepth(int index) => depths[index];

        public Vector3 GetFloatPoint(int index)
        {
            return transform.TransformPoint(floatPoints[index]);
        }

        private void FixedUpdate()
        {
            int submerged = 0;
            for (int i = 0; i < floatPoints.Length; i++)
            {
                var point = GetFloatPoint(i);
                if (!WaterSurface.TryGetHeight(point, out float waterHeight))
                {
                    depths[i] = float.NaN;
                    continue;
                }

                float depth = waterHeight + HoverOffset - point.y;
                depths[i] = depth;
                if (depth <= 0f)
                    continue;

                submerged++;
                float force = Mathf.Clamp01(depth / balance.floatDepthRange) * balance.buoyancyPerPoint * chamberFactors[i];
                rb.AddForceAtPosition(Vector3.up * force, point, ForceMode.Force);
            }

            SubmergedPoints = submerged;
            SetAirborne(submerged == 0);
            rb.drag = IsAirborne ? balance.airDrag : balance.waterDrag * WaterDragMultiplier * ZoneDragMultiplier;
            rb.angularDrag = (IsAirborne ? balance.airAngularDrag : balance.waterAngularDrag) * AngularDragMultiplier;
        }

        private void SetAirborne(bool airborne)
        {
            if (hasState && airborne == IsAirborne)
                return;

            hasState = true;
            IsAirborne = airborne;
            AirborneChanged?.Invoke(airborne);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < floatPoints.Length; i++)
                Gizmos.DrawWireSphere(transform.TransformPoint(floatPoints[i]), 0.15f);
        }
    }
}
