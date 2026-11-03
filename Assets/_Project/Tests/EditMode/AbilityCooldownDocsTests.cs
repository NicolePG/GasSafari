using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using NUnit.Framework;
using UnityEditor;

namespace GasSafari.Tests
{
    // T-ABIL-01 (docs/11 §2): cada habilidad respeta su recarga. Recargas de docs/03.
    public class AbilityCooldownDocsTests
    {
        [TestCase("Alex", "roar", 18f)]
        [TestCase("Marty", "gallop", 12f)]
        [TestCase("Melman", "neck_hook", 15f)]
        [TestCase("Gloria", "belly_flop", 20f)]
        [TestCase("Julien", "royal_party", 30f)]
        public void TAbil01_EachAbilityRespectsItsCooldown(string character, string abilityId, float cooldown)
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + character + ".asset");
            Assert.IsNotNull(data.ability, character + " sin habilidad");
            Assert.AreEqual(abilityId, data.ability.id);
            Assert.AreEqual(cooldown, data.ability.cooldown);

            AssertCooldownBlocks(data.ability.cooldown);
        }

        [Test]
        public void TAbil01_JulienJumpRespectsItsCooldown()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Project/Data/Balance/BalanceConfig.asset");
            Assert.AreEqual(3f, balance.julienJumpCooldown);
            AssertCooldownBlocks(balance.julienJumpCooldown);
        }

        private static void AssertCooldownBlocks(float duration)
        {
            var cd = new AbilityCooldown();
            cd.Start(duration);
            cd.Tick(duration - 0.05f);
            Assert.IsFalse(cd.IsReady, "todavía en recarga");
            cd.Tick(0.1f);
            Assert.IsTrue(cd.IsReady, "lista al cumplirse la recarga");
        }
    }
}
