using UnityEngine;

namespace GasSafari.Level
{
    // Acción de entrada que el cartel muestra con el icono del dispositivo de cada jugador (docs/07 §6 y §8).
    public enum TutorialAction : byte
    {
        Fart,
        SeatMove,
        Aim,
        Blast,
        Charge,
        Grab,
        Ping,
        SuperFart,
    }

    // Qué cumple una lección del T-0 (docs/04 T-0).
    public enum TutorialCondition : byte
    {
        Puff,           // 1) un toque
        MoveSeat,       // 2) mover el asiento 'amount' grados
        RoundBuoy,      // 3) pasar por los TutorialGate de la lección en orden
        Blast,          // 4) una ráfaga de 'amount' s
        Sync,           // 5) un grupo de sync (solo esta la cumple el equipo, no un jugador)
        PickupBeans,    // 6) recoger frijoles (P01)
        PingNow,        // 7) ping "¡Ahora!" y cuenta 3-2-1 (en solo basta con el ping)
        GrabInJet,      // 8) aguantar agarrado 'amount' s mientras el chorro empuja
        ChargedOnRamp,  // 9) pedo cargado de 'amount' s o más dentro de la zona de la rampa
        SuperFart,      // 10) Súper Pedo
    }

    // Una lección del tutorial (docs/07 §8).
    [CreateAssetMenu(menuName = "Gas Safari/Tutorial Step", fileName = "TutorialStep")]
    public class TutorialStep : ScriptableObject
    {
        [Tooltip("Base de las claves de la tabla de textos (tutorial.l01 → .title y .text); vacío = usa title y text")]
        public string textKey;
        public string title;
        [TextArea] public string text;
        public TutorialAction action;
        public TutorialCondition condition;
        [Tooltip("Grados (asiento) o segundos (ráfaga, agarre, carga), según la condición")] public float amount;
        [Tooltip("Lección 10: llena el medidor de equipo al 100 % al entrar en la zona")] public bool fillTeamMeter;
    }
}
