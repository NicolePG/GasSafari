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

    // Voces sin palabras de un animal (docs/08 §2 "Voces"). Cada personaje respeta 3 s entre voces y no dice dos a la vez.
    //   effort: al soltar un pedo cargado, al lanzar el Súper Pedo y al subir a la balsa.
    //   fall:   siempre que cae al agua.
    //   laugh:  sync x3 o x4 del equipo, y 30 % de probabilidad cuando otro jugador cae al agua.
    //   cheer:  checkpoint, meta, primera vez de cada sinergia y logro desbloqueado.
    [RequireComponent(typeof(PlayerAvatar), typeof(AvatarStateMachine), typeof(FartThruster))]
    public class CharacterVoice : MonoBehaviour
    {
        public const float LAUGH_ON_FALL_CHANCE = 0.3f;
        private const int LAUGH_MIN_SYNC = 3;

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
            states.StateChanged += OnStateChanged;
            GameEvents.OnSuperFart += OnSuperFart;
            GameEvents.OnPlayerFell += OnPlayerFell;
            GameEvents.OnSync += OnSync;
            GameEvents.OnCheckpoint += OnCheckpoint;
            GameEvents.OnLevelFinished += OnLevelFinished;
            GameEvents.OnSynergy += OnSynergy;
            GameEvents.OnAchievementUnlocked += OnAchievement;
        }

        private void OnDisable()
        {
            thruster.Farted -= OnFarted;
            states.StateChanged -= OnStateChanged;
            GameEvents.OnSuperFart -= OnSuperFart;
            GameEvents.OnPlayerFell -= OnPlayerFell;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnCheckpoint -= OnCheckpoint;
            GameEvents.OnLevelFinished -= OnLevelFinished;
            GameEvents.OnSynergy -= OnSynergy;
            GameEvents.OnAchievementUnlocked -= OnAchievement;
        }

        // ---------------------------------------------------------------- disparadores
        private void OnFarted(FartType type, float impulse, float multiplier)
        {
            if (type == FartType.Charged)
                Speak(VoiceKind.Effort);
        }

        private void OnSuperFart(float power) => Speak(VoiceKind.Effort);

        private void OnStateChanged(AvatarState from, AvatarState to)
        {
            if (to == AvatarState.Climbing)
                Speak(VoiceKind.Effort);
        }

        private void OnPlayerFell(PlayerAvatar who)
        {
            if (who == avatar)
                Speak(VoiceKind.Fall, true);
            else if (Random01() < LAUGH_ON_FALL_CHANCE)
                Speak(VoiceKind.Laugh);
        }

        private void OnSync(int players, float multiplier)
        {
            if (players >= LAUGH_MIN_SYNC)
                Speak(VoiceKind.Laugh);
        }

        private void OnCheckpoint(int index) => Speak(VoiceKind.Cheer);

        private void OnLevelFinished(float time) => Speak(VoiceKind.Cheer);

        private void OnSynergy(string id, bool firstTime)
        {
            if (firstTime)
                Speak(VoiceKind.Cheer);
        }

        private void OnAchievement(string id) => Speak(VoiceKind.Cheer);

        // ---------------------------------------------------------------- reproducción
        public bool Speak(VoiceKind kind, bool force = false)
        {
            // Un animal en el agua solo grita (esfuerzo al subir, y la caída); no se ríe ni celebra desde ahí.
            var state = states.State;
            bool inWater = state == AvatarState.Falling || state == AvatarState.Swimming;
            if (!force && inWater && kind != VoiceKind.Effort)
                return false;

            var def = Pick(kind);
            if (def == null || !Services.IsReady)
                return false;
            bool speaking = handle != 0 && Services.Audio.IsPlaying(handle);
            if (!gate.TryStart(Clock(), speaking, force))
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
