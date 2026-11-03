using GasSafari.Characters;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Pasiva de un personaje (componente en su prefab). Reacciona a subir y bajar de la balsa
    // o a eventos; nunca sondea en Update.
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat))]
    public abstract class PassiveBase : MonoBehaviour
    {
        protected PlayerAvatar Avatar { get; private set; }
        protected PlayerSeat Seat { get; private set; }
        // Balsa en la que va sentado ahora (null si está en el agua).
        protected RaftSeats Raft { get; private set; }

        protected virtual void Awake()
        {
            Avatar = GetComponent<PlayerAvatar>();
            Seat = GetComponent<PlayerSeat>();
        }

        protected virtual void OnEnable()
        {
            Seat.SatDown += HandleSatDown;
            Seat.StoodUp += HandleStoodUp;
            if (Seat.IsSeated)
                HandleSatDown(Seat.Raft);
        }

        protected virtual void OnDisable()
        {
            Seat.SatDown -= HandleSatDown;
            Seat.StoodUp -= HandleStoodUp;
            if (Raft != null)
                HandleStoodUp(Raft);
        }

        protected virtual void OnBoard(RaftSeats raft) { }
        protected virtual void OnLeave(RaftSeats raft) { }

        private void HandleSatDown(RaftSeats raft)
        {
            Raft = raft;
            OnBoard(raft);
        }

        private void HandleStoodUp(RaftSeats raft)
        {
            OnLeave(raft);
            Raft = null;
        }
    }
}
