using System;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Flotación por 4 puntos y arrastre agua/aire (docs/02 §1). Índices: 0 NE, 1 NO, 2 SE, 3 SO.
    [RequireComponent(typeof(Rigidbody))]
    public class RaftBuoyancy : MonoBehaviour
    {
        // Distancia sobre cada punto desde la que se busca la superficie del agua.
        private const float WATER_PROBE_HEIGHT = 10f;

        [SerializeField] private BalanceConfig balance;
        [Tooltip("Posiciones locales de los puntos de flotación, una por cámara")]
        [SerializeField] private Vector3[] floatPoints = new Vector3[4];

        private float[] chamberFactors;
        private Rigidbody rb;
        private bool hasState;

        public bool IsAirborne { get; private set; }
        public int SubmergedPoints { get; private set; }
        public int PointCount => floatPoints.Length;
        // Los fija RaftEffects (Galope Turbo, Panzazo).
        public float WaterDragMultiplier { get; set; } = 1f;
        public float AngularDragMultiplier { get; set; } = 1f;

        public event Action<bool> AirborneChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            chamberFactors = new float[floatPoints.Length];
            for (int i = 0; i < chamberFactors.Length; i++)
                chamberFactors[i] = 1f;
        }

        public void SetChamberFactor(int index, float factor)
        {
            chamberFactors[index] = factor;
        }

        public Vector3 GetFloatPoint(int index)
        {
            return transform.TransformPoint(floatPoints[index]);
        }

        public static bool TryGetWaterHeight(Vector3 point, out float height)
        {
            var origin = point + Vector3.up * WATER_PROBE_HEIGHT;
            if (Physics.Raycast(origin, Vector3.down, out var hit, WATER_PROBE_HEIGHT * 2f,
                    GameLayers.WaterMask, QueryTriggerInteraction.Collide))
            {
                height = hit.point.y;
                return true;
            }
            height = 0f;
            return false;
        }

        private void FixedUpdate()
        {
            int submerged = 0;
            for (int i = 0; i < floatPoints.Length; i++)
            {
                var point = GetFloatPoint(i);
                if (!TryGetWaterHeight(point, out float waterHeight))
                    continue;

                float depth = waterHeight - point.y;
                if (depth <= 0f)
                    continue;

                submerged++;
                float force = Mathf.Clamp01(depth / balance.floatDepthRange) * balance.buoyancyPerPoint * chamberFactors[i];
                rb.AddForceAtPosition(Vector3.up * force, point, ForceMode.Force);
            }

            SubmergedPoints = submerged;
            SetAirborne(submerged == 0);
            rb.drag = IsAirborne ? balance.airDrag : balance.waterDrag * WaterDragMultiplier;
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
