using System;
using UnityEngine;

namespace GasSafari.Audio
{
    // Tabla SoundId -> SoundDef que usa el AudioDirector.
    [CreateAssetMenu(menuName = "Gas Safari/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SoundId id;
            public SoundDef sound;
        }

        public Entry[] entries;

        [NonSerialized] private SoundDef[] lookup;

        public SoundDef Get(SoundId id)
        {
            if (lookup == null)
                Build();
            int index = (int)id;
            return index < lookup.Length ? lookup[index] : null;
        }

        private void Build()
        {
            lookup = new SoundDef[Enum.GetValues(typeof(SoundId)).Length];
            foreach (var entry in entries)
                lookup[(int)entry.id] = entry.sound;
        }
    }
}
