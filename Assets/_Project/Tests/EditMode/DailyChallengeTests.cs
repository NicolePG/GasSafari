using System;
using System.Linq;
using GasSafari.Modes;
using GasSafari.Save;
using NUnit.Framework;

namespace GasSafari.Tests
{
    // M7-10 Desafío Diario (docs/06 §1, docs/11 T-DAILY-01): la misma fecha da el mismo recorrido de 1500 m y el mismo
    // modificador; el guardado lleva el mejor tiempo del día y la racha de días seguidos.
    public class DailyChallengeTests
    {
        [Test]
        public void T_DAILY_01_SameDate_SameChallenge()
        {
            var config = EndlessConfig.Instance;
            Assert.IsNotNull(config);
            var modifiers = ModesConfig.Instance.modifiers;
            var date = new DateTime(2026, 11, 24, 9, 30, 0);
            int seed = DailyChallenge.SeedFor(date);
            Assert.AreEqual(20261124, seed, "semilla = fecha yyyyMMdd");
            Assert.AreEqual(seed, DailyChallenge.SeedFor(date.Date.AddHours(23)), "todo el día es el mismo desafío");

            var a = config.PlanDaily(seed);
            var b = config.PlanDaily(DailyChallenge.SeedFor(new DateTime(2026, 11, 24)));
            CollectionAssert.AreEqual(a.Select(p => p.ToString()), b.Select(p => p.ToString()), "mismo recorrido");
            Assert.AreSame(DailyChallenge.ModifierFor(seed, modifiers), DailyChallenge.ModifierFor(seed, modifiers), "mismo modificador");
            Assert.IsNotNull(DailyChallenge.ModifierFor(seed, modifiers));

            float length = a.Sum(p => config.chunks[p.Spec].length);
            Assert.GreaterOrEqual(length, 1500f, "recorrido de 1500 m");
            Assert.Less(length - config.chunks[a[a.Count - 1].Spec].length, 1500f, "sin tramos de más");
        }

        [Test]
        public void DifferentDates_UsuallyDifferentChallenges()
        {
            var config = EndlessConfig.Instance;
            var modifiers = ModesConfig.Instance.modifiers;
            var start = new DateTime(2026, 12, 1);
            var courses = Enumerable.Range(0, 7)
                .Select(d => string.Join(",", config.PlanDaily(DailyChallenge.SeedFor(start.AddDays(d))).Select(p => p.ToString())))
                .ToList();
            Assert.AreEqual(7, courses.Distinct().Count(), "una semana, 7 ríos distintos");
            var mods = Enumerable.Range(0, 30).Select(d => DailyChallenge.ModifierFor(DailyChallenge.SeedFor(start.AddDays(d)), modifiers)).Distinct().Count();
            Assert.GreaterOrEqual(mods, 4, "el modificador cambia entre días");
        }

        [Test]
        public void Record_KeepsBestOfTheDay_AndStreak()
        {
            var record = new SaveData.DailyRecord();
            var first = DailyChallenge.Record(record, "20261124", 420f);
            Assert.IsTrue(first.FirstToday);
            Assert.AreEqual(1, first.Streak);
            Assert.AreEqual(420f, record.best);

            var slower = DailyChallenge.Record(record, "20261124", 450f);
            Assert.IsFalse(slower.FirstToday);
            Assert.IsFalse(slower.NewBest);
            Assert.AreEqual(420f, record.best, "se queda el mejor del día");
            Assert.AreEqual(1, record.streak, "repetir el mismo día no suma racha");

            var faster = DailyChallenge.Record(record, "20261124", 400f);
            Assert.IsTrue(faster.NewBest);
            Assert.AreEqual(400f, record.best);

            var nextDay = DailyChallenge.Record(record, "20261125", 500f);
            Assert.AreEqual(2, nextDay.Streak, "día siguiente: racha +1");
            Assert.AreEqual(500f, record.best, "el mejor es del día nuevo");
            Assert.AreEqual("20261125", record.lastDate);

            DailyChallenge.Record(record, "20261201", 480f);
            Assert.AreEqual(1, record.streak, "un día sin jugar corta la racha");

            // Fin de mes y de año.
            var edge = new SaveData.DailyRecord { lastDate = "20261231", streak = 6, best = 300f };
            Assert.AreEqual(7, DailyChallenge.Record(edge, "20270101", 310f).Streak);
        }

        [Test]
        public void Beans_FiveToTwentyFive()
        {
            var config = EndlessConfig.Instance;
            Assert.AreEqual(15, DailyChallenge.Beans(config, new DailyChallenge.Outcome { FirstToday = true, Streak = 1 }));
            Assert.AreEqual(19, DailyChallenge.Beans(config, new DailyChallenge.Outcome { FirstToday = true, Streak = 3 }));
            Assert.AreEqual(25, DailyChallenge.Beans(config, new DailyChallenge.Outcome { FirstToday = true, Streak = 30 }));
            Assert.AreEqual(5, DailyChallenge.Beans(config, new DailyChallenge.Outcome { FirstToday = false, Streak = 3 }));
            Assert.AreEqual(5, EndlessMode.Beans(config, 0f));
            Assert.AreEqual(17, EndlessMode.Beans(config, 1234f));
            Assert.AreEqual(25, EndlessMode.Beans(config, 99999f));
        }
    }
}
