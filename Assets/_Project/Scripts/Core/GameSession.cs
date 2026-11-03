using UnityEngine;

namespace GasSafari.Core
{
    // Estado de la partida en curso (docs/10 §4). Se irá completando con modo, nivel y jugadores.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private DifficultyData difficulty;

        public DifficultyData Difficulty
        {
            get => difficulty;
            set => difficulty = value;
        }
    }
}
