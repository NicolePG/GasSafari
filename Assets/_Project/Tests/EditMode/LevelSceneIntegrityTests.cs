using System.Linq;
using GasSafari.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GasSafari.Tests
{
    // Un constructor de nivel corrido sobre una escena que ya existe agrega el contenido encima (W3-5 llegó a tener 4 copias,
    // 12 mangos y 13 checkpoints). Cada nivel debe tener un solo grupo de cada tipo y 3 mangos dorados.
    public class LevelSceneIntegrityTests
    {
        private static readonly string[] Groups = { "Channel", "Sections", "Checkpoints", "Mangos", "Obstacles", "PowerUps" };

        [Test]
        public void EveryLevel_HasOneOfEachGroupAndThreeGoldenMangos()
        {
            var problems = new System.Collections.Generic.List<string>();
            foreach (var name in CollisionAudit.Scenes)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/Levels/{name}.unity", OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                // Los grupos cuelgan de _Generated (o están sueltos en escenas anteriores a esa raíz).
                var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach (var group in Groups)
                {
                    int count = all.Count(t => t.name == group && t.parent == null || t.name == group && t.parent != null && t.parent.name == GasSafari.Editor.GreyboxLevel.GENERATED_ROOT);
                    if (count > 1)
                        problems.Add($"{name}: {count} copias de {group}");
                }
                int mangos = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Count(t => t.name.StartsWith("MangoSpot_"));
                if (mangos != 3)
                    problems.Add($"{name}: {mangos} mangos dorados (deben ser 3)");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
