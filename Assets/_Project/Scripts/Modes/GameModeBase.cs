using GasSafari.Core;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Modes
{
    // Un modo de juego sobre un nivel (skill game-modes): se agrega al LevelManager al cargar el nivel y recibe los ganchos.
    // El nivel no sabe en qué modo está; los resultados le preguntan por ILevelRunMode.
    public abstract class GameModeBase : MonoBehaviour, ILevelRunMode
    {
        private bool raftReady;

        public static GameModeBase Current { get; private set; }
        public LevelManager Level { get; private set; }
        public abstract string ModeId { get; }
        // Ayuda Real (docs/06 §3) en este modo.
        public virtual bool AllowsRoyalAssist => true;

        public void Bind(LevelManager level)
        {
            Level = level;
            Current = this;
            ResultsController.ActiveMode = this;
            level.StateChanged += HandleStateChanged;
            level.Finished += OnFinish;
            GameEvents.OnCheckpoint += OnCheckpoint;
            OnLevelLoaded();
            if (level.Raft != null)
                RaftReady();
        }

        private void HandleStateChanged(LevelState state)
        {
            if (!raftReady && Level.Raft != null)
                RaftReady();
            OnStateChanged(state);
        }

        private void RaftReady()
        {
            raftReady = true;
            OnRaftSpawned();
        }

        protected virtual void OnDestroy()
        {
            if (Level != null)
            {
                Level.StateChanged -= HandleStateChanged;
                Level.Finished -= OnFinish;
            }
            GameEvents.OnCheckpoint -= OnCheckpoint;
            if (Current == this)
                Current = null;
            if (ResultsController.ActiveMode == (ILevelRunMode)this)
                ResultsController.ActiveMode = null;
            OnExit();
        }

        protected virtual void OnLevelLoaded() { }
        protected virtual void OnRaftSpawned() { }
        protected virtual void OnStateChanged(LevelState state) { }
        protected virtual void OnCheckpoint(int index) { }
        protected virtual void OnFinish(float total) { }
        protected virtual void OnExit() { }

        // Reglas de Historia por defecto.
        public virtual int PreviousBestStars(string levelId) => ResultsController.PreviousBestStars(levelId);

        public virtual void BuildResults(LevelResults results)
        {
            results.RunMode = ModeId;
        }

        public virtual void Commit(LevelData level, LevelResults results)
        {
            ResultsController.Commit(level, results, false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            ResultsController.ActiveMode = null;
        }
    }
}
