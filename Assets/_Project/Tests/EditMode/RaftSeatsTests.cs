using GasSafari.Core;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEditor;

namespace GasSafari.Tests
{
    public class RaftSeatsTests
    {
        private const float MIN = 45f;

        [TestCase(180f, 120f, -60f, -15f)]
        [TestCase(180f, 120f, -10f, -10f)]
        [TestCase(180f, 150f, 30f, 30f)]
        [TestCase(180f, 150f, -10f, 0f)]
        [TestCase(10f, 330f, -40f, -0f)]
        [TestCase(350f, 50f, 20f, 15f)]
        [TestCase(180f, 200f, 10f, 0f)]
        public void ClampDelta_RespectsMinimumSeparation(float from, float other, float delta, float expected)
        {
            Assert.AreEqual(expected, RaftSeats.ClampDelta(from, other, delta, MIN), 0.001f);
        }

        [Test]
        public void StartAngles_MatchDocs()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Project/Data/Balance/BalanceConfig.asset");
            CollectionAssert.AreEqual(new[] { 180f }, balance.startSeatAngles[0].angles);
            CollectionAssert.AreEqual(new[] { 150f, 210f }, balance.startSeatAngles[1].angles);
            CollectionAssert.AreEqual(new[] { 120f, 180f, 240f }, balance.startSeatAngles[2].angles);
            CollectionAssert.AreEqual(new[] { 90f, 150f, 210f, 270f }, balance.startSeatAngles[3].angles);
        }
    }
}
