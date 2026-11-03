using UnityEngine;

namespace GasSafari.Characters
{
    // Stats de un personaje (docs/03 Resumen). Habilidad, pasiva, prefab y sonidos llegan en M2.
    [CreateAssetMenu(menuName = "Gas Safari/Character", fileName = "Character")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string displayName;
        public float gasCapacity;
        public float puffImpulse;
        public float blastForce;
        public float regenPerSecond;
        public float regenDelay;
        [Tooltip("kg")] public float weight;
        [Tooltip("m")] public float height;
    }
}
