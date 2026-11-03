using GasSafari.Characters;
using NUnit.Framework;

namespace GasSafari.Tests
{
    // T-GAS-01 y T-GAS-02 (docs/11 §2). Stats base de docs/03: gas 100, recarga 12/s, retardo 0.6 s.
    public class GasTankStateTests
    {
        private const float CAPACITY = 100f;
        private const float REGEN = 12f;
        private const float DELAY = 0.6f;

        private GasTankState tank;

        [SetUp]
        public void SetUp()
        {
            tank = new GasTankState(CAPACITY, REGEN, DELAY, 1f);
        }

        [Test]
        public void TGas01_NeverBelowZero()
        {
            Assert.IsFalse(tank.TrySpend(CAPACITY + 1f));
            Assert.AreEqual(CAPACITY, tank.Gas);

            Assert.AreEqual(CAPACITY, tank.Drain(CAPACITY * 2f));
            Assert.AreEqual(0f, tank.Gas);
            Assert.IsTrue(tank.IsEmpty);
            Assert.IsFalse(tank.TrySpend(0.01f));
        }

        [Test]
        public void TGas01_NeverAboveCapacity()
        {
            tank.Drain(10f);
            tank.Tick(DELAY + 100f);
            Assert.AreEqual(CAPACITY, tank.Gas);
        }

        [Test]
        public void TGas02_RegenStartsExactlyAfterDelay()
        {
            tank.Drain(50f);
            tank.Tick(DELAY - 0.01f);
            Assert.AreEqual(50f, tank.Gas, 1e-4f);

            tank.Tick(0.02f);
            Assert.AreEqual(50f + REGEN * 0.01f, tank.Gas, 1e-4f);

            tank.Tick(1f);
            Assert.AreEqual(50f + REGEN * 1.01f, tank.Gas, 1e-4f);
        }

        [Test]
        public void TGas02_SpendingResetsDelay()
        {
            tank.Drain(50f);
            tank.Tick(DELAY + 1f);
            float before = tank.Gas;
            tank.TrySpend(5f);
            tank.Tick(DELAY - 0.01f);
            Assert.AreEqual(before - 5f, tank.Gas, 1e-4f);
        }

        [Test]
        public void RegenMultiplier_ScalesRecharge()
        {
            tank.RegenMultiplier = 1.25f;
            tank.Drain(50f);
            tank.Tick(DELAY);
            tank.Tick(1f);
            Assert.AreEqual(50f + REGEN * 1.25f, tank.Gas, 1e-4f);
        }
    }
}
