using System;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Characters
{
    // Pedo cargado (docs/02 §3): mantener Aguantar y soltar con el botón de pedo.
    [RequireComponent(typeof(PlayerAvatar), typeof(FartThruster), typeof(PlayerSeat))]
    public class FartCharger : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;
        private FartThruster thruster;
        private PlayerSeat seat;
        private byte lastFartPresses;
        private bool wasFartHeld;
        private bool holding;
        private bool fired;

        public bool IsCharging => holding && !fired;
        public float ChargeTime { get; private set; }
        public float Charge01 => Mathf.Clamp01(ChargeTime / balance.chargeTimeMax);
        public bool CanCharge { get; set; } = true;

        public event Action ChargeStarted;
        public event Action ChargeLost;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            thruster = GetComponent<FartThruster>();
            seat = GetComponent<PlayerSeat>();
        }

        private void Update()
        {
            var cmd = avatar.Command;
            bool fartPressed = PlayerCommand.NewPresses(cmd.FartPresses, lastFartPresses) > 0
                               || (cmd.FartHeld && !wasFartHeld);
            lastFartPresses = cmd.FartPresses;
            wasFartHeld = cmd.FartHeld;

            bool wantHold = cmd.HoldHeld && CanCharge && seat.IsSeated;
            if (!holding)
            {
                if (wantHold)
                    Begin();
                return;
            }
            if (!wantHold)
            {
                End();
                return;
            }
            if (fired)
                return;

            ChargeTime += Time.deltaTime;
            if (fartPressed && ChargeTime >= balance.chargeTimeMin)
                Fire();
        }

        // Impulso lineal entre chargeTimeMin y chargeTimeMax; más allá se mantiene el máximo.
        public float ImpulseFor(float chargeTime)
        {
            float t = Mathf.InverseLerp(balance.chargeTimeMin, balance.chargeTimeMax, chargeTime);
            return Mathf.Lerp(balance.chargedImpulseMin, balance.chargedImpulseMax, t);
        }

        private void Begin()
        {
            holding = true;
            fired = false;
            ChargeTime = 0f;
            // Mientras se aguanta no hay toques ni ráfagas. En M1-09 lo decide la máquina de estados.
            thruster.CanFart = false;
            ChargeStarted?.Invoke();
        }

        private void End()
        {
            if (!fired)
                ChargeLost?.Invoke();
            holding = false;
            ChargeTime = 0f;
            thruster.CanFart = true;
        }

        private void Fire()
        {
            fired = true;
            thruster.FireImpulse(FartType.Charged, ImpulseFor(ChargeTime), balance.chargedCost);
        }
    }
}
