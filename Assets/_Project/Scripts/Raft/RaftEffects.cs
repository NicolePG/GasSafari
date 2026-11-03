using UnityEngine;

namespace GasSafari.Raft
{
    // Efectos que las habilidades y pasivas dejan sobre la balsa. Volver a aplicar uno temporal lo renueva.
    [RequireComponent(typeof(RaftBuoyancy))]
    public class RaftEffects : MonoBehaviour
    {
        private RaftBuoyancy buoyancy;
        private RaftController raft;
        private float waterDragTimer;
        private float angularDragTimer;
        private float whirlpoolImmunityTimer;
        private float fartBoostTimer;
        private float tiltSuppressTimer;

        public bool IsWaterDragScaled => waterDragTimer > 0f;
        public bool IsAngularDragScaled => angularDragTimer > 0f;
        // Panzazo: los remolinos no atraen a la balsa mientras dura.
        public bool IsWhirlpoolImmune => whirlpoolImmunityTimer > 0f;
        // Segundos extra de anticipación de los avisos de peligro (Vista Alta). Lo lee el aviso común (M3-05).
        public float WarningLeadBonus { get; set; }
        // Dúo Pesado: multiplica la fuerza de todos los pedos de la balsa mientras dura.
        public float FartForceMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            buoyancy = GetComponent<RaftBuoyancy>();
            raft = GetComponent<RaftController>();
        }

        public void BoostFartForce(float multiplier, float duration)
        {
            FartForceMultiplier = multiplier;
            fartBoostTimer = duration;
        }

        // Dúo Pesado: el peso de los animales no inclina la balsa mientras dura.
        public void SuppressWeightTilt(float duration)
        {
            raft.WeightTiltScale = 0f;
            tiltSuppressTimer = duration;
        }

        public void ScaleWaterDrag(float multiplier, float duration)
        {
            buoyancy.WaterDragMultiplier = multiplier;
            waterDragTimer = duration;
        }

        public void ScaleAngularDrag(float multiplier, float duration)
        {
            buoyancy.AngularDragMultiplier = multiplier;
            angularDragTimer = duration;
        }

        public void GrantWhirlpoolImmunity(float duration)
        {
            whirlpoolImmunityTimer = duration;
        }

        // Al reaparecer en un checkpoint.
        public void ClearAll()
        {
            waterDragTimer = 0f;
            angularDragTimer = 0f;
            whirlpoolImmunityTimer = 0f;
            fartBoostTimer = 0f;
            tiltSuppressTimer = 0f;
            buoyancy.WaterDragMultiplier = 1f;
            buoyancy.AngularDragMultiplier = 1f;
            FartForceMultiplier = 1f;
            raft.WeightTiltScale = 1f;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (whirlpoolImmunityTimer > 0f)
                whirlpoolImmunityTimer -= dt;
            if (fartBoostTimer > 0f)
            {
                fartBoostTimer -= dt;
                if (fartBoostTimer <= 0f)
                    FartForceMultiplier = 1f;
            }
            if (tiltSuppressTimer > 0f)
            {
                tiltSuppressTimer -= dt;
                if (tiltSuppressTimer <= 0f)
                    raft.WeightTiltScale = 1f;
            }
            if (waterDragTimer > 0f)
            {
                waterDragTimer -= dt;
                if (waterDragTimer <= 0f)
                    buoyancy.WaterDragMultiplier = 1f;
            }
            if (angularDragTimer > 0f)
            {
                angularDragTimer -= dt;
                if (angularDragTimer <= 0f)
                    buoyancy.AngularDragMultiplier = 1f;
            }
        }
    }
}
