using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Level
{
    // Altura del agua (docs/02 §1, docs/10 §5). Cada tramo tiene su malla de agua en la capa Water (solo consultas);
    // en toboganes, espirales y cascadas la malla sigue la rampa. Fuera de un tramo se usa el nivel base del nivel,
    // si el nivel lo fija con WaterBaseLevel (los niveles con abismos no lo fijan: debajo no hay agua).
    public static class WaterSurface
    {
        // Distancia sobre el punto desde la que se busca la superficie: en una espiral se toma la vuelta más cercana.
        private const float PROBE_UP = 10f;
        private const float PROBE_TOP = 1000f;

        public static bool HasBaseLevel { get; private set; }
        public static float BaseLevel { get; private set; }

        public static void SetBaseLevel(float height)
        {
            BaseLevel = height;
            HasBaseLevel = true;
        }

        public static void ClearBaseLevel()
        {
            HasBaseLevel = false;
        }

        // Superficie más alta en (x, z); NaN si no hay agua ni nivel base.
        public static float GetHeight(float x, float z)
        {
            if (Probe(new Vector3(x, PROBE_TOP, z), PROBE_TOP * 2f, out float height))
                return height;
            return HasBaseLevel ? BaseLevel : float.NaN;
        }

        // Superficie bajo (o hasta PROBE_UP sobre) un punto: la que usan la flotación y el nado.
        public static bool TryGetHeight(Vector3 point, out float height)
        {
            if (Probe(point + Vector3.up * PROBE_UP, PROBE_UP * 2f, out height))
                return true;
            height = BaseLevel;
            return HasBaseLevel;
        }

        private static bool Probe(Vector3 origin, float distance, out float height)
        {
            if (Physics.Raycast(origin, Vector3.down, out var hit, distance, GameLayers.WaterMask, QueryTriggerInteraction.Collide))
            {
                height = hit.point.y;
                return true;
            }
            height = 0f;
            return false;
        }
    }
}
