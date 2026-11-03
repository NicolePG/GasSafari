using GasSafari.Abilities;
using NUnit.Framework;

namespace GasSafari.Tests
{
    public class AbilityCooldownTests
    {
        [Test]
        public void Cooldown_BlocksUntilDurationElapsed()
        {
            var cd = new AbilityCooldown();
            Assert.IsTrue(cd.IsReady);

            cd.Start(18f);
            Assert.IsFalse(cd.IsReady);
            cd.Tick(17.9f);
            Assert.IsFalse(cd.IsReady);
            Assert.AreEqual(17.9f / 18f, cd.Progress, 1e-4f);
            cd.Tick(0.2f);
            Assert.IsTrue(cd.IsReady);
        }
    }
}
