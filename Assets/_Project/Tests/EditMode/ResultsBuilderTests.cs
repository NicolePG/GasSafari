using System.Collections.Generic;
using GasSafari.Level;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // docs/04 (estrellas), docs/06 §2 (frijolitos) y §4 (premios).
    public class ResultsBuilderTests
    {
        private LevelData level;

        [SetUp]
        public void SetUp()
        {
            level = ScriptableObject.CreateInstance<LevelData>();
            level.goldTime = 100f;
            level.silverTime = 140f;
            level.mangoCount = 3;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(level);
        }

        [Test]
        public void Stars_OneForFinishing_TwoWithSilver_ThreeWithGoldWithoutSinking()
        {
            Assert.AreEqual(3, ResultsBuilder.StarsFor(level, 90f, false));
            Assert.AreEqual(2, ResultsBuilder.StarsFor(level, 90f, true), "hundir la balsa quita la tercera");
            Assert.AreEqual(2, ResultsBuilder.StarsFor(level, 140f, false));
            Assert.AreEqual(1, ResultsBuilder.StarsFor(level, 141f, false));
        }

        [Test]
        public void Beans_FollowDocsRules()
        {
            Assert.AreEqual(20 + 30 + 30 + 4, ResultsBuilder.BeansFor(3, -1, 4), "primera vez, 3 estrellas nuevas, 4 syncs x4");
            Assert.AreEqual(20 + 10, ResultsBuilder.BeansFor(3, 2, 0), "una estrella nueva");
            Assert.AreEqual(20, ResultsBuilder.BeansFor(2, 3, 0), "sin estrellas nuevas");
            Assert.AreEqual(20 + 10, ResultsBuilder.BeansFor(1, 1, 99), "los syncs x4 dan 10 como máximo");
        }

        private static StatsData Stats(int players)
        {
            return new StatsData { Players = players };
        }

        [Test]
        public void Awards_PickThreeWithAtLeastOnePositiveAndOneShame()
        {
            var stats = Stats(3);
            stats.SyncGroups[0] = 3; stats.SyncGroups[1] = 1;
            stats.Rescues[2] = 2;
            stats.Falls[1] = 2;
            stats.NoGas[0] = 1; stats.NoGas[2] = 0;
            stats.Farts[0] = 10; stats.Farts[1] = 5; stats.Farts[2] = 2;

            for (int seed = 0; seed < 20; seed++)
            {
                var awards = new List<AwardResult>();
                ResultsBuilder.PickAwards(stats, new System.Random(seed), awards);
                Assert.AreEqual(3, awards.Count, "seed " + seed);
                Assert.IsTrue(awards.Exists(a => a.Positive), "seed " + seed);
                Assert.IsTrue(awards.Exists(a => !a.Positive), "seed " + seed);
                Assert.AreEqual(3, new HashSet<string>(awards.ConvertAll(a => a.Id)).Count, "sin repetidos");
            }
        }

        [Test]
        public void Awards_WinnerIsTheExtreme_TiesHaveNoWinner()
        {
            var stats = Stats(3);
            stats.SyncGroups[0] = 3; stats.SyncGroups[1] = 1;
            stats.Abilities[0] = 2; stats.Abilities[1] = 2;
            stats.Farts[0] = 10; stats.Farts[1] = 5; stats.Farts[2] = 2;
            stats.Falls[1] = 4;

            var awards = new List<AwardResult>();
            ResultsBuilder.PickAwards(stats, new System.Random(1), awards);

            Assert.IsFalse(awards.Exists(a => a.Id == "show_king"), "empate: sin premio");
            var sync = awards.Find(a => a.Id == "sync_master");
            if (sync.Id != null)
                Assert.AreEqual(0, sync.Player);
            var sleeping = awards.Find(a => a.Id == "sleeping_beauty");
            if (sleeping.Id != null)
                Assert.AreEqual(2, sleeping.Player, "gana quien menos pedos tiró");
            var titanic = awards.Find(a => a.Id == "captain_titanic");
            if (titanic.Id != null)
                Assert.AreEqual(1, titanic.Player);
        }

        [Test]
        public void Awards_SoloPlayerGetsNone()
        {
            var stats = Stats(1);
            stats.SyncGroups[0] = 5;
            var awards = new List<AwardResult>();
            ResultsBuilder.PickAwards(stats, new System.Random(0), awards);
            Assert.AreEqual(0, awards.Count);
        }

        [Test]
        public void Build_AssemblesResults()
        {
            var stats = Stats(2);
            stats.GoldenMangos = 2;
            stats.FullSyncGroups = 3;
            var results = ResultsBuilder.Build(level, 95f, stats, -1, null, new System.Random(0));

            Assert.AreEqual(3, results.Stars);
            Assert.AreEqual(2, results.Mangos);
            Assert.AreEqual(3, results.MangoTotal);
            Assert.IsTrue(results.FirstTime);
            Assert.AreEqual(20 + 30 + 30 + 3, results.Beans);
            Assert.IsNull(results.Replay);
        }

        [Test]
        public void ReplayClip_InterpolatesBetweenSamples()
        {
            var clip = new ReplayClip(
                new[] { Vector3.zero, Vector3.right * 10f, Vector3.right * 20f },
                new[] { Quaternion.identity, Quaternion.identity, Quaternion.identity }, 1, 2);
            clip.Evaluate(0.05f, out var position, out _);
            Assert.AreEqual(5f, position.x, 1e-4f);
            clip.Evaluate(99f, out position, out _);
            Assert.AreEqual(20f, position.x, 1e-4f);
            Assert.AreEqual(0.3f, clip.Duration, 1e-4f);
        }
    }
}
