using System;
using UnityEngine;

namespace GasSafari.Core
{
    // Tabla maestra de docs/02 §14. Los valores viven en el asset, no en el código.
    [CreateAssetMenu(menuName = "Gas Safari/Balance Config", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Serializable]
        public struct SeatLayout
        {
            public float[] angles;
        }

        [Header("Balsa")]
        public float raftBaseMass;
        public float raftRadius;
        public float floatDepthRange;
        public float buoyancyPerPoint;
        public float poppedFactor;
        public float waterDrag;
        public float waterAngularDrag;
        public float airDrag;
        public float airAngularDrag;
        public Vector3 centerOfMassOffset;
        public float seatTiltFactor;
        [Tooltip("Grados")] public float capsizeAngle;
        public float capsizeTime;

        [Header("Pedos y gas")]
        public float puffMaxPressTime;
        public float puffCost;
        public float blastCostPerSecond;
        public float chargedCost;
        public float chargedImpulseMin;
        public float chargedImpulseMax;
        public float chargeTimeMin;
        public float chargeTimeMax;
        public float emptyCoughTime;

        [Header("Asientos y puntería")]
        [Tooltip("Grados")] public float maxAimAngle;
        [Tooltip("Grados por segundo")] public float seatMoveSpeed;
        [Tooltip("Grados")] public float minSeatSeparation;
        [Tooltip("Ángulos iniciales (docs/02 §2). Índice 0 = 1 jugador")] public SeatLayout[] startSeatAngles;

        [Header("Sincronía y equipo")]
        [Tooltip("Por defecto; la dificultad activa lo reemplaza")] public float syncWindow;
        [Tooltip("Índice = jugadores en el grupo")] public float[] syncMultipliers;
        [Tooltip("Índice = jugadores en el grupo")] public float[] syncMeterGain;
        public float syncBlastBonusTime;
        public float teamMeterMax;

        [Header("Súper Pedo")]
        public float superFartWindow;
        public float superFartForward;
        public float superFartUp;
        public float superFartSlowMoScale;
        public float superFartSlowMoDuration;

        [Header("Daño, caídas y reaparición")]
        public float chamberDamageImpulse;
        [Tooltip("Segundos que tarda en hundirse sin cámaras (docs/02 §8)")] public float sinkDuration;
        public float knockoffImpulse;
        public float swimSpeed;
        public float autoRespawnTime;
        public float respawnFade;

        [Header("Aire y pings")]
        public float airControlFactor;
        public float pingCooldown;
    }
}
