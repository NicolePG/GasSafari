using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Vista Alta (docs/03): con Melman a bordo, los avisos de peligro llegan antes.
    public class MelmanLookoutPassive : PassiveBase
    {
        [SerializeField] private BalanceConfig balance;

        private RaftEffects effects;

        protected override void OnBoard(RaftSeats raft)
        {
            effects = raft.GetComponent<RaftEffects>();
            effects.WarningLeadBonus += balance.lookoutWarningLead;
        }

        protected override void OnLeave(RaftSeats raft)
        {
            effects.WarningLeadBonus -= balance.lookoutWarningLead;
            effects = null;
        }
    }
}
