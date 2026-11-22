using UnityEngine;

namespace GasSafari.Modes
{
    // Valores de Contrarreloj, Espejo y Modificadores (docs/06 §1). Vive en Resources/ModesConfig.asset y lo llena
    // "Gas Safari/Setup/Crear modos de M7". Los marcados como provisionales están documentados en docs/06.
    [CreateAssetMenu(menuName = "Gas Safari/Modes Config", fileName = "ModesConfig")]
    public class ModesConfig : ScriptableObject
    {
        public const string RESOURCE_PATH = "ModesConfig";

        [Header("Contrarreloj")]
        [Tooltip("Muestras por segundo de la pose de la balsa (docs/06: 10 Hz)")] public float ghostSampleRate = 10f;
        [Tooltip("Mejores tiempos guardados por nivel (docs/06: 5)")] public int topTimes = 5;
        [Tooltip("Material transparente del fantasma")] public Material ghostMaterial;
        public Color ghostColor = new Color(0.55f, 0.9f, 1f, 0.35f);
        [Tooltip("Provisional: segundos que se ve la diferencia con el fantasma al pasar un checkpoint")] public float splitShowSeconds = 3f;

        [Header("Espejo")]
        [Tooltip("docs/06: frijolitos x1.5")] public float mirrorBeanMultiplier = 1.5f;
        [Tooltip("docs/06: mínimo dificultad Normal")] public string mirrorMinDifficulty = "Normal";

        [Header("Modificadores")]
        public ModifierData[] modifiers = new ModifierData[0];
        [Tooltip("Provisional: segundos de un vaivén completo de Cámara mareada")] public float dizzyPeriod = 4f;

        private static ModesConfig instance;

        public static ModesConfig Instance
        {
            get => instance != null ? instance : instance = Resources.Load<ModesConfig>(RESOURCE_PATH);
            set => instance = value;
        }

        public ModifierData FindModifier(string id)
        {
            for (int i = 0; i < modifiers.Length; i++)
            {
                if (modifiers[i] != null && modifiers[i].id == id)
                    return modifiers[i];
            }
            return null;
        }
    }
}
