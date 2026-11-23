using UnityEngine;

namespace GasSafari.Modes
{
    // Un minijuego de Fiesta (docs/06 §1). Los crea "Gas Safari/Setup/Crear minijuegos de Fiesta (M7)" en Data/Party.
    // Los valores marcados como provisionales están documentados en docs/06.
    [CreateAssetMenu(menuName = "Gas Safari/Party Game", fileName = "Party_")]
    public class PartyGameData : ScriptableObject
    {
        [Tooltip("goal, king, fishing, bubble, coconut: claves party.<id>.name/desc")] public string id;
        [Tooltip("Escena en Scenes/Juego/Fiesta (docs/10 §3)")] public string sceneName;
        public int order;
        [Tooltip("Ya tiene escena y reglas; si no, la ficha dice Próximamente")] public bool implemented;
        public string icon = "Icon_Mode_M5_Fiesta";

        [Header("Participantes (docs/06: 2-4, los bots completan)")]
        public int minPlayers = 2;
        public int maxPlayers = 4;
        [Tooltip("Balsa-Gol: 1 vs 1 o 2 vs 2, nunca un número impar")] public bool evenPlayers;

        [Header("Reglas")]
        [Tooltip("Segundos de juego (0 = sin límite). Balsa-Gol 180, Pesca de Mangos 90")] public float durationSeconds;
        [Tooltip("Puntos que terminan el minijuego (Balsa-Gol: 5 goles; 0 = sin límite)")] public int targetScore;
        [Tooltip("Rondas ganadas que dan la victoria (Rey de la Balsa: mejor de 3 = 2)")] public int roundsToWin;
        [Tooltip("Provisional: máximo de rondas si hay empates (Rey de la Balsa)")] public int maxRounds;
        [Tooltip("Provisional: segundos máximos de una ronda; al terminar la ronda queda sin ganador")] public float roundSeconds;

        [Header("Balsa-Gol (provisional salvo lo indicado)")]
        [Tooltip("Radio de la pelota de playa gigante (m)")] public float ballRadius = 1.4f;
        public float ballMass = 2f;
        [Tooltip("Segundos entre un gol y el saque")] public float goalPauseSeconds = 2.5f;
        [Tooltip("Saque neutral: si la pelota se mueve menos de ballStallDistance en estos segundos, vuelve al centro")] public float ballStallSeconds = 5f;
        [Tooltip("Metros en horizontal que debe moverse la pelota en ballStallSeconds (flotando nunca está del todo quieta)")] public float ballStallDistance = 1.5f;

        [Header("Rey de la Balsa")]
        [Tooltip("docs/06: cada pedo empuja a los animales a menos de 2.5 m")] public float pushRadius = 2.5f;
        [Tooltip("Provisional: velocidad horizontal con que sale despedido el empujado (m/s)")] public float pushSpeed = 3.5f;
        [Tooltip("Provisional: giro de la balsa (grados por segundo)")] public float spinDegreesPerSecond = 30f;

        [Header("Pesca de Mangos (provisional)")]
        [Tooltip("Mangos flotando a la vez")] public int mangosAtOnce = 8;
        [Tooltip("Segundos hasta que aparece otro mango tras recoger uno")] public float mangoRespawnSeconds = 1.5f;
        [Tooltip("Skill game-modes: mini balsa de radio 1 m y 1 asiento")] public float miniRaftRadius = 1f;

        public string NameKey => "party." + id + ".name";
        public string DescKey => "party." + id + ".desc";
    }
}
