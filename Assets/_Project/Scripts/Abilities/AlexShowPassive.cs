using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Estrella del Show (docs/03): si Alex entra en un grupo de sync, la ventana del equipo sube un rato.
    [RequireComponent(typeof(FartThruster))]
    public class AlexShowPassive : PassiveBase
    {
        [SerializeField] private BalanceConfig balance;

        private FartThruster thruster;
        private SyncDetector sync;

        protected override void Awake()
        {
            base.Awake();
            thruster = GetComponent<FartThruster>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvents.OnSync += OnSync;
        }

        protected override void OnDisable()
        {
            GameEvents.OnSync -= OnSync;
            base.OnDisable();
        }

        protected override void OnBoard(RaftSeats raft) => sync = raft.GetComponent<SyncDetector>();
        protected override void OnLeave(RaftSeats raft) => sync = null;

        private void OnSync(int players, float multiplier)
        {
            // No se acumula: volver a activarla solo renueva la duración.
            if (sync != null && sync.IsInGroup(thruster))
                sync.SetWindowBonus(SyncBonusSource.StarOfTheShow, balance.showWindowBonus, balance.showBonusDuration);
        }
    }
}
