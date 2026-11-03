using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Team;
using NUnit.Framework;

namespace GasSafari.Tests
{
    // T-SYNC-01..03 (docs/11 §2). Valores de docs/02 §14 y ventana Normal de docs/06 §3.
    public class SyncGroupTrackerTests
    {
        private const float WINDOW = 0.25f;
        private static readonly float[] Multipliers = { 1f, 1f, 1.3f, 1.6f, 2f };
        private static readonly float[] MeterGain = { 0f, 0f, 10f, 15f, 20f };

        private class FakeMember : ISyncMember
        {
            public readonly List<float> TopUps = new List<float>();
            public float Total = 1f;

            public void ApplySyncTopUp(FartType type, float baseImpulse, float from, float to)
            {
                TopUps.Add((to - from) * baseImpulse);
                Total = to;
            }
        }

        private SyncGroupTracker tracker;

        [SetUp]
        public void SetUp()
        {
            tracker = new SyncGroupTracker(Multipliers, MeterGain);
        }

        [TestCase(0.24f, 2)]
        [TestCase(0.26f, 1)]
        public void TSync01_WindowEdge(float secondAt, int expectedSize)
        {
            tracker.Register(new FakeMember(), FartType.Puff, 20f, 0f, WINDOW);
            var result = tracker.Register(new FakeMember(), FartType.Puff, 20f, secondAt, WINDOW);
            Assert.AreEqual(expectedSize, result.GroupSize);
        }

        [Test]
        public void TSync02_MultipliersAndMeter()
        {
            var a = new FakeMember();
            float meter = 0f;
            meter += tracker.Register(a, FartType.Puff, 20f, 0f, WINDOW).MeterGain;

            var r2 = tracker.Register(new FakeMember(), FartType.Puff, 20f, 0.05f, WINDOW);
            Assert.AreEqual(1.3f, r2.Multiplier, 1e-5f);
            meter += r2.MeterGain;
            Assert.AreEqual(10f, meter, 1e-5f);

            var r3 = tracker.Register(new FakeMember(), FartType.Puff, 20f, 0.1f, WINDOW);
            Assert.AreEqual(1.6f, r3.Multiplier, 1e-5f);
            meter += r3.MeterGain;
            Assert.AreEqual(15f, meter, 1e-5f);

            var r4 = tracker.Register(new FakeMember(), FartType.Puff, 20f, 0.2f, WINDOW);
            Assert.AreEqual(2f, r4.Multiplier, 1e-5f);
            meter += r4.MeterGain;
            Assert.AreEqual(20f, meter, 1e-5f);

            // El primero recibió (1.3-1)·20, luego (1.6-1.3)·20 y (2-1.6)·20: termina en x2.
            Assert.AreEqual(3, a.TopUps.Count);
            Assert.AreEqual(6f, a.TopUps[0], 1e-4f);
            Assert.AreEqual(2f, a.Total, 1e-5f);
        }

        [Test]
        public void TSync03_SamePlayerTwiceIsNotSync()
        {
            var a = new FakeMember();
            tracker.Register(a, FartType.Puff, 20f, 0f, WINDOW);
            var again = tracker.Register(a, FartType.Puff, 20f, 0.1f, WINDOW);

            Assert.AreEqual(1, again.GroupSize);
            Assert.AreEqual(1f, again.Multiplier);
            Assert.AreEqual(0, a.TopUps.Count);
            Assert.AreEqual(1, tracker.GroupSize);
        }
    }
}
