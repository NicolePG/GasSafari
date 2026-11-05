using UnityEngine;

namespace GasSafari.Bots
{
    // Ajustes de los bots (docs/10 §15, skill bot-ai). Los valores marcados "docs" salen de los documentos;
    // el resto es provisional y se afina con QA-10.
    [CreateAssetMenu(menuName = "Gas Safari/Bot Config", fileName = "BotConfig")]
    public class BotConfig : ScriptableObject
    {
        [Header("Decisión y percepción")]
        [Tooltip("s entre decisiones (docs)")] public float decideInterval = 0.1f;
        [Tooltip("s de avance sobre el cauce para el punto objetivo (docs)")] public float lookaheadSeconds = 2f;
        [Tooltip("m, límites del avance del punto objetivo")] public float lookaheadMin = 6f;
        public float lookaheadMax = 30f;
        [Tooltip("m del SphereCast hacia adelante (docs)")] public float hazardRange = 8f;
        [Tooltip("m de radio del SphereCast")] public float hazardRadius = 1.5f;
        [Tooltip("Grados que se desvía el rumbo para esquivar un obstáculo")] public float avoidAngle = 35f;

        [Header("Navegación")]
        [Tooltip("m/s que busca mantener sobre el cauce")] public float cruiseSpeed = 7f;
        [Tooltip("Grados de error de rumbo desde los que corrige con ráfaga")] public float bigHeadingError = 25f;
        [Tooltip("Cuánto de la velocidad lateral intenta anular al elegir la dirección del empuje")] public float lateralDamping = 0.35f;
        [Tooltip("Grados entre el rumbo del cauce ahora y 4 s adelante: por debajo es recta larga")] public float straightAngle = 12f;
        [Tooltip("Grados de separación entre bots vecinos al repartirse los asientos traseros")] public float seatSpread = 50f;
        [Tooltip("Grados de error de asiento a partir de los que aún no dispara")] public float seatTolerance = 25f;
        [Tooltip("Puntería (-1..1) por rad/s de giro de la balsa, para frenarlo")] public float yawDamping = 0.6f;

        [Header("Pedos y gas")]
        [Tooltip("Grados de inclinación de la balsa a partir de los que nadie dispara (vuelco); vuelve a disparar bajo la mitad")] public float maxTilt = 20f;
        [Tooltip("Gas mínimo para empezar a disparar (docs: 20)")] public float minGasToFire = 20f;
        [Tooltip("Fracción de gas que pide para iniciar una ráfaga en recta")] public float blastStartGas = 0.5f;
        [Tooltip("Fracción de gas a la que suelta la ráfaga")] public float blastStopGas = 0.25f;
        [Tooltip("s entre toques de corrección")] public float puffCadenceMin = 0.5f;
        public float puffCadenceMax = 0.8f;
        [Tooltip("s sin disparar con el equipo detenido (docs: 4)")] public float idleLimit = 4f;
        [Tooltip("m/s por debajo de los que el equipo cuenta como detenido")] public float stoppedSpeed = 1f;

        [Header("Sincronía (docs)")]
        public float syncDelayMin = 0.05f;
        public float syncDelayMax = 0.15f;
        [Tooltip("s sin disparar tras un ping Espera (docs: 1.5)")] public float waitPingTime = 1.5f;

        [Header("Agarrarse y nadar")]
        [Tooltip("s que se mantiene agarrado tras el aviso o golpe (docs: 0.5)")] public float releaseAfter = 0.5f;
        [Tooltip("m de la balsa a los que un bot nadando intenta subir")] public float climbDistance = 1.5f;
    }
}
