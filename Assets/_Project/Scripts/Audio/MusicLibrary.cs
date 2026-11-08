using System;
using UnityEngine;

namespace GasSafari.Audio
{
    // Pistas de música (docs/08 §5). Cada pista de mundo tiene la capa base y, si pasó la prueba de sincronía, una de intensidad.
    // El id es el de LevelData.musicId / WorldData.musicId (mus_w3, mus_w3_boss, mus_title...).
    [CreateAssetMenu(menuName = "Gas Safari/Music Library", fileName = "MusicLibrary")]
    public class MusicLibrary : ScriptableObject
    {
        [Serializable]
        public class Track
        {
            public string id;
            public AudioClip baseClip;
            [Tooltip("Capa de intensidad: vacía si la pista no la tiene (solo suena la base)")] public AudioClip intensityClip;
            [Tooltip("Tiempos (s) de cada pulso de la pista: JSON con una lista de números")] public TextAsset beats;
        }

        [SerializeField] private Track[] tracks = Array.Empty<Track>();

        public Track[] Tracks => tracks;

        public Track Get(string id)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].id == id)
                    return tracks[i];
            }
            return null;
        }

        public void Set(Track[] value) => tracks = value;
    }
}
