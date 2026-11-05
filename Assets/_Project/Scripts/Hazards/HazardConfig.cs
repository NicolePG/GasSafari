using System;
using GasSafari.Audio;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Configuración de un tipo de obstáculo (docs/05, docs/10 §9). Los valores por nivel se ajustan en el prefab.
    [CreateAssetMenu(menuName = "Gas Safari/Hazard", fileName = "H00_Hazard")]
    public class HazardConfig : ScriptableObject
    {
        // Todo peligro avisa al menos 0.5 s antes (docs/05).
        public const float MIN_WARNING_TIME = 0.5f;

        [Serializable]
        public struct Parameter
        {
            public string key;
            public float value;
        }

        [Tooltip("H03, H24...")] public string id;
        public string displayName;
        [Tooltip("Segundos de aviso antes de actuar; nunca menos de 0.5")] public float warningTime = MIN_WARNING_TIME;
        public SoundId warningSound;
        [Tooltip("Pinchos y mordidas revientan cámaras aunque la dificultad desactive el daño por impactos (docs/06 §3)")]
        public bool alwaysDamagesChambers;
        [Tooltip("Parámetros base de docs/05: ciclo, velocidad, empuje, radio...")] public Parameter[] parameters;

        public float Get(string key, float fallback = 0f)
        {
            if (parameters == null)
                return fallback;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].key == key)
                    return parameters[i].value;
            }
            return fallback;
        }

        private void OnValidate()
        {
            warningTime = Mathf.Max(warningTime, MIN_WARNING_TIME);
        }
    }
}
