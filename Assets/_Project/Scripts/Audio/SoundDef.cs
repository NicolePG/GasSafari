using System;
using UnityEngine;
using UnityEngine.Audio;

namespace GasSafari.Audio
{
    // Un sonido (docs/08 §2): clips, mezcla y variación.
    [CreateAssetMenu(menuName = "Gas Safari/Sound", fileName = "Sound")]
    public class SoundDef : ScriptableObject
    {
        public AudioClip[] clips;
        public AudioMixerGroup mixerGroup;
        [Range(0f, 1f)] public float volume = 1f;
        public float pitchMin = 0.9f;
        public float pitchMax = 1.15f;
        [Tooltip("Falso = 2D (interfaz)")] public bool spatial = true;
        public float maxDistance = 40f;
        [Tooltip("Mayor = más difícil de reemplazar cuando se acaban las fuentes")] public int priority;
        [Tooltip("Cuenta para el límite de pedos simultáneos")] public bool isFart;

        [NonSerialized] private int lastClip = -1;

        // Nunca el mismo clip dos veces seguidas.
        public AudioClip NextClip()
        {
            if (clips.Length == 1)
                return clips[0];
            int index = UnityEngine.Random.Range(0, clips.Length - 1);
            if (index >= lastClip && lastClip >= 0)
                index++;
            lastClip = index;
            return clips[index];
        }
    }
}
