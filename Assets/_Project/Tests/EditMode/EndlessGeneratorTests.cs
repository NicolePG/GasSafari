using System.Collections.Generic;
using System.Linq;
using GasSafari.Modes;
using GasSafari.Progression;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // M7-09 Infinito (docs/06 §1): tramos de 30-60 m de los niveles de M5, por mundo y dificultad 1-5; el planificador sube
    // la dificultad cada 200 m, no repite el tramo anterior y mantiene el rumbo; la misma semilla da el mismo río.
    public class EndlessGeneratorTests
    {
        private static EndlessConfig Config()
        {
            var config = EndlessConfig.Instance;
            Assert.IsNotNull(config, "Resources/EndlessConfig.asset (Gas Safari/Setup/Crear tramos de Infinito)");
            return config;
        }

        [Test]
        public void Chunks_FollowTheDocs()
        {
            var config = Config();
            Assert.AreEqual(200f, config.difficultyStep, "dificultad +1 cada 200 m");
            Assert.AreEqual(3, config.checkpointEvery, "un checkpoint cada 3 tramos");
            Assert.AreEqual(3, config.lives, "termina al hundirse 3 veces");
            Assert.AreEqual(1, config.chunks.Count(c => c.difficulty == 0), "un solo tramo de salida");
            foreach (var chunk in config.chunks)
            {
                Assert.IsNotNull(chunk.prefab, chunk.id);
                Assert.That(chunk.length, Is.InRange(30f, 60f), chunk.id + ": tramos de 30-60 m");
                Assert.That(chunk.world, Is.InRange(1, 6), chunk.id);
                Assert.IsFalse(string.IsNullOrEmpty(chunk.sourceLevel), chunk.id + ": nivel de M5 de origen");
                Assert.That(chunk.prefab.Path.Length, Is.GreaterThan(2), chunk.id + ": cauce local");
                Assert.AreEqual(Vector3.zero, chunk.prefab.Path[0], chunk.id + ": entra por el origen");
                Assert.AreEqual(chunk.prefab.Path.Length, chunk.prefab.Accel.Length, chunk.id);
                if (Mathf.Abs(chunk.turn) > 0.5f)
                {
                    Assert.IsNotNull(chunk.mirroredPrefab, chunk.id + ": variante espejada");
                    Assert.AreEqual(-chunk.prefab.ExitYawLocal, chunk.mirroredPrefab.ExitYawLocal, 0.5f, chunk.id);
                    Assert.AreEqual(chunk.turn, chunk.prefab.ExitYawLocal, 0.5f, chunk.id);
                }
            }
            for (int tier = 1; tier <= EndlessPlanner.MAX_TIER; tier++)
                Assert.IsTrue(config.chunks.Any(c => c.difficulty == tier), "hay tramos de dificultad " + tier);
            for (int world = 1; world <= 6; world++)
                Assert.IsTrue(config.chunks.Any(c => c.world == world), "hay tramos del mundo " + world);
        }

        [Test]
        public void Planner_SameSeed_SameRiver()
        {
            var a = Take(Config().CreatePlanner(1234), 60);
            var b = Take(Config().CreatePlanner(1234), 60);
            CollectionAssert.AreEqual(a.Select(p => p.ToString()), b.Select(p => p.ToString()));
            var c = Take(Config().CreatePlanner(98765), 60);
            CollectionAssert.AreNotEqual(a.Select(p => p.ToString()), c.Select(p => p.ToString()), "otra semilla, otro río");
        }

        [Test]
        public void Planner_DifficultyRisesEvery200m_AndHeadingStaysBounded()
        {
            var config = Config();
            var specs = config.Specs;
            for (int seed = 1; seed <= 20; seed++)
            {
                var planner = config.CreatePlanner(seed);
                int previous = -1;
                float heading = 0f;
                for (int i = 0; i < 80; i++)
                {
                    var pick = planner.Next();
                    var spec = specs[pick.Spec];
                    if (i == 0)
                    {
                        Assert.AreEqual(0, spec.Difficulty, "empieza por la salida");
                        continue;
                    }
                    int tier = Mathf.Min(5, 1 + (int)(pick.Start / 200f));
                    Assert.AreEqual(tier, pick.Tier);
                    Assert.That(spec.Difficulty, Is.InRange(tier - 1, tier).And.GreaterThan(0), $"semilla {seed}, tramo {i}");
                    Assert.AreNotEqual(previous, pick.Spec, "no repite el tramo anterior");
                    previous = pick.Spec;
                    heading += pick.Mirrored ? -spec.Turn : spec.Turn;
                    Assert.That(Mathf.Abs(heading), Is.LessThanOrEqualTo(config.maxHeading + 0.01f), $"semilla {seed}: rumbo {heading}");
                }
                Assert.Greater(planner.Distance, 1000f * 3f, "80 tramos pasan los 3 km");
            }
        }

        [Test]
        public void Planner_SyntheticSpecs_FallsBackWhenTierIsMissing()
        {
            var specs = new List<ChunkSpec>
            {
                new ChunkSpec { Id = "start", Difficulty = 0, Length = 30f },
                new ChunkSpec { Id = "a", Difficulty = 1, Length = 50f },
                new ChunkSpec { Id = "b", Difficulty = 1, Length = 50f, Turn = 30f },
            };
            var planner = new EndlessPlanner(specs, 7, 200f, 45f);
            for (int i = 0; i < 40; i++)
            {
                var pick = planner.Next();
                Assert.That(pick.Spec, Is.InRange(0, 2));
                Assert.That(Mathf.Abs(planner.Heading), Is.LessThanOrEqualTo(45f));
            }
        }

        [Test]
        public void Modes_EndlessAndDailyAreActive()
        {
            CollectionAssert.Contains(GasSafari.Editor.ModesDataBuilder.Implemented, ProgressionRules.MODE_ENDLESS);
            var config = Config();
            Assert.IsNotNull(config.level, "datos de la escena Infinito");
            Assert.AreEqual("Infinito", config.level.sceneName);
            Assert.IsTrue(UnityEditor.EditorBuildSettings.scenes.Any(s => s.path == GasSafari.Editor.EndlessChunkBuilder.SCENE_PATH && s.enabled),
                "Infinito en Build Settings");
        }

        private static List<ChunkPick> Take(EndlessPlanner planner, int count)
        {
            var list = new List<ChunkPick>();
            for (int i = 0; i < count; i++)
                list.Add(planner.Next());
            return list;
        }
    }
}
