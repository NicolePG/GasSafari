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
        public static event Action OnRaftCapsized;
        public static event Action<PlayerAvatar> OnGasEmpty;
        public static event Action<PlayerAvatar, FartType, float> OnFart;
        public static event Action<int, float> OnSync;
        public static event Action<float> OnTeamMeterChanged;
        public static event Action<float> OnSuperFart;
        public static event Action<PlayerAvatar, Input.PingType> OnPing;
        public static event Action<PlayerAvatar, string> OnAbilityUsed;
        // firstTime: primera vez que este equipo la logra (nombre en pantalla y logro).
        public static event Action<string, bool> OnSynergy;
        public static event Action<int> OnCheckpoint;
        public static event Action OnRespawn;
        public static event Action<PlayerAvatar> OnPlayerFell;
        // rescuer es null cuando la balsa pasa por encima del caído.
        public static event Action<PlayerAvatar, PlayerAvatar> OnPlayerRescued;
        // Un peligro empieza su aviso previo (docs/05): lo usan el HUD, Melman y los bots.
        public static event Action<Hazards.HazardBase> OnHazardWarning;

        public static void RaiseFart(PlayerAvatar avatar, FartType type, float impulse) => OnFart?.Invoke(avatar, type, impulse);
        public static void RaiseSync(int players, float multiplier) => OnSync?.Invoke(players, multiplier);
        public static void RaiseTeamMeterChanged(float value) => OnTeamMeterChanged?.Invoke(value);
        public static void RaiseSuperFart(float power) => OnSuperFart?.Invoke(power);
        public static void RaisePing(PlayerAvatar avatar, Input.PingType type) => OnPing?.Invoke(avatar, type);
        public static void RaiseAbilityUsed(PlayerAvatar avatar, string id) => OnAbilityUsed?.Invoke(avatar, id);
        public static void RaiseSynergy(string id, bool firstTime) => OnSynergy?.Invoke(id, firstTime);
        public static void RaiseCheckpoint(int index) => OnCheckpoint?.Invoke(index);
        public static void RaiseRespawn() => OnRespawn?.Invoke();
        public static void RaisePlayerFell(PlayerAvatar avatar) => OnPlayerFell?.Invoke(avatar);
        public static void RaisePlayerRescued(PlayerAvatar rescued, PlayerAvatar rescuer) => OnPlayerRescued?.Invoke(rescued, rescuer);
        public static void RaiseHazardWarning(Hazards.HazardBase hazard) => OnHazardWarning?.Invoke(hazard);
        public static void RaiseChamberPopped(int chamber) => OnChamberPopped?.Invoke(chamber);
        public static void RaiseChamberRepaired(int chamber) => OnChamberRepaired?.Invoke(chamber);
        public static void RaiseRaftSunk() => OnRaftSunk?.Invoke();
        public static void RaiseRaftCapsized() => OnRaftCapsized?.Invoke();
        public static void RaiseGasEmpty(PlayerAvatar avatar) => OnGasEmpty?.Invoke(avatar);

        // Al descargar un nivel.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnChamberPopped = null;
            OnChamberRepaired = null;
            OnRaftSunk = null;
            OnRaftCapsized = null;
            OnGasEmpty = null;
            OnFart = null;
            OnSync = null;
            OnTeamMeterChanged = null;
            OnSuperFart = null;
            OnPing = null;
            OnAbilityUsed = null;
            OnSynergy = null;
            OnCheckpoint = null;
            OnRespawn = null;
            OnPlayerFell = null;
            OnPlayerRescued = null;
            OnHazardWarning = null;
        }
    }
}
