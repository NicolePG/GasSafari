using GasSafari.Core;
using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Characters
{
    // Cómo llega un pedo a la balsa: registro de sincronía y fuerza aplicada (docs/02 §2-5, §15).
    // Los componentes de equipo de la balsa actual solo se buscan al cambiar de balsa.
    public sealed class FartDelivery
    {
        private readonly PlayerSeat seat;
        private readonly BalanceConfig balance;
        private RaftSeats cachedRaft;
        private SyncDetector sync;
        private RaftEffects effects;

        // Locoto: multiplica la fuerza de los pedos de este animal.
        public float Multiplier { get; set; } = 1f;

        public FartDelivery(PlayerSeat seat, BalanceConfig balance)
        {
            this.seat = seat;
            this.balance = balance;
        }

        public float RegisterSync(ISyncMember member, FartType type, float baseImpulse)
        {
            Refresh();
            return sync != null ? sync.Register(member, type, baseImpulse) : 1f;
        }

        public void Push(Vector3 direction, float magnitude, ForceMode mode)
        {
            Refresh();
            var raft = seat.Raft.Controller;
            float factor = Multiplier * (raft.Buoyancy.IsAirborne ? balance.airControlFactor : 1f);
            if (effects != null)
                factor *= effects.FartForceMultiplier;
            raft.ApplyThrust(direction * (magnitude * factor), raft.SeatThrustPoint(seat.WorldPoint), mode);
        }

        private void Refresh()
        {
            if (seat.Raft == cachedRaft)
                return;
            cachedRaft = seat.Raft;
            sync = cachedRaft.GetComponent<SyncDetector>();
            effects = cachedRaft.GetComponent<RaftEffects>();
        }
    }

    // Una ráfaga que entra en un grupo de sync mantiene su multiplicador syncBlastBonusTime.
    public struct BlastBonus
    {
        private float multiplier;
        private float timer;

        public void Start(float newMultiplier, float duration)
        {
            multiplier = newMultiplier;
            timer = duration;
        }

        // Multiplicador de este paso físico; el tiempo corre aunque no haya bono.
        public float Consume(float deltaTime)
        {
            float current = timer > 0f ? multiplier : 1f;
            timer -= deltaTime;
            return current;
        }

        public void Clear() => timer = 0f;
    }
}
