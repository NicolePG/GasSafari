using GasSafari.Level;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace GasSafari.UI
{
    public enum InputDeviceKind : byte
    {
        Xbox,
        PlayStation,
        Keyboard,
    }

    // Nombre del botón de cada acción por dispositivo (docs/02 §13, docs/07 §6). Provisional: texto en lugar de los
    // sprites de Art/UI/InputIcons/, que aún no existen.
    public static class InputGlyphs
    {
        public static InputDeviceKind KindOf(PlayerInput input)
        {
            if (input == null)
                return InputDeviceKind.Keyboard;
            var devices = input.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is DualShockGamepad)
                    return InputDeviceKind.PlayStation;
                if (devices[i] is Gamepad)
                    return InputDeviceKind.Xbox;
            }
            return InputDeviceKind.Keyboard;
        }

        public static string Label(TutorialAction action, InputDeviceKind kind)
        {
            switch (action)
            {
                case TutorialAction.Fart:
                case TutorialAction.Blast:
                    return Pick(kind, "RT", "R2", "Espacio");
                case TutorialAction.SeatMove: return Pick(kind, "Stick izq.", "Stick izq.", "A / D");
                case TutorialAction.Aim: return Pick(kind, "Stick der.", "Stick der.", "Q / E");
                case TutorialAction.Charge: return Pick(kind, "LT + RT", "L2 + R2", "Shift + Espacio");
                case TutorialAction.Grab: return Pick(kind, "B", "Círculo", "Ctrl");
                case TutorialAction.Ping: return Pick(kind, "Cruceta ↑", "Cruceta ↑", "1");
                case TutorialAction.SuperFart: return Pick(kind, "LB + RB", "L1 + R1", "R");
                default: return string.Empty;
            }
        }

        private static string Pick(InputDeviceKind kind, string xbox, string ps, string keyboard)
        {
            switch (kind)
            {
                case InputDeviceKind.PlayStation: return ps;
                case InputDeviceKind.Keyboard: return keyboard;
                default: return xbox;
            }
        }
    }
}
