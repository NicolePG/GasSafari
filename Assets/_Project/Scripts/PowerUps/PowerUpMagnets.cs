using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // Imanes de power-ups (Corona Magnética de Julien). Los power-ups consultan el más cercano.
    public static class PowerUpMagnets
    {
        private struct Magnet
        {
            public Transform Target;
            public float Radius;
        }

        private static readonly List<Magnet> Magnets = new List<Magnet>();

        public static int Count => Magnets.Count;

        public static void Add(Transform target, float radius)
        {
            Remove(target);
            Magnets.Add(new Magnet { Target = target, Radius = radius });
        }

        public static void Remove(Transform target)
        {
            for (int i = Magnets.Count - 1; i >= 0; i--)
            {
                if (Magnets[i].Target == target)
                    Magnets.RemoveAt(i);
            }
        }

        // El imán más cercano que alcanza a 'position'.
        public static bool TryFind(Vector3 position, out Transform target)
        {
            target = null;
            float best = float.MaxValue;
            for (int i = 0; i < Magnets.Count; i++)
            {
                var m = Magnets[i];
                float d = (m.Target.position - position).sqrMagnitude;
                if (d <= m.Radius * m.Radius && d < best)
                {
                    best = d;
                    target = m.Target;
                }
            }
            return target != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Magnets.Clear();
    }
}
