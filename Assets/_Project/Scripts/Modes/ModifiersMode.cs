using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Modificadores / Caos (docs/06 §1): aplica los ModifierData elegidos (GameSession.Modifiers) al crear la balsa y los
    // revierte al salir. Los tiempos no cuentan para estrellas ni récords; la Ayuda Real se ofrece igual.
    public class ModifiersMode : GameModeBase
    {
        private readonly List<ModifierData> active = new List<ModifierData>();
        private readonly List<BigHeadScaler> heads = new List<BigHeadScaler>();
        private Vector3 savedGravity;
        private bool gravityChanged;
        private DizzyCamera dizzy;

        public override string ModeId => ProgressionRules.MODE_MODIFIERS;
        public IReadOnlyList<ModifierData> Active => active;
        public DizzyCamera Dizzy => dizzy;

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
                Apply(modifier);
        }

        private void Apply(ModifierData modifier)
        {
            var raft = Level.Raft;
            switch (modifier.kind)
            {
                case ModifierKind.SoapyRaft:
                    raft.Buoyancy.ModifierDragMultiplier = modifier.value;
                    break;
                case ModifierKind.InfiniteGas:
                    foreach (var avatar in Level.Avatars)
                        avatar.Gas.Infinite = true;
                    break;
                case ModifierKind.LunarGravity:
                    if (!gravityChanged)
                    {
                        savedGravity = Physics.gravity;
                        gravityChanged = true;
                    }
                    Physics.gravity = savedGravity * modifier.value;
                    break;
                case ModifierKind.ReversedFarts:
                    var effects = raft.GetComponent<RaftEffects>();
                    if (effects == null)
                        effects = raft.gameObject.AddComponent<RaftEffects>();
                    effects.FartSign = -1f;
                    break;
                case ModifierKind.BigHeads:
                    foreach (var avatar in Level.Avatars)
                    {
                        var scaler = avatar.gameObject.AddComponent<BigHeadScaler>();
                        scaler.Setup(modifier.value);
                        heads.Add(scaler);
                    }
                    break;
                case ModifierKind.TurboCurrent:
                    var flow = raft.GetComponent<RaftFlowReceiver>();
                    if (flow != null)
                        flow.CurrentMultiplier = modifier.value;
                    break;
                case ModifierKind.DizzyCamera:
                    dizzy = gameObject.AddComponent<DizzyCamera>();
                    dizzy.Setup(modifier.value, ModesConfig.Instance != null ? ModesConfig.Instance.dizzyPeriod : 4f);
                    break;
                case ModifierKind.SingleChamber:
                    var chambers = raft.GetComponent<RaftChambers>();
                    if (chambers != null)
                        chambers.SingleChamber = true;
                    break;
            }
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

        // La gravedad es global: siempre se devuelve. Lo demás vive en la balsa y los animales, que se van con la escena,
        // pero se deshace igual por si el modo se quita antes.
        protected override void OnExit()
        {
            if (gravityChanged)
                Physics.gravity = savedGravity;
            gravityChanged = false;
            if (dizzy != null)
                Destroy(dizzy);
            foreach (var head in heads)
            {
                if (head != null)
                    Destroy(head);
            }
            var raft = Level != null ? Level.Raft : null;
            if (raft == null)
                return;
            raft.Buoyancy.ModifierDragMultiplier = 1f;
            var effects = raft.GetComponent<RaftEffects>();
            if (effects != null)
                effects.FartSign = 1f;
            var flowReceiver = raft.GetComponent<RaftFlowReceiver>();
            if (flowReceiver != null)
                flowReceiver.CurrentMultiplier = 1f;
            var chambersOnRaft = raft.GetComponent<RaftChambers>();
            if (chambersOnRaft != null)
                chambersOnRaft.SingleChamber = false;
            foreach (var avatar in Level.Avatars)
            {
                if (avatar != null)
                    avatar.Gas.Infinite = false;
            }
        }
    }
}
