using System;
using UnityEngine;

namespace GasSafari.UI
{
    // Las siete viñetas del juego por momento (Intro, fin de los mundos 1 a 5 y la secreta del mundo 6).
    [CreateAssetMenu(menuName = "Gas Safari/Story Library", fileName = "StoryLibrary")]
    public class StoryLibrary : ScriptableObject
    {
        public StoryVignetteData[] vignettes;

        public StoryVignetteData Get(string moment)
        {
            if (vignettes == null || string.IsNullOrEmpty(moment))
                return null;
            foreach (var vignette in vignettes)
            {
                if (vignette != null && string.Equals(vignette.id, moment, StringComparison.OrdinalIgnoreCase))
                    return vignette;
            }
            return null;
        }
    }

    // Cuándo sale cada viñeta (docs/01): la intro al empezar la Historia, la de cada mundo al terminar su jefe (nivel 5) y la
    // del mundo 6 al desbloquearlo. Quien maneja el mapa de mundos (M6) pregunta aquí qué viñeta toca.
    public static class StoryMoments
    {
        public const string Intro = "Intro";
        public const string World6Secret = "W6_Secret";

        // Viñeta que sale al terminar un nivel; null si ese nivel no cierra un mundo (W6-5 cierra con los créditos).
        public static string AfterLevel(string levelId)
        {
            switch (levelId)
            {
                case "W1-5": return "W1_End";
                case "W2-5": return "W2_End";
                case "W3-5": return "W3_End";
                case "W4-5": return "W4_End";
                case "W5-5": return "W5_End";
                default: return null;
            }
        }

        // Viñeta que sale al desbloquear un mundo; solo el secreto la tiene.
        public static string OnWorldUnlocked(int world) => world == 6 ? World6Secret : null;
    }
}
