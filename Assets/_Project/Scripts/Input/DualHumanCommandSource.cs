using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Input
{
    // Modo solo con 2 animales (docs/02 §13): un solo mando produce los comandos de dos animales.
    //   Primero:  stick izquierdo (asiento y nado), LT (aguantar), RT (pedo), X (habilidad).
    //   Segundo:  stick derecho (asiento y nado), LB (aguantar), RB (pedo), Y (habilidad).
    // Provisional, no está en docs/02 §13: B agarra a los dos, A es el salto/subir de los dos, L3+R3 es el Súper Pedo
    // de los dos (LB+RB ya son del segundo animal), la cruceta son los pings del primero y no hay puntería.
    public class DualHumanCommandSource : MonoBehaviour
    {
        private const float PING_THRESHOLD = 0.5f;

        private sealed class Animal : IPlayerCommandSource
        {
            public PlayerCommand Command;
            public PlayerCommand Current => Command;
        }

        private readonly Animal first = new Animal();
        private readonly Animal second = new Animal();
        private Gamepad pad;

        public IPlayerCommandSource First => first;
        public IPlayerCommandSource Second => second;

        // Pruebas y dispositivos que no vienen de un PlayerInput.
        public void Bind(Gamepad gamepad)
        {
            pad = gamepad;
        }

        private void Update()
        {
            if (pad == null || !pad.added)
                pad = FindPad();
            if (pad == null)
                return;

            var left = pad.leftStick.ReadValue();
            var right = pad.rightStick.ReadValue();
            first.Command.SeatMove = left.x;
            second.Command.SeatMove = right.x;
            first.Command.Swim = left;
            second.Command.Swim = right;

            first.Command.HoldHeld = pad.leftTrigger.isPressed;
            first.Command.FartHeld = pad.rightTrigger.isPressed;
            second.Command.HoldHeld = pad.leftShoulder.isPressed;
            second.Command.FartHeld = pad.rightShoulder.isPressed;
            if (pad.rightTrigger.wasPressedThisFrame)
                first.Command.FartPresses++;
            if (pad.rightShoulder.wasPressedThisFrame)
                second.Command.FartPresses++;

            first.Command.AbilityHeld = pad.buttonWest.isPressed;
            second.Command.AbilityHeld = pad.buttonNorth.isPressed;
            if (pad.buttonWest.wasPressedThisFrame)
                first.Command.AbilityPresses++;
            if (pad.buttonNorth.wasPressedThisFrame)
                second.Command.AbilityPresses++;

            bool grab = pad.buttonEast.isPressed;
            bool super = pad.leftStickButton.isPressed && pad.rightStickButton.isPressed;
            first.Command.GrabHeld = second.Command.GrabHeld = grab;
            first.Command.SuperHeld = second.Command.SuperHeld = super;
            if (pad.buttonSouth.wasPressedThisFrame)
            {
                first.Command.JumpPresses++;
                second.Command.JumpPresses++;
            }

            first.Command.Ping = ToPing(pad.dpad.ReadValue());
        }

        // El mando que emparejó el PlayerInput de este jugador.
        private Gamepad FindPad()
        {
            var input = GetComponent<PlayerInput>();
            if (input == null)
                return null;
            foreach (var device in input.devices)
            {
                if (device is Gamepad gamepad)
                    return gamepad;
            }
            return null;
        }

        private static PingType ToPing(Vector2 value)
        {
            if (value.y > PING_THRESHOLD) return PingType.Now;
            if (value.y < -PING_THRESHOLD) return PingType.Wait;
            if (value.x < -PING_THRESHOLD) return PingType.TurnLeft;
            if (value.x > PING_THRESHOLD) return PingType.TurnRight;
            return PingType.None;
        }
    }
}
