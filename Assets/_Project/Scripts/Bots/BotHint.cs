using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Bots
{
    public enum BotHintType : byte
    {
        Ramp,
        Drop,
        Whirlpool,
        // Despegue de un abismo que exige el Súper Pedo (H22 con H36 antes): los bots lo lanzan aquí aunque no haya humanos.
        SuperFart,
        // Ruta alternativa (W5-3): una lista de puntos que los bots siguen solo si la ruta forzada (BotHint.ForcedRoute) es la suya.
        Route
    }

    // Marca que el nivel coloca delante de rampas y abismos (Marty galopa antes de ellos) y sobre remolinos (Gloria
    // hace Panzazo dentro de ellos), docs/10 §15. La marca SuperFart es la única ocasión en que los bots lanzan el Súper Pedo sin un humano. Los bots no tienen otra forma de saber qué hay en el terreno.
    public class BotHint : MonoBehaviour
    {
        private static readonly List<BotHint> All = new List<BotHint>();

        [SerializeField] private BotHintType type;
        [Tooltip("m: tamaño de la zona (remolino) o tolerancia lateral (rampa, abismo)")]
        [SerializeField] private float radius = 3f;

        [SerializeField] private string routeId;
        [Tooltip("Route: puntos del mundo, en orden, que recorre el bot (el primero es la entrada)")]
        [SerializeField] private Vector3[] waypoints;

        // Ruta que los bots toman donde haya una marca Route con ese id; null = siguen el cauce principal (lo normal en el juego).
        // Las pruebas de nivel la usan para forzar cada ruta alternativa y comprobar que todas se pueden terminar.
        public static string ForcedRoute;

        public BotHintType Type => type;
        public float Radius => radius;
        public string RouteId => routeId;
        public Vector3[] Waypoints => waypoints;

        public void Configure(BotHintType hintType, float hintRadius)
        {
            type = hintType;
            radius = hintRadius;
        }

        public void ConfigureRoute(string id, Vector3[] points)
        {
            routeId = id;
            waypoints = points;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        // ¿Hay una marca de este tipo en 'range' metros por delante de 'from'?
        public static bool IsAhead(Vector3 from, Vector3 forward, float range, BotHintType hintType)
        {
            forward.y = 0f;
            forward.Normalize();
            for (int i = 0; i < All.Count; i++)
            {
                var hint = All[i];
                if (hint.type != hintType)
                    continue;
                var to = hint.transform.position - from;
                to.y = 0f;
                float along = Vector3.Dot(to, forward);
                if (along < 0f || along > range)
                    continue;
                if ((to - forward * along).magnitude <= hint.radius + 2f)
                    return true;
            }
            return false;
        }

        // La marca de este tipo en 'range' metros por delante de 'from' (o bajo el punto), si hay; null si no.
        public static BotHint FindAheadOrInside(Vector3 from, Vector3 forward, float range, BotHintType hintType)
        {
            forward.y = 0f;
            forward.Normalize();
            for (int i = 0; i < All.Count; i++)
            {
                var hint = All[i];
                if (hint.type != hintType)
                    continue;
                var to = hint.transform.position - from;
                to.y = 0f;
                float along = Vector3.Dot(to, forward);
                if (to.magnitude <= hint.radius || (along >= 0f && along <= range && (to - forward * along).magnitude <= hint.radius + 2f))
                    return hint;
            }
            return null;
        }

        // ¿Está el punto dentro de una marca de este tipo?
        public static bool IsInside(Vector3 point, BotHintType hintType)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var hint = All[i];
                if (hint.type != hintType)
                    continue;
                var to = hint.transform.position - point;
                to.y = 0f;
                if (to.magnitude <= hint.radius)
                    return true;
            }
            return false;
        }

        // Sigue la ruta forzada: si 'current' es null busca una marca Route de la ruta cuya entrada esté a 'enterRange' m;
        // devuelve true con el punto al que apuntar. Al pasar el último punto (o si la balsa reaparece lejos) la suelta.
        public static bool FollowRoute(Vector3 position, float enterRange, ref BotHint current, ref int index, out Vector3 target)
        {
            target = default;
            if (ForcedRoute == null)
            {
                current = null;
                return false;
            }
            if (current == null)
            {
                for (int i = 0; i < All.Count; i++)
                {
                    var hint = All[i];
                    if (hint.type != BotHintType.Route || hint.routeId != ForcedRoute || hint.waypoints == null || hint.waypoints.Length == 0)
                        continue;
                    var to = hint.waypoints[0] - position;
                    to.y = 0f;
                    if (to.magnitude > enterRange)
                        continue;
                    current = hint;
                    index = 0;
                    break;
                }
                if (current == null)
                    return false;
            }

            var points = current.waypoints;
            while (index < points.Length)
            {
                var to = points[index] - position;
                to.y = 0f;
                if (to.magnitude > 60f)
                {
                    current = null;       // reapareció en otro sitio
                    return false;
                }
                bool last = index == points.Length - 1;
                if (to.magnitude > (last ? 5f : 4f))
                    break;
                index++;
            }
            if (index >= points.Length)
            {
                current = null;
                return false;
            }
            target = points[index];
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            All.Clear();
            ForcedRoute = null;
        }
    }
}
