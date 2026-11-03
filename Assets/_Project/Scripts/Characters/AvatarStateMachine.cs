using System;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Characters
{
    // Estado del animal y qué puede hacer en cada uno (docs/10 §6, docs/02 §7).
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat), typeof(FartThruster))]
    [RequireComponent(typeof(FartCharger))]
    public class AvatarStateMachine : MonoBehaviour
    {
        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private FartThruster thruster;
        private FartCharger charger;

        public AvatarState State { get; private set; }
        public bool IsGrabbing => State == AvatarState.Grabbing;
        public bool IsOnRaft => State <= AvatarState.AbilityCast;

        public event Action<AvatarState, AvatarState> StateChanged;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            seat = GetComponent<PlayerSeat>();
            thruster = GetComponent<FartThruster>();
            charger = GetComponent<FartCharger>();
            ApplyPermissions(State);
        }

        private void Update()
        {
            var cmd = avatar.Command;
            switch (State)
            {
                case AvatarState.Seated:
                    if (cmd.GrabHeld)
                        Enter(AvatarState.Grabbing);
                    else if (cmd.HoldHeld)
                        Enter(AvatarState.Charging);
                    break;
                case AvatarState.Charging:
                    if (cmd.GrabHeld)
                        Enter(AvatarState.Grabbing);
                    else if (!cmd.HoldHeld)
                        Enter(AvatarState.Seated);
                    break;
                case AvatarState.Grabbing:
                    if (!cmd.GrabHeld)
                        Enter(AvatarState.Seated);
                    break;
            }
        }

        // Para los sistemas de caída, nado y reaparición.
        public void Enter(AvatarState next)
        {
            if (next == State)
                return;
            var previous = State;
            State = next;
            ApplyPermissions(next);
            StateChanged?.Invoke(previous, next);
        }

        private void ApplyPermissions(AvatarState state)
        {
            seat.CanMove = state == AvatarState.Seated || state == AvatarState.Charging;
            thruster.CanFart = state == AvatarState.Seated;
            charger.CanCharge = state == AvatarState.Charging;
        }
    }
}
