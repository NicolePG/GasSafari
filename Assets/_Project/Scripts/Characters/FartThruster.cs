using System;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Characters
{
    // Toque y ráfaga con puntería (docs/02 §2-3). La fuerza entra a la balsa por RaftController.ApplyThrust.
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat), typeof(GasTank))]
    public class FartThruster : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private GasTank gas;
        private byte lastFartPresses;
        private bool pressing;
        private float pressTime;
        private float coughTimer;

        public bool IsBlasting { get; private set; }
        public bool IsCoughing => coughTimer > 0f;
        // La máquina de estados lo apaga mientras carga, se agarra o está fuera de la balsa.
        public bool CanFart { get; set; } = true;
        public Vector3 ThrustDirection { get; private set; }

        public event Action<FartType, float> Farted;
        public event Action Coughed;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            seat = GetComponent<PlayerSeat>();
            gas = GetComponent<GasTank>();
        }

        private void Update()
        {
            var cmd = avatar.Command;
            int newPresses = PlayerCommand.NewPresses(cmd.FartPresses, lastFartPresses);
            lastFartPresses = cmd.FartPresses;

            if (!seat.IsSeated)
            {
                CancelPress();
                return;
            }

            UpdateAim(cmd.Aim);

            if (coughTimer > 0f)
            {
                coughTimer -= Time.deltaTime;
                CancelPress();
                return;
            }
            if (!CanFart)
            {
                CancelPress();
                return;
            }

            if (!pressing)
            {
                if (cmd.FartHeld)
                {
                    pressing = true;
                    pressTime = 0f;
                }
                else if (newPresses > 0)
                {
                    // Pulsó y soltó entre dos frames.
                    Puff();
                }
                return;
            }

            pressTime += Time.deltaTime;
            if (!IsBlasting && pressTime >= balance.puffMaxPressTime)
                StartBlast();

            if (cmd.FartHeld)
                return;

            pressing = false;
            if (IsBlasting)
                IsBlasting = false;
            else
                Puff();
        }

        private void FixedUpdate()
        {
            if (!IsBlasting)
                return;

            float needed = balance.blastCostPerSecond * Time.fixedDeltaTime;
            float used = gas.Drain(needed);
            if (used <= 0f)
            {
                CancelPress();
                Cough();
                return;
            }
            Push(avatar.Character.blastForce * (used / needed), ForceMode.Force);
        }

        private void UpdateAim(float aim)
        {
            // Aim positivo gira la balsa a la derecha desde cualquier asiento.
            float yaw = -Mathf.Clamp(aim, -1f, 1f) * balance.maxAimAngle;
            seat.AimOffset = yaw;
            ThrustDirection = Quaternion.AngleAxis(yaw, Vector3.up) * seat.InwardDirection;
        }

        private void Puff()
        {
            if (!gas.TrySpend(balance.puffCost))
            {
                Cough();
                return;
            }
            float impulse = avatar.Character.puffImpulse;
            Push(impulse, ForceMode.Impulse);
            Notify(FartType.Puff, impulse);
        }

        private void StartBlast()
        {
            if (gas.IsEmpty)
            {
                CancelPress();
                Cough();
                return;
            }
            IsBlasting = true;
            Notify(FartType.Blast, avatar.Character.blastForce);
        }

        private void Push(float magnitude, ForceMode mode)
        {
            var raft = seat.Raft.Controller;
            float factor = raft.Buoyancy.IsAirborne ? balance.airControlFactor : 1f;
            raft.ApplyThrust(ThrustDirection * (magnitude * factor), seat.WorldPoint, mode);
        }

        private void Notify(FartType type, float impulse)
        {
            Farted?.Invoke(type, impulse);
            GameEvents.RaiseFart(avatar, type, impulse);
        }

        private void Cough()
        {
            coughTimer = balance.emptyCoughTime;
            Coughed?.Invoke();
        }

        private void CancelPress()
        {
            pressing = false;
            IsBlasting = false;
        }
    }
}
