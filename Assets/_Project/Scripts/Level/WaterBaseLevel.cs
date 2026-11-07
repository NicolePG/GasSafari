using UnityEngine;

namespace GasSafari.Level
{
    // Nivel base del agua de este nivel (docs/02 §1): la altura que vale fuera de las mallas de agua de cada tramo.
    public class WaterBaseLevel : MonoBehaviour
    {
        [Tooltip("Altura del agua fuera de los tramos (m, mundo)")]
        [SerializeField] private float height;

        public float Height => height;

        public void Configure(float baseHeight)
        {
            height = baseHeight;
            if (isActiveAndEnabled)
                WaterSurface.SetBaseLevel(height);
        }

        private void OnEnable() => WaterSurface.SetBaseLevel(height);

        private void OnDisable()
        {
            if (WaterSurface.HasBaseLevel && Mathf.Approximately(WaterSurface.BaseLevel, height))
                WaterSurface.ClearBaseLevel();
        }
    }
}
