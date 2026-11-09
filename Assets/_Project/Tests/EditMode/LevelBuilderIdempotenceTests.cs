using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GasSafari.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GasSafari.Tests
{
    // Los constructores de nivel son idempotentes: correrlos dos veces deja la escena igual (todo lo que arman cuelga de
    // _Generated y se rehace; nunca se suma encima). Cada caso corre el constructor, huella la jerarquía, lo corre de nuevo
    // y compara. Reescribe la escena del nivel con el mismo contenido.
    public class LevelBuilderIdempotenceTests
    {
        public static IEnumerable<TestCaseData> Builders()
        {
            yield return new TestCaseData((Action)W1LevelsBuilder.BuildW1_1, "N01_W1-1_FuentesZoo").SetName("W1-1");
            yield return new TestCaseData((Action)W2LevelsBuilder.BuildW2_1, "N06_W2-1_CajasDeriva").SetName("W2-1 (agua abierta)");
            yield return new TestCaseData((Action)W3LevelsBuilder.BuildW3_3, "N13_W3-3_TerritorioFosas").SetName("W3-3");
            yield return new TestCaseData((Action)W4LevelsBuilder.BuildW4_5, "N20_W4-5_VolcanJefe").SetName("W4-5");
        }

        [TestCaseSource(nameof(Builders))]
        public void RunningBuilderTwice_LeavesTheSameScene(Action build, string scene)
        {
            string path = $"Assets/_Project/Scenes/Juego/Niveles/{scene}.unity";
            build();
            string first = Fingerprint(path, out int generatedFirst);
            build();
            string second = Fingerprint(path, out int generatedSecond);

            Assert.AreEqual(1, generatedFirst, "una sola raíz _Generated tras la primera corrida");
            Assert.AreEqual(1, generatedSecond, "una sola raíz _Generated tras la segunda corrida");
            Assert.IsEmpty(Diff(first, second), $"{scene}: la segunda corrida cambió la escena");
        }

        [Test]
        public void ManualObjects_OutsideGenerated_SurviveARebuild()
        {
            const string path = "Assets/_Project/Scenes/Juego/Niveles/N01_W1-1_FuentesZoo.unity";
            W1LevelsBuilder.BuildW1_1();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var manual = new GameObject("PruebaManual");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(manual, scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);

            W1LevelsBuilder.BuildW1_1();

            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.IsTrue(roots.Any(r => r.name == "PruebaManual"), "lo puesto a mano fuera de _Generated se conserva");
                UnityEngine.Object.DestroyImmediate(roots.First(r => r.name == "PruebaManual"));
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string Fingerprint(string path, out int generatedRoots)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var sb = new StringBuilder();
                var roots = scene.GetRootGameObjects();
                generatedRoots = roots.Count(r => r.name == GreyboxLevel.GENERATED_ROOT);
                foreach (var root in roots)
                    Walk(root.transform, "", sb);
                return sb.ToString();
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Walk(Transform t, string parent, StringBuilder sb)
        {
            string path = parent + "/" + t.name;
            var c = CultureInfo.InvariantCulture;
            var p = t.position;
            var e = t.eulerAngles;
            var s = t.lossyScale;
            string components = string.Join(",", t.GetComponents<Component>().Where(x => x != null).Select(x => x.GetType().Name));
            sb.AppendLine(string.Format(c, "{0}|{1}|L{2}|{3:F2},{4:F2},{5:F2}|{6:F0},{7:F0},{8:F0}|{9:F2},{10:F2},{11:F2}|{12}",
                path, t.gameObject.activeSelf ? 1 : 0, t.gameObject.layer, p.x, p.y, p.z, Mathf.Round(e.x) % 360, Mathf.Round(e.y) % 360,
                Mathf.Round(e.z) % 360, s.x, s.y, s.z, components));
            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), path, sb);
        }

        private static List<string> Diff(string a, string b)
        {
            var left = a.Split('\n');
            var right = b.Split('\n');
            var diff = new List<string>();
            if (left.Length != right.Length)
                diff.Add($"{left.Length} objetos antes, {right.Length} después");
            for (int i = 0; i < Mathf.Min(left.Length, right.Length) && diff.Count < 10; i++)
            {
                if (left[i] != right[i])
                    diff.Add($"[{i}] antes: {left[i].Trim()}  después: {right[i].Trim()}");
            }
            return diff;
        }
    }
}
