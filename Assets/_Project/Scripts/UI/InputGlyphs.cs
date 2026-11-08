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

    // Botón de cada acción por dispositivo (docs/02 §13, docs/07 §6): iconos de Art/UI/InputIcons/ (Kenney, CC0) y, como
    // respaldo y para lectores de pantalla, el nombre en texto.
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

        // Iconos Kenney de cada acción por dispositivo (docs/07 §6). Un elemento es el id de un sprite de InputIconSet
        // o un separador de texto ("+" para combinar botones, "/" para alternativas).
        public static string[] Icons(TutorialAction action, InputDeviceKind kind)
        {
            switch (action)
            {
                case TutorialAction.Fart:
                case TutorialAction.Blast:
                    return Pick(kind, new[] { "xbox_rt" }, new[] { "playstation_trigger_r2" }, new[] { "keyboard_space" });
                case TutorialAction.SeatMove:
                    return Pick(kind, new[] { "xbox_stick_l" }, new[] { "playstation_stick_l" }, new[] { "keyboard_a", "/", "keyboard_d" });
                case TutorialAction.Aim:
                    return Pick(kind, new[] { "xbox_stick_r" }, new[] { "playstation_stick_r" }, new[] { "keyboard_q", "/", "keyboard_e" });
                case TutorialAction.Charge:
                    return Pick(kind, new[] { "xbox_lt", "+", "xbox_rt" }, new[] { "playstation_trigger_l2", "+", "playstation_trigger_r2" },
                        new[] { "keyboard_shift", "+", "keyboard_space" });
                case TutorialAction.Grab:
                    return Pick(kind, new[] { "xbox_button_b" }, new[] { "playstation_button_circle" }, new[] { "keyboard_ctrl" });
                case TutorialAction.Ping:
                    return Pick(kind, new[] { "xbox_dpad_up" }, new[] { "playstation_dpad_up" }, new[] { "keyboard_1" });
                case TutorialAction.SuperFart:
                    return Pick(kind, new[] { "xbox_lb", "+", "xbox_rb" }, new[] { "playstation_trigger_l1", "+", "playstation_trigger_r1" },
                        new[] { "keyboard_r" });
                default:
                    return new string[0];
            }
        }

        private static T Pick<T>(InputDeviceKind kind, T xbox, T ps, T keyboard)
        {
            switch (kind)
            {
                case InputDeviceKind.PlayStation: return ps;
                case InputDeviceKind.Keyboard: return keyboard;
                default: return xbox;
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
