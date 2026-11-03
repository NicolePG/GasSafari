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
        private float blastMultiplier = 1f;
        private float blastBonusTimer;
        private RaftSeats syncRaft;
        private SyncDetector sync;

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

            float needed = balance.blastCostPerSecond * Time.fixedDeltaTime;
            float used = gas.Drain(needed);
            if (used <= 0f)
            {
                CancelPress();
                Cough();
                return;
            }

            float multiplier = blastBonusTimer > 0f ? blastMultiplier : 1f;
            blastBonusTimer -= Time.fixedDeltaTime;
            Push(avatar.Character.blastForce * multiplier * (used / needed), ForceMode.Force);
        }

        // Un grupo de sync creció después de este pedo.
        public void ApplySyncTopUp(FartType type, float baseImpulse, float from, float to)
        {
            if (!seat.IsSeated)
                return;
            if (type == FartType.Blast)
            {
                if (IsBlasting)
                    StartBlastBonus(to);
                return;
            }
            Push((to - from) * baseImpulse, ForceMode.Impulse);
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
            float total = impulse * RegisterSync(type, impulse);
            Push(total, ForceMode.Impulse);
            Notify(type, total);
            return true;
        }

        private void Puff()
        {
            FireImpulse(FartType.Puff, avatar.Character.puffImpulse, balance.puffCost);
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
            float force = avatar.Character.blastForce;
            float multiplier = RegisterSync(FartType.Blast, force);
            if (multiplier > 1f)
                StartBlastBonus(multiplier);
            Notify(FartType.Blast, force * multiplier);
        }

        // Una ráfaga que entra en un grupo mantiene el multiplicador syncBlastBonusTime.
        private void StartBlastBonus(float multiplier)
        {
            blastMultiplier = multiplier;
            blastBonusTimer = balance.syncBlastBonusTime;
        }

        private float RegisterSync(FartType type, float baseImpulse)
        {
            if (seat.Raft != syncRaft)
            {
                syncRaft = seat.Raft;
                sync = syncRaft.GetComponent<SyncDetector>();
            }
            return sync != null ? sync.Register(this, type, baseImpulse) : 1f;
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
            blastBonusTimer = 0f;
        }
    }
}
