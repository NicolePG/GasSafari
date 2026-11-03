using GasSafari.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // Los valores esperados salen de docs/02 §14 y docs/06 §3.
    public class DataAssetsTests
    {
        private const string BALANCE_PATH = "Assets/_Project/Data/Balance/BalanceConfig.asset";
        private const string DIFFICULTY_FOLDER = "Assets/_Project/Data/Difficulty/";

        [Test]
        public void BalanceConfig_MatchesDocs()
        {
            var b = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BALANCE_PATH);
            Assert.IsNotNull(b);
            Assert.AreEqual(10f, b.raftBaseMass);
            Assert.AreEqual(150f, b.buoyancyPerPoint);
            Assert.AreEqual(new Vector3(0f, -0.3f, 0f), b.centerOfMassOffset);
            Assert.AreEqual(0.15f, b.puffMaxPressTime);
            Assert.AreEqual(35f, b.chargedCost);
            Assert.AreEqual(40f, b.maxAimAngle);
            CollectionAssert.AreEqual(new[] { 1f, 1f, 1.3f, 1.6f, 2f }, b.syncMultipliers);
            CollectionAssert.AreEqual(new[] { 0f, 0f, 10f, 15f, 20f }, b.syncMeterGain);
            Assert.AreEqual(0.5f, b.airControlFactor);
            Assert.AreEqual(1f, b.pingCooldown);
        }

        [TestCase("Difficulty_Familiar", 0.40f, 1.25f, false, 0f, 0.85f, 4f, CheckpointFilter.NormalPlusFamiliarOnly)]
        [TestCase("Difficulty_Normal", 0.25f, 1f, true, 3f, 1f, 6f, CheckpointFilter.Normal)]
        [TestCase("Difficulty_AmistadesRotas", 0.18f, 0.85f, true, 6f, 1.15f, 8f, CheckpointFilter.HardKeepOnly)]
        public void Difficulty_MatchesDocs(string asset, float sync, float regen, bool impacts,
            float penalty, float hazard, float autoRespawn, CheckpointFilter filter)
        {
            var d = AssetDatabase.LoadAssetAtPath<DifficultyData>(DIFFICULTY_FOLDER + asset + ".asset");
            Assert.IsNotNull(d, asset);
            Assert.AreEqual(sync, d.syncWindow);
            Assert.AreEqual(regen, d.regenMultiplier);
            Assert.AreEqual(impacts, d.impactsDamageChambers);
            Assert.AreEqual(penalty, d.respawnPenalty);
            Assert.AreEqual(hazard, d.hazardSpeed);
            Assert.AreEqual(autoRespawn, d.autoRespawnTime);
            Assert.AreEqual(filter, d.checkpointFilter);
        }
    }
}
