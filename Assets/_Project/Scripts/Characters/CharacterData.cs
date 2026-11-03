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

        [Header("Presentación")]
        [Tooltip("Provisional: avatar genérico hasta los prefabs finales (M2-13)")]
        public GameObject prefab;
        public AnimatorOverrideController overrideController;
        public Sprite icon;
        [Tooltip("PENDIENTE de diseño: los docs no definen un color de UI por personaje")]
        public Color uiColor = Color.white;
    }
}
