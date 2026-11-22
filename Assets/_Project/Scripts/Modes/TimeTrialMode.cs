using GasSafari.Level;
using GasSafari.Progression;
using UnityEngine;

namespace GasSafari.Modes
{
    // Contrarreloj (docs/06 §1): corre contra el fantasma del mejor intento, muestra la diferencia en cada checkpoint y al
    // final, y guarda el récord. Sin Ayuda Real: un tramo saltado no puede ser récord (decisión provisional).
    public class TimeTrialMode : StoryMode
    {
        private static readonly Color DefaultGhostColor = new Color(0.55f, 0.9f, 1f, 0.35f);

        public override string ModeId => ProgressionRules.MODE_TIME_TRIAL;
        public override bool AllowsRoyalAssist => false;
        public GhostPlayer Ghost { get; private set; }
        public TimeTrialHud Hud { get; private set; }

        protected override void OnRaftSpawned()
        {
            if (Recorder == null)
                return;
            var config = Config;
            if (PreviousGhost != null && PreviousGhost.Count > 0)
            {
                Ghost = new GameObject("Ghost").AddComponent<GhostPlayer>();
                Ghost.Setup(PreviousGhost, Level.Timer, Level.Raft.transform,
                    config != null ? config.ghostMaterial : null, config != null ? config.ghostColor : DefaultGhostColor);
            }
            Hud = new GameObject("TimeTrialHud").AddComponent<TimeTrialHud>();
            Hud.Build(PreviousGhost != null ? PreviousGhost.Time : 0f, config != null ? config.splitShowSeconds : 3f);
        }

        protected override void OnCheckpoint(int index)
        {
            base.OnCheckpoint(index);
            if (PreviousGhost == null || Hud == null || !Level.Timer.IsRunning || index <= 0)
                return;
            float split = PreviousGhost.SplitAt(index);
            if (split >= 0f)
                Hud.ShowSplit(Level.Timer.Total - split);
        }

        protected override void OnFinish(float total)
        {
            if (Hud != null && PreviousGhost != null)
                Hud.ShowSplit(total - PreviousGhost.Time);
        }

        public override void BuildResults(LevelResults results)
        {
            base.BuildResults(results);
            if (PreviousGhost != null)
                results.GhostDelta = results.Time - PreviousGhost.Time;
            results.NewRecord = !results.Skipped && (PreviousGhost == null || results.Time < PreviousGhost.Time);
        }

        protected override void OnExit()
        {
            if (Ghost != null)
                Destroy(Ghost.gameObject);
            if (Hud != null)
                Destroy(Hud.gameObject);
        }
    }
}
