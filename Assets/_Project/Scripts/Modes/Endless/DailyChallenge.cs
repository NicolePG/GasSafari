using System;
using System.Collections.Generic;
using System.Globalization;
using GasSafari.Save;

namespace GasSafari.Modes
{
    // Reglas del Desafío Diario (docs/06 §1) como funciones puras: semilla = fecha yyyyMMdd, recorrido de 1500 m con el
    // generador de Infinito y 1 modificador fijo para ese día; guarda el mejor tiempo del día y la racha de días seguidos.
    public static class DailyChallenge
    {
        public const string DATE_FORMAT = "yyyyMMdd";
        // Mezcla la semilla para el modificador: así no sale del mismo número que elige el primer tramo.
        private const int MODIFIER_SALT = 0x5EED;

        public static string DateKey(DateTime date) => date.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);

        public static int SeedFor(DateTime date) => int.Parse(DateKey(date), CultureInfo.InvariantCulture);

        // Índice del modificador del día entre 'count' (todos los de Caos, sin mirar candados: provisional, docs/06).
        public static int ModifierIndex(int seed, int count)
        {
            if (count <= 0)
                return -1;
            return new Random(seed ^ MODIFIER_SALT).Next(count);
        }

        public static ModifierData ModifierFor(int seed, IReadOnlyList<ModifierData> modifiers)
        {
            int index = ModifierIndex(seed, modifiers != null ? modifiers.Count : 0);
            return index >= 0 ? modifiers[index] : null;
        }

        public struct Outcome
        {
            public bool FirstToday;
            public bool NewBest;
            public float Best;
            public int Streak;
        }

        // Guarda un desafío terminado. La racha cuenta días seguidos con el desafío terminado: sube si el último fue ayer,
        // vuelve a 1 si pasó más de un día y no cambia si ya se terminó hoy. 'best' es el mejor tiempo del día.
        public static Outcome Record(SaveData.DailyRecord record, string dateKey, float time)
        {
            var outcome = new Outcome();
            if (record.lastDate == dateKey)
            {
                outcome.NewBest = record.best <= 0f || time < record.best;
                if (outcome.NewBest)
                    record.best = time;
            }
            else
            {
                outcome.FirstToday = true;
                outcome.NewBest = true;
                record.streak = IsDayBefore(record.lastDate, dateKey) ? record.streak + 1 : 1;
                record.lastDate = dateKey;
                record.best = time;
            }
            outcome.Best = record.best;
            outcome.Streak = record.streak;
            return outcome;
        }

        public static bool IsDayBefore(string previous, string today)
        {
            if (string.IsNullOrEmpty(previous) || string.IsNullOrEmpty(today))
                return false;
            if (!DateTime.TryParseExact(previous, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var a)
                || !DateTime.TryParseExact(today, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var b))
                return false;
            return (b - a).Days == 1;
        }

        // Frijolitos (docs/06 §2: 5 a 25; reparto provisional): la primera vez del día, base + extra por la racha; repetir, el mínimo.
        public static int Beans(EndlessConfig config, Outcome outcome)
        {
            if (!outcome.FirstToday)
                return config.beansMin;
            int beans = config.dailyFirstBeans + config.dailyStreakBeans * Math.Max(0, outcome.Streak - 1);
            return Math.Min(config.beansMax, Math.Max(config.beansMin, beans));
        }
    }
}
