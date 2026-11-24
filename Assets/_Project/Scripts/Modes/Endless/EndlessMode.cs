using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using UnityEngine;

namespace GasSafari.Modes
{
    // Infinito (docs/06 §1): río sin fin de EndlessTrack; la dificultad sube cada 200 m y la partida termina al hundirse
    // 3 veces. La puntuación es la distancia; se guarda el récord (SaveData.endlessMeters, logro Kilómetro). Sin estrellas
    // ni Ayuda Real.
    public class EndlessMode : GameModeBase
    {
        private EndlessTrack track;
        private EndlessHud hud;
        private int sinks;
        private int previousBest;

        public override string ModeId => ProgressionRules.MODE_ENDLESS;
        public override bool AllowsRoyalAssist => false;
        public EndlessTrack Track => track;
        public EndlessHud Hud => hud;
        public int Lives => Mathf.Max(0, MaxLives - sinks);
        public int MaxLives => track != null ? track.Config.lives : 3;
        public bool Ended { get; private set; }

        protected override void OnLevelLoaded()
        {
            track = EndlessTrack.Current;
            previousBest = Services.IsReady && Services.Progression != null && Services.Progression.Data != null
                ? Services.Progression.Data.endlessMeters : 0;
            hud = gameObject.AddComponent<EndlessHud>();
            hud.Build();
            if (track != null)
                track.TierChanged += OnTierChanged;
            GameEvents.OnRaftSunk += OnSunk;
            GameEvents.OnRaftCapsized += OnSunk;
            RefreshHud();
        }

        protected override void OnExit()
        {
            if (track != null)
                track.TierChanged -= OnTierChanged;
            GameEvents.OnRaftSunk -= OnSunk;
            GameEvents.OnRaftCapsized -= OnSunk;
        }

        // Hundirse o volcar cuenta como perder la balsa (provisional, docs/06). Al tercero se acaba; antes reaparece en el
        // último checkpoint como en un nivel.
        private void OnSunk()
        {
            if (Ended || Level == null || (Level.State != LevelState.Running && Level.State != LevelState.Respawning))
                return;
            sinks++;
            RefreshHud();
            if (sinks >= MaxLives)
            {
                Ended = true;
                Level.EndRun();
            }
            else if (Lives == 1)
            {
                EndlessHud.Announce("endless.last_life");
            }
        }

        private void OnTierChanged(int tier)
        {
            EndlessHud.Announce("endless.tier_up", tier);
            RefreshHud();
        }

        private void Update()
        {
            if (track != null && hud != null)
                hud.SetMeters(track.MaxDistance, track.CourseLength);
        }

        private void RefreshHud()
        {
            if (hud == null)
                return;
            int tier = track != null ? track.Tier : 1;
            hud.SetSecond("endless.hud.record_tier", Mathf.Max(previousBest, Meters), tier);
            hud.SetThird("endless.sinks", sinks);
        }

        public int Meters => track != null ? Mathf.FloorToInt(track.MaxDistance) : 0;

        public static int Beans(EndlessConfig config, float meters)
        {
            int beans = config.beansMin + (int)(meters / Mathf.Max(1f, config.metersPerBean));
            return Mathf.Min(config.beansMax, beans);
        }

        public override int PreviousBestStars(string levelId) => 0;

        public override void BuildResults(LevelResults results)
        {
            base.BuildResults(results);
            results.ShowStars = false;
            results.Stars = 0;
            results.FirstTime = false;
            results.Distance = Meters;
            results.NewRecord = Meters > previousBest;
            results.BestDistance = Mathf.Max(previousBest, Meters);
            var config = track != null ? track.Config : EndlessConfig.Instance;
            results.Beans = config != null ? Beans(config, Meters) : 0;
        }

        // No es un nivel: no guarda estrellas ni tiempos; solo el récord de distancia y los frijolitos.
        public override void Commit(LevelData level, LevelResults results)
        {
            var progression = Services.IsReady ? Services.Progression : null;
            if (progression == null || progression.Data == null)
                return;
            progression.AddBeans(results.Beans);
            if (Services.Achievements != null)
                Services.Achievements.RecordEndlessDistance(Meters);
            else if (Meters > progression.Data.endlessMeters)
                progression.Data.endlessMeters = Meters;
            progression.Save();
        }
    }
}
