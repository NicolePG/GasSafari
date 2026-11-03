using UnityEngine;

namespace GasSafari.Raft
{
    // Efectos que las habilidades y pasivas dejan sobre la balsa. Volver a aplicar uno temporal lo renueva.
    [RequireComponent(typeof(RaftBuoyancy))]
    public class RaftEffects : MonoBehaviour
    {
        private RaftBuoyancy buoyancy;
        private float waterDragTimer;
        private float angularDragTimer;
        private float whirlpoolImmunityTimer;

        public bool IsWaterDragScaled => waterDragTimer > 0f;
        public bool IsAngularDragScaled => angularDragTimer > 0f;
        // Panzazo: los remolinos no atraen a la balsa mientras dura.
        public bool IsWhirlpoolImmune => whirlpoolImmunityTimer > 0f;
        // Segundos extra de anticipación de los avisos de peligro (Vista Alta). Lo lee el aviso común (M3-05).
        public float WarningLeadBonus { get; set; }

        private void Awake()
        {
            buoyancy = GetComponent<RaftBuoyancy>();
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
            buoyancy.WaterDragMultiplier = 1f;
            buoyancy.AngularDragMultiplier = 1f;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (whirlpoolImmunityTimer > 0f)
                whirlpoolImmunityTimer -= dt;
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
