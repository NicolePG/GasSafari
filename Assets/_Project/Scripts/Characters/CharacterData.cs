using UnityEngine;

namespace GasSafari.Characters
{
    // Un personaje (docs/03 Resumen, docs/10 §9).
    [CreateAssetMenu(menuName = "Gas Safari/Character", fileName = "Character")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Header("Stats")]
        public float gasCapacity;
        public float puffImpulse;
        public float blastForce;
        public float regenPerSecond;
        public float regenDelay;
        [Tooltip("kg")] public float weight;
        [Tooltip("m")] public float height;

        [Header("Habilidad")]
        [Tooltip("La pasiva es un componente PassiveBase en el prefab del personaje")]
        public Abilities.AbilityBase ability;

        [Header("Presentación")]
        public GameObject prefab;
        public AnimatorOverrideController overrideController;
        public Audio.SfxSet sfxSet;
        public Sprite icon;
        [Tooltip("Color de UI del personaje (docs/03)")]
        public Color uiColor = Color.white;
    }
}
