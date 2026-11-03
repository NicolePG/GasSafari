using System;
using GasSafari.Characters;
using UnityEngine;

namespace GasSafari.Core
{
    // Eventos globales de docs/10 §10. Suscribirse en OnEnable y desuscribirse en OnDisable.
    public static class GameEvents
    {
        public static event Action<int> OnChamberPopped;
        public static event Action<int> OnChamberRepaired;
        public static event Action OnRaftSunk;
        public static event Action<PlayerAvatar> OnGasEmpty;
        public static event Action<PlayerAvatar, FartType, float> OnFart;
        public static event Action<int, float> OnSync;
        public static event Action<float> OnTeamMeterChanged;
        public static event Action<float> OnSuperFart;
        public static event Action<PlayerAvatar> OnPlayerFell;
        // rescuer es null cuando la balsa pasa por encima del caído.
        public static event Action<PlayerAvatar, PlayerAvatar> OnPlayerRescued;

        public static void RaiseFart(PlayerAvatar avatar, FartType type, float impulse) => OnFart?.Invoke(avatar, type, impulse);
        public static void RaiseSync(int players, float multiplier) => OnSync?.Invoke(players, multiplier);
        public static void RaiseTeamMeterChanged(float value) => OnTeamMeterChanged?.Invoke(value);
        public static void RaiseSuperFart(float power) => OnSuperFart?.Invoke(power);
        public static void RaisePlayerFell(PlayerAvatar avatar) => OnPlayerFell?.Invoke(avatar);
        public static void RaisePlayerRescued(PlayerAvatar rescued, PlayerAvatar rescuer) => OnPlayerRescued?.Invoke(rescued, rescuer);
        public static void RaiseChamberPopped(int chamber) => OnChamberPopped?.Invoke(chamber);
        public static void RaiseChamberRepaired(int chamber) => OnChamberRepaired?.Invoke(chamber);
        public static void RaiseRaftSunk() => OnRaftSunk?.Invoke();
        public static void RaiseGasEmpty(PlayerAvatar avatar) => OnGasEmpty?.Invoke(avatar);

        // Al descargar un nivel.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnChamberPopped = null;
            OnChamberRepaired = null;
            OnRaftSunk = null;
            OnGasEmpty = null;
            OnFart = null;
            OnSync = null;
            OnTeamMeterChanged = null;
            OnSuperFart = null;
            OnPlayerFell = null;
            OnPlayerRescued = null;
        }
    }
}
