using System.Collections.Generic;
using System.Linq;
using GasSafari.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace GasSafari.Tests
{
    // docs/04: checkpoint cada 70-190 m de recorrido (unos 20-40 s). level-reviewer encontró tramos fuera de rango; los
    // corregidos quedan vigilados aquí. Excepciones: niveles largos cuyo número de checkpoints fija docs/04 (agregar
    // checkpoints cambia el doc, LevelData y LevelSeedTable: decisión de diseño pendiente). Un nivel sale de la lista al arreglarse.
    public class CheckpointSpacingTests
    {
        private const float MIN = 59f; // W1-2 queda a 60 m entre la entrada y el primer checkpoint (aceptado en la revisión)
        private const float MAX = 191f;
        private const float MAX_TO_FINISH = 210f; // el último tramo incluye la recta de llegada (W2-1 208 m, W2-5 205 m)
        private static readonly HashSet<string> PendingDesign = new HashSet<string> { "W1-1", "W1-3", "W1-4", "W1-5" };

        [Test]
        public void Checkpoints_AreBetween60And190Metres_ExceptPendingLongLevels()
        {
            var problems = new List<string>();
            foreach (var name in CollisionAudit.Scenes)
            {
                string id = System.Text.RegularExpressions.Regex.Match(name, @"N\d\d_(T0|W\d-\d)_").Groups[1].Value;
                if (id == "T0" || PendingDesign.Contains(id))
                    continue;
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/Juego/Niveles/{name}.unity", OpenSceneMode.Single);
                var container = Object.FindFirstObjectByType<SplineContainer>();
                if (container == null)
                    continue;
                float length = container.CalculateLength();
                var checkpoints = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Where(t => t.name.StartsWith("Checkpoint_")).OrderBy(t => t.name).ToList();
                var arcs = checkpoints.Select(c => ArcOf(container, c.position, length)).ToList();
                arcs.Add(length); // la meta
                for (int i = 1; i < arcs.Count; i++)
                {
                    float gap = arcs[i] - arcs[i - 1];
                    if (gap < MIN || gap > (i == arcs.Count - 1 ? MAX_TO_FINISH : MAX))
                        problems.Add($"{id}: tramo {i - 1}->{(i == arcs.Count - 1 ? "meta" : i.ToString())} = {gap:F0} m");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        private static float ArcOf(SplineContainer container, Vector3 point, float length)
        {
            float best = float.MaxValue, bestT = 0f;
            for (int i = 0; i <= 2000; i++)
            {
                float t = i / 2000f;
                float d = ((Vector3)container.EvaluatePosition(t) - point).sqrMagnitude;
                if (d < best) { best = d; bestT = t; }
            }
            return bestT * length;
        }
    }
}
