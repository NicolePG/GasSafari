using GasSafari.Core;
using GasSafari.PowerUps;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Corona Magnética (docs/03): con Julien a bordo, los power-ups cercanos van hacia él.
    public class JulienMagnetPassive : PassiveBase
    {
        [SerializeField] private BalanceConfig balance;

        protected override void OnBoard(RaftSeats raft) => PowerUpMagnets.Add(transform, balance.julienMagnetRadius);
        protected override void OnLeave(RaftSeats raft) => PowerUpMagnets.Remove(transform);
    }
}
