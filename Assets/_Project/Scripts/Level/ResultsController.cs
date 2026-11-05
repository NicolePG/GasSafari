using System;
using UnityEngine;

namespace GasSafari.Level
{
    // Une las estadísticas del nivel, el buffer de repetición y el LevelManager: al llegar a Results arma los resultados.
    public class ResultsController : MonoBehaviour
    {
        // Mejor número de estrellas ya logrado en un nivel, -1 si nunca se terminó. Lo conecta ProgressionService (M3-10).
        public static Func<string, int> PreviousBestStars = _ => -1;
        // Guarda el resultado (nivel, resultados, modificadores activos). Lo conecta ProgressionService.
        public static Action<LevelData, LevelResults, bool> Commit = (level, results, modifiers) => { };

        public static void ResetHooks()
        {
            PreviousBestStars = _ => -1;
            Commit = (level, results, modifiers) => { };
        }

        [SerializeField] private LevelManager level;
        [SerializeField] private LevelStats stats;
        [SerializeField] private ReplayRecorder recorder;

        public LevelResults Results { get; private set; }
        // Para pruebas y repeticiones: el azar de los premios.
        public System.Random Rng { get; set; } = new System.Random();

        public event Action<LevelResults> ResultsReady;

        private void Start()
        {
            stats.Bind(level);
            recorder.Bind(level, stats);
            level.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (level != null)
                level.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(LevelState state)
        {
            if (state != LevelState.Results)
                return;

            string id = level.Data != null ? level.Data.id : string.Empty;
            Results = ResultsBuilder.Build(level.Data, level.Timer.Total, stats.Data, PreviousBestStars(id), recorder.Worst, Rng);
            if (level.Data != null)
                Commit(level.Data, Results, false);
            ResultsReady?.Invoke(Results);
        }
    }
}
