using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // Power-ups recogibles en escena. Cada power-up se registra al activarse (PowerUpBase);
    // lo consultan el salto de Julien y, más adelante, los bots.
    public static class PowerUpTargets
    {
        private static readonly List<Transform> Targets = new List<Transform>();

        public static int Count => Targets.Count;

        public static void Add(Transform target)
        {
            if (!Targets.Contains(target))
                Targets.Add(target);
        }

        public static void Remove(Transform target) => Targets.Remove(target);

        // El power-up más cercano a 'position' dentro de 'radius' (distancia horizontal).
        public static bool TryFindNear(Vector3 position, float radius, out Transform target)
        {
            target = null;
            float best = radius * radius;
            for (int i = 0; i < Targets.Count; i++)
            {
                var delta = Targets[i].position - position;
                delta.y = 0f;
                float d = delta.sqrMagnitude;
                if (d <= best)
                {
                    best = d;
                    target = Targets[i];
                }
            }
            return target != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Targets.Clear();
    }
}
