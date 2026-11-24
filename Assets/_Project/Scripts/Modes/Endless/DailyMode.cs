using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;

namespace GasSafari.Modes
{
    // Desafío Diario (docs/06 §1): EndlessTrack arma 1500 m con la semilla de la fecha y este modo pone el modificador del
    // día (DailyChallenge). Es una carrera contra el reloj hasta la meta: hundirse reaparece en el checkpoint con la
    // penalización de la dificultad. Guarda el mejor tiempo del día y la racha (SaveData.daily, logro Constante).
    public class DailyMode : GameModeBase
    {
        private readonly ModifierRuntime runtime = new ModifierRuntime();
        private EndlessTrack track;
        private EndlessHud hud;
        private DailyChallenge.Outcome outcome;

        public override string ModeId => ProgressionRules.MODE_DAILY;
        public override bool AllowsRoyalAssist => false;
        public EndlessTrack Track => track;
        public EndlessHud Hud => hud;
        public ModifierData Modifier { get; private set; }
        public ModifierRuntime Runtime => runtime;

        protected override void OnLevelLoaded()
        {
            track = EndlessTrack.Current;
            var config = ModesConfig.Instance;
            if (track != null && config != null)
                Modifier = DailyChallenge.ModifierFor(track.Seed, config.modifiers);
            hud = gameObject.AddComponent<EndlessHud>();
            hud.Build();
            var table = LocalizedText.Table;
            hud.SetSecond("daily.hud.modifier", Modifier != null && table != null ? table.Get(Modifier.NameKey) : "-");
            var today = Today();
            if (today != null && today.best > 0f)
                hud.SetThird("daily.hud.best", LevelSheetScreen.FormatTime(today.best));
            else
                hud.SetThird("daily.hud.first");
        }

        protected override void OnRaftSpawned()
        {
            if (Modifier != null)
                runtime.Apply(Level, Modifier, gameObject);
        }

        protected override void OnExit() => runtime.Revert(Level);

        private void Update()
        {
            if (track != null && hud != null)
                hud.SetMeters(track.MaxDistance, track.CourseLength);
        }

        // El registro del día en el guardado, o null si hoy todavía no se terminó.
        private SaveData.DailyRecord Today()
        {
            var data = Services.IsReady && Services.Progression != null ? Services.Progression.Data : null;
            if (data == null || track == null || data.daily == null || data.daily.lastDate != track.DateKey)
                return null;
            return data.daily;
        }

        public override int PreviousBestStars(string levelId) => 0;

        public override void BuildResults(LevelResults results)
        {
            base.BuildResults(results);
            results.ShowStars = false;
            results.Stars = 0;
            results.FirstTime = false;
            results.ModifiersActive = Modifier != null;
            // Vista previa sobre una copia: el guardado cambia recién en Commit.
            var data = Services.IsReady && Services.Progression != null ? Services.Progression.Data : null;
            var saved = data != null && data.daily != null ? data.daily : new SaveData.DailyRecord();
            var copy = new SaveData.DailyRecord { lastDate = saved.lastDate, streak = saved.streak, best = saved.best };
            outcome = DailyChallenge.Record(copy, track != null ? track.DateKey : string.Empty, results.Time);
            results.NewRecord = outcome.NewBest;
            results.DailyBest = outcome.Best;
            results.DailyStreak = outcome.Streak;
            var config = track != null ? track.Config : EndlessConfig.Instance;
            results.Beans = config != null ? DailyChallenge.Beans(config, outcome) : 0;
        }

        // No es un nivel: ni estrellas ni tiempos de nivel; el resultado va a SaveData.daily (M3-10).
        public override void Commit(LevelData level, LevelResults results)
        {
            var progression = Services.IsReady ? Services.Progression : null;
            if (progression == null || progression.Data == null || track == null)
                return;
            if (progression.Data.daily == null)
                progression.Data.daily = new SaveData.DailyRecord();
            DailyChallenge.Record(progression.Data.daily, track.DateKey, results.Time);
            progression.AddBeans(results.Beans);
            progression.Save();
            if (Services.Achievements != null)
                Services.Achievements.Evaluate();
        }
    }
}
