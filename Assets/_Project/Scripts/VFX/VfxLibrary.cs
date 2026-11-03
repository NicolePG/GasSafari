using System;
using UnityEngine;

namespace GasSafari.VFX
{
    // Prefabs de efectos y cuántos precalentar en el pool.
    [CreateAssetMenu(menuName = "Gas Safari/VFX Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public VfxId id;
            public PooledVfx prefab;
            public int prewarm;
        }

        public Entry[] entries;
    }
}
