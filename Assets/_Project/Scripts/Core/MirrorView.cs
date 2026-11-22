using GasSafari.Input;
using UnityEngine;

namespace GasSafari.Core
{
    // Modo Espejo (docs/06 §1): la imagen del nivel se ve invertida en X y la entrada de los humanos se invierte igual, así que
    // se juega el nivel espejado. El mundo (física, splines, peligros, checkpoints y bots) no cambia: las piezas del escenario
    // van en static batching y una escala -1 en tiempo de ejecución no llega a sus mallas combinadas.
    // Todo lo que pasa de mundo a pantalla o de mando a mundo pregunta aquí.
    public static class MirrorView
    {
        public static bool Active { get; set; }

        // Punto de viewport (0-1) tal como se ve en pantalla.
        public static Vector3 Viewport(Vector3 view)
        {
            if (Active)
                view.x = 1f - view.x;
            return view;
        }

        // Comando de un humano: lo que ve a la izquierda es la derecha del mundo.
        public static PlayerCommand Apply(PlayerCommand command)
        {
            if (!Active)
                return command;
            command.SeatMove = -command.SeatMove;
            command.Aim = -command.Aim;
            command.Swim.x = -command.Swim.x;
            command.Ping = Swap(command.Ping);
            return command;
        }

        // Los pings "gira a la izquierda/derecha" se guardan en sentido del mundo y se muestran en sentido de la pantalla.
        public static PingType Swap(PingType ping)
        {
            if (!Active)
                return ping;
            if (ping == PingType.TurnLeft)
                return PingType.TurnRight;
            if (ping == PingType.TurnRight)
                return PingType.TurnLeft;
            return ping;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active = false;
    }
}
