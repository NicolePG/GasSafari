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

        [Header("Habilidad")]
        public SoundDef ability;

        [Header("Voces (voice_<pj>_<tipo>). Aún sin disparadores: docs/08 no dice cuándo suena cada una")]
        public SoundDef voiceEffort;
        public SoundDef voiceFall;
        public SoundDef voiceLaugh;
        public SoundDef voiceCheer;
    }
}
