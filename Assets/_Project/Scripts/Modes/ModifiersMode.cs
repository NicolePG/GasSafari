using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;

namespace GasSafari.Modes
{
    // Modificadores / Caos (docs/06 §1): aplica los ModifierData elegidos (GameSession.Modifiers) al crear la balsa y los
    // revierte al salir. Los tiempos no cuentan para estrellas ni récords; la Ayuda Real se ofrece igual.
    public class ModifiersMode : GameModeBase
    {
        private readonly List<ModifierData> active = new List<ModifierData>();
        private readonly ModifierRuntime runtime = new ModifierRuntime();

        public override string ModeId => ProgressionRules.MODE_MODIFIERS;
        public IReadOnlyList<ModifierData> Active => active;
        public DizzyCamera Dizzy => runtime.Dizzy;

        protected override void OnLevelLoaded()
        {
            var config = ModesConfig.Instance;
            if (config == null || !Services.IsReady)
                return;
            foreach (var id in Services.Session.Modifiers)
            {
                var data = config.FindModifier(id);
                if (data != null && !active.Contains(data))
                    active.Add(data);
            }
        }

        protected override void OnRaftSpawned()
        {
            foreach (var modifier in active)
                runtime.Apply(Level, modifier, gameObject);
        }

        public override void BuildResults(LevelResults results)
        {
            base.BuildResults(results);
            results.ModifiersActive = true;
            results.Beans = ResultsBuilder.BeansWithoutProgress(results.FullSyncGroups);
        }

        public override void Commit(LevelData level, LevelResults results)
        {
            ResultsController.Commit(level, results, true);
        }

        protected override void OnExit() => runtime.Revert(Level);
    }
}
