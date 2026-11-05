using GasSafari.Input;

namespace GasSafari.Bots
{
    // Fuente de comandos de un bot (docs/10 §8): BotBrain escribe el comando y el avatar lo lee como el de un humano.
    public class BotCommandSource : IPlayerCommandSource
    {
        private PlayerCommand current;

        public PlayerCommand Current => current;
        public ref PlayerCommand Command => ref current;

        public void PressFart() => current.FartPresses++;
        public void PressAbility() => current.AbilityPresses++;
        public void PressJump() => current.JumpPresses++;
    }
}
