using GasSafari.Hazards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // M5-02: obstáculos de W1. Valores de docs/05 en los HazardConfig y lógica pura de los ciclos.
    public class HazardW1Tests
    {
        private static HazardConfig Load(string file) =>
            AssetDatabase.LoadAssetAtPath<HazardConfig>("Assets/_Project/Data/Hazards/" + file + ".asset");

        [Test]
        public void Configs_MatchDocs05()
        {
            Assert.AreEqual(0.3f, Load("H01_Wall").Get("bounce"), "H01 rebote 0.3");
            var gate = Load("H03_TimedGate");
            Assert.That(gate.Get("cycle"), Is.InRange(2f, 3f), "H03 ciclo 2-3 s");
            Assert.AreEqual(0.5f, gate.Get("openFraction"), "H03 abierta 50 %");
            var wave = Load("H04_Wave");
            Assert.AreEqual(0.6f, wave.Get("amplitude"), "H04 amplitud");
            Assert.AreEqual(20f, wave.Get("push"), "H04 empuje");
            var blocker = Load("H05_MovingBlocker");
            Assert.AreEqual(2f, blocker.Get("speed"), "H05 velocidad");
            Assert.AreEqual(30f, blocker.Get("hit"), "H05 golpe");
            Assert.AreEqual(20f, Load("H07_Crate").Get("impactToBreak"), "H07 impacto");
            Assert.GreaterOrEqual(Load("H24_Crane").warningTime, 1f, "H24 aviso 1 s");
            Assert.AreEqual(10f, Load("H27_Trampoline").Get("launchSpeed"), "H27 10 m/s");
            Assert.AreEqual(30f, Load("H28_CrossingBoat").Get("hit"), "H28 bloqueador grande");
        }

        [Test]
        public void EveryHazard_WarnsAtLeastHalfASecond()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:HazardConfig", new[] { "Assets/_Project/Data/Hazards" }))
            {
                var config = AssetDatabase.LoadAssetAtPath<HazardConfig>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.GreaterOrEqual(config.warningTime, HazardConfig.MIN_WARNING_TIME, config.name);
            }
        }

        [Test]
        public void Prefabs_HaveTheRightComponentsAndLayer()
        {
            string[] names = { "H01_Wall", "H03_TimedGate", "H04_Wave", "H05_MovingBlocker", "H07_Crate", "H24_Crane", "H27_Awning", "H27_Fountain", "H28_Ferry", "H28_Speedboat" };
            foreach (var name in names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Hazards/{name}.prefab");
                Assert.IsNotNull(prefab, name);
                Assert.AreEqual(LayerMask.NameToLayer("Hazard"), prefab.layer, name);
            }
            var blocker = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Hazards/H05_MovingBlocker.prefab");
            Assert.IsTrue(blocker.GetComponent<HazardTags>().Has(HazardTag.Heavy), "H05 es Heavy");
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Hazards/H07_Crate.prefab");
            Assert.IsTrue(crate.GetComponent<HazardTags>().Has(HazardTag.Fragile), "H07 es Fragile");
            var ferry = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Hazards/H28_Ferry.prefab");
            Assert.IsTrue(ferry.GetComponent<HazardTags>().Has(HazardTag.Heavy), "H28 es Heavy");
        }

        [Test]
        public void TimedGate_IsOpenHalfTheCycle()
        {
            Assert.IsFalse(TimedGate.ClosedAt(0f, 2.5f, 0.5f));
            Assert.IsFalse(TimedGate.ClosedAt(1.2f, 2.5f, 0.5f));
            Assert.IsTrue(TimedGate.ClosedAt(1.3f, 2.5f, 0.5f));
            Assert.IsTrue(TimedGate.ClosedAt(2.4f, 2.5f, 0.5f));
            Assert.IsFalse(TimedGate.ClosedAt(2.6f, 2.5f, 0.5f), "el ciclo se repite");
        }

        [Test]
        public void MovingBlocker_StopsAtTheEndsAndRunsAtSpeed()
        {
            // Recorrido de 6 m a 2 m/s = 3 s de marcha, 1 s de parada en cada extremo.
            Assert.AreEqual(0f, MovingBlocker.Evaluate(0.5f, 6f, 2f, 1f, out int dir, out float wait));
            Assert.AreEqual(0, dir);
            Assert.AreEqual(0.5f, wait, 0.001f, "falta medio segundo para arrancar");

            Assert.AreEqual(0.5f, MovingBlocker.Evaluate(2.5f, 6f, 2f, 1f, out dir, out _), 0.001f);
            Assert.AreEqual(1, dir);

            Assert.AreEqual(1f, MovingBlocker.Evaluate(4.5f, 6f, 2f, 1f, out dir, out _), 0.001f);
            Assert.AreEqual(0, dir);

            Assert.AreEqual(0.5f, MovingBlocker.Evaluate(6.5f, 6f, 2f, 1f, out dir, out _), 0.001f);
            Assert.AreEqual(-1, dir, "vuelve");

            Assert.AreEqual(0f, MovingBlocker.Evaluate(8f + 0.5f, 6f, 2f, 1f, out dir, out _), 0.001f, "el ciclo dura 8 s");
        }

        [Test]
        public void MovingBlocker_IsDeterministic()
        {
            float a = MovingBlocker.Evaluate(13.37f, 6f, 2f, 1f, out int da, out _);
            float b = MovingBlocker.Evaluate(13.37f, 6f, 2f, 1f, out int db, out _);
            Assert.AreEqual(a, b);
            Assert.AreEqual(da, db);
        }

        [Test]
        public void Wave_LiftImpulseRaisesTheRaftByTheAmplitude()
        {
            float mass = 80f;
            float impulse = WaveHazard.LiftImpulse(mass, 0.6f);
            float speed = impulse / mass;
            float rise = speed * speed / (2f * 9.81f);
            Assert.AreEqual(0.6f, rise, 0.01f);
        }

        [Test]
        public void CranePattern_StaysBetweenTwoAndFourSeconds()
        {
            for (int i = 0; i < 12; i++)
                Assert.That(CraneBoss.IntervalAt(i), Is.InRange(2f, 4f), $"ataque {i}");
        }
    }
}
