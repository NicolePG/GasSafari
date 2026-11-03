using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Peso Pesado (docs/03): con Gloria a bordo, la componente lateral de corriente y viento baja.
    public class GloriaHeavyPassive : PassiveBase
    {
        [SerializeField] private BalanceConfig balance;

        private RaftFlowReceiver flow;

        protected override void OnBoard(RaftSeats raft)
        {
            flow = raft.GetComponent<RaftFlowReceiver>();
            flow.LateralFactor = balance.heavyLateralFactor;
        }

        protected override void OnLeave(RaftSeats raft)
        {
            flow.LateralFactor = 1f;
            flow = null;
        }
    }
}
