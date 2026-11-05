using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Bots
{
    public enum BotHintType : byte
    {
        Ramp,
        Drop,
        Whirlpool
    }

    // Marca que el nivel coloca delante de rampas y abismos (Marty galopa antes de ellos) y sobre remolinos (Gloria
    // hace Panzazo dentro de ellos), docs/10 §15. Los bots no tienen otra forma de saber qué hay en el terreno.
    public class BotHint : MonoBehaviour
    {
        private static readonly List<BotHint> All = new List<BotHint>();

        [SerializeField] private BotHintType type;
        [Tooltip("m: tamaño de la zona (remolino) o tolerancia lateral (rampa, abismo)")]
        [SerializeField] private float radius = 3f;

        public BotHintType Type => type;
        public float Radius => radius;

        public void Configure(BotHintType hintType, float hintRadius)
        {
            type = hintType;
            radius = hintRadius;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();
    }
}
