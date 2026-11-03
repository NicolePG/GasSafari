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
        [Tooltip("Distancia máxima al borde para subir (docs/02 §9)")] public float climbReach;
        public float autoRespawnTime;
        [Tooltip("Gas al reaparecer solo tras caer (docs/02 §9)")] public float respawnGasFraction;
        public float respawnFade;

        [Header("Aire y pings")]
        public float airControlFactor;
        [Tooltip("Grados: aterrizaje torcido (docs/02 §15)")] public float landingTiltAngle;
        [Tooltip("PENDIENTE de diseño: impulso de rebote al aterrizar torcido (docs/02 §15 no da el valor)")]
        public float landingBounceImpulse;

        [Header("Cámara (docs/02 §12)")]
        [Tooltip("Grados")] public float cameraTilt;
        [Tooltip("Balsa sola")] public float cameraDistanceMin;
        [Tooltip("Alguien en el agua")] public float cameraDistanceMax;
        [Tooltip("Segundos de anticipación")] public float cameraLookahead;
        [Tooltip("Metros extra a máxima velocidad")] public float cameraSpeedZoom;
        [Tooltip("Provisional: velocidad que se toma como máxima (la del Súper Pedo)")] public float cameraZoomSpeedReference;
        [Tooltip("Provisional: suavizado del zoom en segundos")] public float cameraZoomSmoothTime;
        public float cameraRaftWeight;
        public float cameraSwimmerWeight;
        [Tooltip("Provisional: fuerza de sacudida media (cámara reventada)")] public float shakeMedium;
        [Tooltip("Provisional: fuerza de sacudida fuerte (Súper Pedo)")] public float shakeStrong;
        public float pingCooldown;
    }
}
