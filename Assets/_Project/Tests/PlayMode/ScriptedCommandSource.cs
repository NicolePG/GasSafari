using GasSafari.Input;
using UnityEngine;

namespace GasSafari.Tests
{
    // Fuente de comandos controlada por la prueba.
    public class ScriptedCommandSource : MonoBehaviour, IPlayerCommandSource
    {
        public PlayerCommand Command;

        public PlayerCommand Current => Command;
    }
}
