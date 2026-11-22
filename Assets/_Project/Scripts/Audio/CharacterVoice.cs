using System;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Audio
{
    public enum VoiceKind : byte
    {
        Effort,
        Fall,
        Laugh,
        Cheer
    }

    // Voces sin palabras de un animal (docs/08 §2 y §4, decisión provisional pendiente de playtest). Cada personaje respeta 3 s
    // entre voces y no dice dos a la vez; los clips varían como cualquier sonido de AudioDirector (pitch del SoundDef y nunca
    // el mismo clip dos veces seguidas).
    //   effort: al lanzar un pedo (toque, cargado, Súper Pedo) o empujar (empezar una ráfaga).
    //   fall:   al caer al agua o al hundirse la balsa (suena siempre y corta la voz anterior).
    //   laugh:  cuando otro animal cae al agua o falla un pedo (tose sin gas), con LAUGH_CHANCE para que no se ría todo el equipo.
    //   cheer:  en checkpoint, en cada sincronía y en la meta.
    // El esfuerzo es la voz más frecuente: risa y festejo lo cortan aunque no hayan pasado los 3 s (el pedo que completa una
    // sincronía hace esfuerzo y enseguida festeja).
    [RequireComponent(typeof(PlayerAvatar), typeof(AvatarStateMachine), typeof(FartThruster))]
    public class CharacterVoice : MonoBehaviour
    {
        public const float LAUGH_CHANCE = 0.3f;

        // Pruebas: sustituyen el azar y el reloj.
        public static Func<float> Random01 = () => UnityEngine.Random.value;
        public static Func<float> Clock = () => Time.unscaledTime;

        private readonly VoiceGate gate = new VoiceGate();
        private PlayerAvatar avatar;
        private AvatarStateMachine states;
        private FartThruster thruster;
        private int handle;

        public VoiceKind? LastSpoken { get; private set; }

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            states = GetComponent<AvatarStateMachine>();
            thruster = GetComponent<FartThruster>();
        }

        private void OnEnable()
        {
            thruster.Farted += OnFarted;
            GameEvents.OnSuperFart += OnSuperFart;
            GameEvents.OnPlayerFell += OnPlayerFell;
            GameEvents.OnFartFailed += OnFartFailed;
            GameEvents.OnRaftSunk += OnRaftSunk;
            GameEvents.OnSync += OnSync;
            GameEvents.OnCheckpoint += OnCheckpoint;
            GameEvents.OnLevelFinished += OnLevelFinished;
        }

        private void OnDisable()
        {
            thruster.Farted -= OnFarted;
            GameEvents.OnSuperFart -= OnSuperFart;
            GameEvents.OnPlayerFell -= OnPlayerFell;
            GameEvents.OnFartFailed -= OnFartFailed;
            GameEvents.OnRaftSunk -= OnRaftSunk;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnCheckpoint -= OnCheckpoint;
            GameEvents.OnLevelFinished -= OnLevelFinished;
        }

        // ---------------------------------------------------------------- disparadores
        // Toque, cargado y comienzo de ráfaga (empujar).
        private void OnFarted(FartType type, float impulse, float multiplier) => Speak(VoiceKind.Effort);

        private void OnSuperFart(float power) => Speak(VoiceKind.Effort);

        private void OnPlayerFell(PlayerAvatar who)
        {
            if (who == avatar)
                Speak(VoiceKind.Fall, true);
            else
                MaybeLaugh();
        }

        private void OnFartFailed(PlayerAvatar who)
        {
            if (who != avatar)
                MaybeLaugh();
        }

        // Solo los que seguían a bordo (los que están en el agua ya gritaron al caer).
        private void OnRaftSunk()
        {
            if (!InWater)
                Speak(VoiceKind.Fall, true);
        }

        private bool InWater => states.State == AvatarState.Falling || states.State == AvatarState.Swimming;

        private void OnSync(int players, float multiplier) => Speak(VoiceKind.Cheer);

        private void OnCheckpoint(int index) => Speak(VoiceKind.Cheer);

        private void OnLevelFinished(float time) => Speak(VoiceKind.Cheer);

        private void MaybeLaugh()
        {
            if (Random01() < LAUGH_CHANCE)
                Speak(VoiceKind.Laugh);
        }

        // ---------------------------------------------------------------- reproducción
        public bool Speak(VoiceKind kind, bool force = false)
        {
            // Un animal en el agua solo grita (la caída); no se esfuerza, ríe ni celebra desde ahí.
            if (!force && InWater)
                return false;

            var def = Pick(kind);
            if (def == null || !Services.IsReady)
                return false;
            bool speaking = handle != 0 && Services.Audio.IsPlaying(handle);
            bool overEffort = LastSpoken == VoiceKind.Effort && kind != VoiceKind.Effort;
            if (!gate.TryStart(Clock(), speaking, force || overEffort))
                return false;

            if (speaking)
                Services.Audio.Stop(handle);
            handle = Services.Audio.Play(def, transform.position, 0f, transform, false);
            LastSpoken = kind;
            return true;
        }

        private SoundDef Pick(VoiceKind kind)
        {
            var sfx = avatar.Character != null ? avatar.Character.sfxSet : null;
            if (sfx == null)
                return null;
            switch (kind)
            {
                case VoiceKind.Effort: return sfx.voiceEffort;
                case VoiceKind.Fall: return sfx.voiceFall;
                case VoiceKind.Laugh: return sfx.voiceLaugh;
                default: return sfx.voiceCheer;
            }
        }
    }
}
