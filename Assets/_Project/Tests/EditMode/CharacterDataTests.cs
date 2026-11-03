using GasSafari.Characters;
using NUnit.Framework;
using UnityEditor;

namespace GasSafari.Tests
{
    // Tabla "Resumen" de docs/03.
    public class CharacterDataTests
    {
        private const string FOLDER = "Assets/_Project/Data/Characters/";

        [TestCase("Alex", 100f, 22f, 135f, 12f, 0.6f, 3.0f, 1.8f)]
        [TestCase("Marty", 80f, 20f, 120f, 17f, 0.3f, 2.0f, 1.7f)]
        [TestCase("Melman", 100f, 17f, 100f, 12f, 0.6f, 2.5f, 2.6f)]
        [TestCase("Gloria", 140f, 25f, 150f, 8f, 0.8f, 4.0f, 1.6f)]
        [TestCase("Julien", 80f, 16f, 95f, 15f, 0.5f, 1.0f, 1.0f)]
        public void Stats_MatchDocs(string name, float gas, float puff, float blast, float regen,
            float delay, float weight, float height)
        {
            var c = AssetDatabase.LoadAssetAtPath<CharacterData>(FOLDER + name + ".asset");
            Assert.IsNotNull(c, name);
            Assert.AreEqual(gas, c.gasCapacity);
            Assert.AreEqual(puff, c.puffImpulse);
            Assert.AreEqual(blast, c.blastForce);
            Assert.AreEqual(regen, c.regenPerSecond);
            Assert.AreEqual(delay, c.regenDelay, 1e-5f);
            Assert.AreEqual(weight, c.weight, 1e-5f);
            Assert.AreEqual(height, c.height, 1e-5f);
            Assert.IsNotNull(c.prefab, "sin prefab");
        }
    }
}
