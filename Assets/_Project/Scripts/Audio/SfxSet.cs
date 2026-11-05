using UnityEngine;

namespace GasSafari.Audio
{
    // Sonidos propios de un personaje (docs/03 "Sonidos por personaje", docs/08 §4).
    [CreateAssetMenu(menuName = "Gas Safari/Sfx Set", fileName = "SfxSet")]
    public class SfxSet : ScriptableObject
    {
        [Header("Pedos (fart_<tipo>_<pj>)")]
        public SoundDef puff;
        [Tooltip("Loop mientras dura la ráfaga")] public SoundDef blast;
        [Tooltip("Loop mientras carga")] public SoundDef charge;
        public SoundDef mega;
        public SoundDef cough;

        [Header("Voces (voice_<pj>_<tipo>); las dispara CharacterVoice, reglas en docs/08 §2")]
        public SoundDef voiceEffort;
        public SoundDef voiceFall;
        public SoundDef voiceLaugh;
        public SoundDef voiceCheer;
    }
}
