using UnityEngine;

namespace GasSafari.Bots
{
    // Sincronía, habilidades, agarrarse, Súper Pedo y pings (M4-02). En M4-01 solo están los puntos de enganche.
    public partial class BotBrain
    {
        private void AwakeSkills() { }
        private void OnEnableSkills() { }
        private void OnDisableSkills() { }

        private void NavigateSkills(ref Input.PlayerCommand cmd, Vector3 position, Vector3 velocity, Vector3 heading) { }

        // Agarrado: sin motivo para seguirlo, se suelta.
        private void HoldOn(ref Input.PlayerCommand cmd)
        {
            cmd.GrabHeld = false;
            Idle(ref cmd);
        }
    }
}
