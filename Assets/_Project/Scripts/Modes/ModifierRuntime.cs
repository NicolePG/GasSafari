using System.Collections.Generic;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Efectos de los modificadores de Caos (docs/06 §1) sobre la balsa, los animales y la física, y cómo deshacerlos.
    // Lo usan Modificadores (los elegidos) y el Desafío Diario (el del día).
    public sealed class ModifierRuntime
    {
        private readonly List<BigHeadScaler> heads = new List<BigHeadScaler>();
        private Vector3 savedGravity;
        private bool gravityChanged;
        private DizzyCamera dizzy;

        public DizzyCamera Dizzy => dizzy;

        public void Apply(LevelManager level, ModifierData modifier, GameObject host)
        {
            var raft = level.Raft;
            switch (modifier.kind)
            {
                case ModifierKind.SoapyRaft:
                    raft.Buoyancy.ModifierDragMultiplier = modifier.value;
                    break;
                case ModifierKind.InfiniteGas:
                    foreach (var avatar in level.Avatars)
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
                    foreach (var avatar in level.Avatars)
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
                    dizzy = host.AddComponent<DizzyCamera>();
                    dizzy.Setup(modifier.value, ModesConfig.Instance != null ? ModesConfig.Instance.dizzyPeriod : 4f);
                    break;
                case ModifierKind.SingleChamber:
                    var chambers = raft.GetComponent<RaftChambers>();
                    if (chambers != null)
                        chambers.SingleChamber = true;
                    break;
            }
        }

        // La gravedad es global: siempre se devuelve. Lo demás vive en la balsa y los animales, que se van con la escena,
        // pero se deshace igual por si el modo se quita antes.
        public void Revert(LevelManager level)
        {
            if (gravityChanged)
                Physics.gravity = savedGravity;
            gravityChanged = false;
            if (dizzy != null)
                Object.Destroy(dizzy);
            foreach (var head in heads)
            {
                if (head != null)
                    Object.Destroy(head);
            }
            var raft = level != null ? level.Raft : null;
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
            foreach (var avatar in level.Avatars)
            {
                if (avatar != null)
                    avatar.Gas.Infinite = false;
            }
        }
    }
}
