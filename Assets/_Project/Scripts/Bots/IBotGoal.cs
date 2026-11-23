using GasSafari.Input;
using UnityEngine;

namespace GasSafari.Bots
{
    // Objetivo de los bots fuera del cauce (minijuegos de Fiesta, docs/06 §1): el minijuego dice hacia dónde llevar la balsa
    // o maneja el comando entero. Sin objetivo, los bots siguen el cauce como siempre.
    public interface IBotGoal
    {
        // Rumbo horizontal deseado para la balsa de este bot; false = el del cauce.
        bool TryGetHeading(BotBrain bot, Vector3 raftPosition, out Vector3 heading);

        // Escribe el comando completo de un bot sentado (minijuegos sin navegación); false = navegación normal.
        bool Drive(BotBrain bot, ref PlayerCommand command);
    }
}
