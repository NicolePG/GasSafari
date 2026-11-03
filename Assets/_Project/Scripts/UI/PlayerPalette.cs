using UnityEngine;

namespace GasSafari.UI
{
    // Colores de jugador (aro, HUD, ping), independientes del personaje (docs/03 Reglas de selección).
    [CreateAssetMenu(menuName = "Gas Safari/Player Palette", fileName = "PlayerPalette")]
    public class PlayerPalette : ScriptableObject
    {
        [Tooltip("Índice = número de jugador (J1 = 0)")]
        public Color[] colors;

        public Color Get(int playerIndex)
        {
            return colors[playerIndex % colors.Length];
        }
    }
}
