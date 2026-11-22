using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using UnityEngine;

namespace GasSafari.Modes
{
    // Historia: reglas normales. Además graba el fantasma de cada carrera limpia (sin Ayuda Real de saltar) y guarda el mejor
    // por nivel, para que el primer Contrarreloj ya tenga contra quién correr. También llena la tabla de los 5 mejores tiempos.
    public class StoryMode : GameModeBase
    {
        private GhostRecorder recorder;

        public override string ModeId => ProgressionRules.MODE_STORY;
        public GhostRecorder Recorder => recorder;
        // El fantasma que había antes de esta carrera (null si no había).
        public GhostData PreviousGhost { get; protected set; }
        public bool GhostSaved { get; private set; }
        public int TrialRank { get; private set; } = -1;

        protected static ModesConfig Config => ModesConfig.Instance;

        protected override void OnLevelLoaded()
        {
            if (Level.Data == null || !Level.Data.showTimer)
                return;
            float rate = Config != null ? Config.ghostSampleRate : 10f;
            recorder = new GhostRecorder(Level.Data.id, rate, Level.Data.silverTime * 1.5f);
            PreviousGhost = GhostStore.Load(Level.Data.id);
        }

        protected virtual void Update()
        {
            if (recorder == null || Level.Raft == null || !Level.Timer.IsRunning)
                return;
            var raft = Level.Raft.transform;
            recorder.Tick(Level.Timer.RunTime, raft.position, raft.rotation);
        }

        protected override void OnCheckpoint(int index)
        {
            if (recorder != null && Level.Timer.IsRunning)
                recorder.Split(index, Level.Timer.Total);
        }

        public override void Commit(LevelData level, LevelResults results)
        {
            base.Commit(level, results);
            SaveGhost(level, results);
        }

        // Guarda el fantasma si es la mejor carrera y anota el tiempo en la tabla del nivel.
        protected void SaveGhost(LevelData level, LevelResults results)
        {
            if (recorder == null || results.Skipped || results.ModifiersActive)
                return;
            var raft = Level.Raft != null ? Level.Raft.transform : null;
            if (raft != null)
                recorder.Tick(Level.Timer.RunTime, raft.position, raft.rotation);
            if (PreviousGhost == null || results.Time < PreviousGhost.Time)
            {
                var ghost = recorder.Finish(results.Time);
                ghost.Characters.Clear();
                foreach (var avatar in Level.Avatars)
                    ghost.Characters.Add(avatar.Character != null ? avatar.Character.id : string.Empty);
                GhostStore.Save(ghost);
                GhostSaved = true;
            }
            if (Services.IsReady && Services.Progression != null)
            {
                var characters = new System.Collections.Generic.List<string>();
                foreach (var avatar in Level.Avatars)
                    characters.Add(avatar.Character != null ? avatar.Character.id : string.Empty);
                int keep = Config != null ? Config.topTimes : 5;
                TrialRank = Services.Progression.RecordTrial(level.id, results.Time, TeamName(characters), characters, keep);
            }
        }

        // Decisión provisional (docs/06): no hay pantalla para escribir el nombre del equipo; se arma con los animales.
        public static string TeamName(System.Collections.Generic.IList<string> characters)
        {
            var catalog = GameCatalog.Instance;
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < characters.Count; i++)
            {
                string name = characters[i];
                if (catalog != null)
                {
                    foreach (var data in catalog.characters)
                    {
                        if (data != null && data.id == characters[i])
                            name = data.displayName;
                    }
                }
                if (names.Length > 0)
                    names.Append(" · ");
                names.Append(name);
            }
            return names.ToString();
        }
    }
}
