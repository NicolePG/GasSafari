using System;
using System.Collections;
using System.Linq;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-10 Desafío Diario con 4 bots (docs/06 §1): la escena Infinito arma los 1500 m de la fecha con la meta al final,
    // pone el modificador del día, los bots llegan a la meta y el resultado del día (mejor tiempo y racha) queda en el
    // guardado (M3-10). Se elige el primer día desde el 24/11/2026 cuyo modificador no desarma a los bots (Pedos al revés,
    // Gravedad lunar y Cámara única los confunden: los bots no los conocen).
    public class DailyModeTests
    {
        private const float LIMIT = 900f; // s de juego
        private static readonly ModifierKind[] BotFriendly =
        {
            ModifierKind.SoapyRaft, ModifierKind.InfiniteGas, ModifierKind.BigHeads, ModifierKind.TurboCurrent, ModifierKind.DizzyCamera
        };

        private EndlessTestRig rig;
        private DateTime date;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            date = new DateTime(2026, 11, 24);
            var modifiers = ModesConfig.Instance.modifiers;
            while (Array.IndexOf(BotFriendly, DailyChallenge.ModifierFor(DailyChallenge.SeedFor(date), modifiers).kind) < 0)
                date = date.AddDays(1);
            rig = new EndlessTestRig();
            rig.Prepare(ProgressionRules.MODE_DAILY);
            // Ayer se terminó el desafío: hoy la racha pasa a 3.
            rig.Save.daily.lastDate = DailyChallenge.DateKey(date.AddDays(-1));
            rig.Save.daily.streak = 2;
            rig.Save.daily.best = 321f;
            EndlessTrack.DateOverride = date;
            yield return rig.Load();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return rig.Unload();
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Daily_FourBots_FinishTheCourse_SaveBestAndStreak()
        {
            var mode = rig.Mode as DailyMode;
            Assert.IsNotNull(mode, "la escena Infinito en modo Desafío Diario");
            Assert.IsTrue(rig.Track.IsDaily);
            Assert.AreEqual(DailyChallenge.SeedFor(date), rig.Track.Seed, "semilla = fecha");
            Assert.GreaterOrEqual(rig.Track.CourseLength, 1500f);
            Assert.IsTrue(UnityEngine.Object.FindFirstObjectByType<FinishGate>().GetComponent<Collider>().enabled, "meta al final");
            var expected = DailyChallenge.ModifierFor(DailyChallenge.SeedFor(date), ModesConfig.Instance.modifiers);
            Assert.AreSame(expected, mode.Modifier, "modificador del día");
            yield return rig.WaitForRunning();
            AssertModifierApplied(mode);
            Debug.Log($"DAILY fecha={DailyChallenge.DateKey(date)} modificador={mode.Modifier.id} tramos={rig.Track.ChunksPlaced} largo={rig.Track.CourseLength:F0}");

            float elapsed = 0f;
            while (rig.FinishedAt < 0f && elapsed < LIMIT)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                if ((int)elapsed % 20 == 0)
                    rig.Log("daily", elapsed);
            }
            rig.Log("daily-fin", elapsed);
            Assert.Greater(rig.FinishedAt, 0f, $"4 bots terminan los 1500 m en menos de {LIMIT} s");
            Assert.GreaterOrEqual(rig.Checkpoints.CurrentIndex, rig.Checkpoints.Active.Count - 2, "pasan por los checkpoints");

            yield return rig.WaitForState(LevelState.Results, 10f);
            var results = UnityEngine.Object.FindFirstObjectByType<ResultsController>().Results;
            Assert.IsNotNull(results);
            Assert.AreEqual(ProgressionRules.MODE_DAILY, results.RunMode);
            Assert.IsFalse(results.ShowStars);
            Assert.AreEqual(3, results.DailyStreak);
            Assert.IsTrue(results.NewRecord, "primer desafío del día");
            Assert.AreEqual(results.Time, results.DailyBest, 0.01f);
            Assert.AreEqual(19, results.Beans, "15 + 2 por cada día de racha después del primero");

            var daily = rig.Save.daily;
            Assert.AreEqual(DailyChallenge.DateKey(date), daily.lastDate, "resultado del día en el guardado");
            Assert.AreEqual(3, daily.streak);
            Assert.AreEqual(results.Time, daily.best, 0.01f);
            Assert.AreEqual(19, rig.Save.frijolitos);
            Assert.IsEmpty(rig.Save.levels.Where(l => l.id == "INF"), "el Diario no guarda un nivel");
        }

        private void AssertModifierApplied(DailyMode mode)
        {
            var raft = rig.Level.Raft;
            switch (mode.Modifier.kind)
            {
                case ModifierKind.SoapyRaft:
                    Assert.AreEqual(mode.Modifier.value, raft.Buoyancy.ModifierDragMultiplier, 1e-4f);
                    break;
                case ModifierKind.InfiniteGas:
                    Assert.IsTrue(rig.Level.Avatars.All(a => a.Gas.Infinite));
                    break;
                case ModifierKind.BigHeads:
                    Assert.IsTrue(rig.Level.Avatars.All(a => a.GetComponent<BigHeadScaler>() != null));
                    break;
                case ModifierKind.TurboCurrent:
                    Assert.AreEqual(mode.Modifier.value, raft.GetComponent<RaftFlowReceiver>().CurrentMultiplier, 1e-4f);
                    break;
                case ModifierKind.DizzyCamera:
                    Assert.IsNotNull(mode.Runtime.Dizzy);
                    break;
            }
        }
    }
}
