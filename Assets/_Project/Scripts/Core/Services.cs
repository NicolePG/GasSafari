using UnityEngine;

namespace GasSafari.Core
{
    // Acceso estático a los servicios persistentes. Lo llena GameServices al arrancar.
    public static class Services
    {
        public static GameServices Root { get; private set; }
        public static GameSession Session { get; private set; }
        public static TimeController TimeController { get; private set; }
        public static Input.InputHub InputHub { get; private set; }
        public static Audio.AudioDirector Audio { get; private set; }
        public static VFX.VfxPool Vfx { get; private set; }
        public static Progression.ProgressionService Progression { get; private set; }
        public static bool IsReady => Root != null;

        internal static void Bind(GameServices root)
        {
            Root = root;
            Session = root.GetComponentInChildren<GameSession>();
            TimeController = root.GetComponentInChildren<TimeController>();
            InputHub = root.GetComponentInChildren<Input.InputHub>();
            Audio = root.GetComponentInChildren<Audio.AudioDirector>();
            Vfx = root.GetComponentInChildren<VFX.VfxPool>();
            Progression = root.GetComponentInChildren<Progression.ProgressionService>();
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
            TimeController = null;
            InputHub = null;
            Audio = null;
            Vfx = null;
            Progression = null;
        }
    }
}
