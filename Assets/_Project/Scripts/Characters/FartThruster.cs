using System;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Characters
{
    // Toque y ráfaga con puntería (docs/02 §2-3). La fuerza entra a la balsa por RaftController.ApplyThrust.
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat), typeof(GasTank))]
    public class FartThruster : MonoBehaviour, ISyncMember
    {
        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private GasTank gas;
        private byte lastFartPresses;
        private bool pressing;
        private float pressTime;
        private float coughTimer;
        // Tras recuperar CanFart con el botón aún pulsado, no empezar una ráfaga hasta soltarlo.
        private bool waitForRelease;
        public FartDelivery Delivery { get; private set; }
        private BlastBonus blastBonus;

        public bool IsBlasting { get; private set; }
        public bool IsCoughing => coughTimer > 0f;
        // La máquina de estados lo apaga mientras carga, se agarra o está fuera de la balsa.
        public bool CanFart { get; set; } = true;
        public Vector3 ThrustDirection { get; private set; }

        // tipo, impulso aplicado, multiplicador de sync (> 1 si este pedo completó un grupo).
        public event Action<FartType, float, float> Farted;
        public event Action Coughed;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            seat = GetComponent<PlayerSeat>();
            gas = GetComponent<GasTank>();
            Delivery = new FartDelivery(seat, balance);
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
                waitForRelease = true;
                return;
            }
            if (waitForRelease)
            {
                waitForRelease = cmd.FartHeld;
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

            bool wasBlasting = IsBlasting;
            CancelPress();
            if (!wasBlasting)
                Puff();
        }

        private void FixedUpdate()
        {
            if (!IsBlasting)
                return;
            // Cayó al agua entre el último Update y este paso físico.
            if (!seat.IsSeated)
            {
                CancelPress();
                return;
            }

            float needed = balance.blastCostPerSecond * Time.fixedDeltaTime;
            float used = gas.Drain(needed);
            if (used <= 0f)
            {
                CancelPress();
                Cough();
                return;
            }

            float multiplier = blastBonus.Consume(Time.fixedDeltaTime);
            Delivery.Push(ThrustDirection, avatar.Character.blastForce * multiplier * (used / needed), ForceMode.Force);
        }

        // Un grupo de sync creció después de este pedo.
        public void ApplySyncTopUp(FartType type, float baseImpulse, float from, float to)
        {
            if (!seat.IsSeated)
                return;
            if (type == FartType.Blast)
            {
                if (IsBlasting)
                    blastBonus.Start(to, balance.syncBlastBonusTime);
                return;
            }
            Delivery.Push(ThrustDirection, (to - from) * baseImpulse, ForceMode.Impulse);
        }

        private void UpdateAim(float aim)
        {
            // Aim positivo gira la balsa a la derecha desde cualquier asiento.
            float yaw = -Mathf.Clamp(aim, -1f, 1f) * balance.maxAimAngle;
            seat.AimOffset = yaw;
            ThrustDirection = Quaternion.AngleAxis(yaw, Vector3.up) * seat.InwardDirection;
        }

        // Pedo instantáneo (toque o cargado). Sin gas suficiente, tose.
        public bool FireImpulse(FartType type, float impulse, float cost)
        {
            if (!seat.IsSeated || IsCoughing)
                return false;
            if (!gas.TrySpend(cost))
            {
                Cough();
                return false;
            }
            float multiplier = Delivery.RegisterSync(this, type, impulse);
            float total = impulse * multiplier;
            Delivery.Push(ThrustDirection, total, ForceMode.Impulse);
            Notify(type, total, multiplier);
            return true;
        }

        private void Puff() => FireImpulse(FartType.Puff, avatar.Character.puffImpulse, balance.puffCost);

        private void StartBlast()
        {
            if (gas.IsEmpty)
            {
                CancelPress();
                Cough();
                return;
            }
            IsBlasting = true;
            float force = avatar.Character.blastForce;
            float multiplier = Delivery.RegisterSync(this, FartType.Blast, force);
            if (multiplier > 1f)
                blastBonus.Start(multiplier, balance.syncBlastBonusTime);
            Notify(FartType.Blast, force * multiplier, multiplier);
        }

        private void Notify(FartType type, float impulse, float multiplier)
        {
            Farted?.Invoke(type, impulse, multiplier);
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
            blastBonus.Clear();
        }
    }
}
