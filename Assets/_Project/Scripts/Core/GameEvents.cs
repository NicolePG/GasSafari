using System;
using UnityEngine;

namespace GasSafari.Core
{
    // Eventos globales de docs/10 §10. Suscribirse en OnEnable y desuscribirse en OnDisable.
    public static class GameEvents
    {
        public static event Action<int> OnChamberPopped;
        public static event Action<int> OnChamberRepaired;
        public static event Action OnRaftSunk;

        public static void RaiseChamberPopped(int chamber) => OnChamberPopped?.Invoke(chamber);
        public static void RaiseChamberRepaired(int chamber) => OnChamberRepaired?.Invoke(chamber);
        public static void RaiseRaftSunk() => OnRaftSunk?.Invoke();

        // Al descargar un nivel.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnChamberPopped = null;
            OnChamberRepaired = null;
            OnRaftSunk = null;
        }
    }
}
