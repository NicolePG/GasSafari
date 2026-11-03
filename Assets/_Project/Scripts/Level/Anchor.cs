using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Level
{
    // Punto enganchable por el Cuello Gancho de Melman (docs/10 §5).
    public class Anchor : MonoBehaviour
    {
        private static readonly List<Anchor> All = new List<Anchor>();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public static Anchor FindNearest(Vector3 from, float range)
        {
            Anchor best = null;
            float bestDistance = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                float d = (All[i].transform.position - from).sqrMagnitude;
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = All[i];
                }
            }
            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();
    }
}
