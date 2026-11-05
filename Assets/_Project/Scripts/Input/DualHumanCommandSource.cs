using System;
using GasSafari.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Input
{
    // Control doble (docs/02 §13): un mando produce los comandos de dos animales, solo con mando (no con teclado).
    //   Animal A = mitad izquierda: stick izquierdo (asiento y nado), LT pedo, LB aguantar, X habilidad, L3 salto de Julien.
    //   Animal B = mitad derecha:   stick derecho,   RT pedo, RB aguantar, Y habilidad, R3 salto de Julien.
    //   Puntería fija en 0 para los dos (se gira disparando uno u otro).
    //   B/Círculo (mantener): se agarran los dos. A/Cruz: si alguno está nadando, sube a la balsa; si no, es el
    //   Súper Pedo y cuenta como pulsado por los dos animales. Cruceta: pings (un solo ping, con el color del jugador).
    public class DualHumanCommandSource : MonoBehaviour
    {
        private const float PING_THRESHOLD = 0.5f;

        private sealed class Animal : IPlayerCommandSource, IAvatarBoundSource
        {
            public PlayerCommand Command;
            public Func<bool> InWater = () => false;
            public PlayerCommand Current => Command;

            public void Bind(PlayerAvatar avatar)
            {
                var states = avatar.GetComponent<AvatarStateMachine>();
                InWater = () => states != null && states.State == AvatarState.Swimming;
            }
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

        // Pruebas: simula que un animal está nadando sin armar una balsa.
        public void SetInWaterCheck(bool forFirst, Func<bool> inWater)
        {
            (forFirst ? first : second).InWater = inWater;
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
            first.Command.Aim = second.Command.Aim = 0f;

            first.Command.FartHeld = pad.leftTrigger.isPressed;
            second.Command.FartHeld = pad.rightTrigger.isPressed;
            first.Command.HoldHeld = pad.leftShoulder.isPressed;
            second.Command.HoldHeld = pad.rightShoulder.isPressed;
            if (pad.leftTrigger.wasPressedThisFrame)
                first.Command.FartPresses++;
            if (pad.rightTrigger.wasPressedThisFrame)
                second.Command.FartPresses++;

            first.Command.AbilityHeld = pad.buttonWest.isPressed;
            second.Command.AbilityHeld = pad.buttonNorth.isPressed;
            if (pad.buttonWest.wasPressedThisFrame)
                first.Command.AbilityPresses++;
            if (pad.buttonNorth.wasPressedThisFrame)
                second.Command.AbilityPresses++;

            // Salto de Julien: L3 para A, R3 para B.
            if (pad.leftStickButton.wasPressedThisFrame)
                first.Command.JumpPresses++;
            if (pad.rightStickButton.wasPressedThisFrame)
                second.Command.JumpPresses++;

            first.Command.GrabHeld = second.Command.GrabHeld = pad.buttonEast.isPressed;

            // A/Cruz: sube al que está en el agua; si nadie nada, es el Súper Pedo de los dos.
            bool firstSwims = first.InWater();
            bool secondSwims = second.InWater();
            bool anySwims = firstSwims || secondSwims;
            if (pad.buttonSouth.wasPressedThisFrame && anySwims)
            {
                if (firstSwims)
                    first.Command.JumpPresses++;
                if (secondSwims)
                    second.Command.JumpPresses++;
            }
            first.Command.SuperHeld = second.Command.SuperHeld = pad.buttonSouth.isPressed && !anySwims;

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
