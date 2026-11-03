using UnityEngine;

namespace GasSafari.Core
{
    // Acceso estático a los servicios persistentes. Lo llena GameServices al arrancar.
    public static class Services
    {
        public static GameServices Root { get; private set; }
        public static GameSession Session { get; private set; }
        public static bool IsReady => Root != null;

        internal static void Bind(GameServices root)
        {
            Root = root;
            Session = root.GetComponentInChildren<GameSession>();
        }

        internal static void Unbind(GameServices root)
        {
            if (Root == root)
                ResetStatics();
        }

        // Por si se entra a Play sin recargar el dominio.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Root = null;
            Session = null;
        }
    }
}
