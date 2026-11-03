using System;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Team
{
    // Súper Pedo Safari (docs/02 §6). Con el medidor lleno, cada animal a bordo pulsa el botón de equipo.
    [RequireComponent(typeof(RaftController), typeof(RaftSeats), typeof(TeamMeter))]
    public class SuperFartController : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private readonly List<PlayerAvatar> pressed = new List<PlayerAvatar>();
        private RaftController raft;
        private RaftSeats seats;
        private TeamMeter meter;
        private AbilitySynergySystem synergy;
        private float windowTimer;
        private bool wasReady;

        public bool IsReady => meter.IsFull;
        public bool IsWindowOpen => pressed.Count > 0;
        public int PressedCount => pressed.Count;

        public event Action Ready;
        public event Action WindowOpened;

        private void Awake()
        {
            raft = GetComponent<RaftController>();
            seats = GetComponent<RaftSeats>();
            meter = GetComponent<TeamMeter>();
            synergy = GetComponent<AbilitySynergySystem>();
        }

        private void Update()
        {
            if (!meter.IsFull)
            {
                wasReady = false;
                pressed.Clear();
                return;
            }
            if (!wasReady)
            {
                wasReady = true;
                Ready?.Invoke();
            }

            CollectPresses();
            if (pressed.Count == 0)
                return;

            windowTimer -= Time.deltaTime;
            int onBoard = seats.Seats.Count;
            if (pressed.Count >= onBoard || windowTimer <= 0f)
                Launch(onBoard > 0 ? (float)pressed.Count / onBoard : 0f);
        }

        private void CollectPresses()
        {
            var list = seats.Seats;
            for (int i = 0; i < list.Count; i++)
            {
                var avatar = list[i].Avatar;
                if (!avatar.Command.SuperHeld || pressed.Contains(avatar))
                    continue;

                if (pressed.Count == 0)
                {
                    windowTimer = balance.superFartWindow;
                    WindowOpened?.Invoke();
                }
                pressed.Add(avatar);
            }
        }

        private void Launch(float power)
        {
            pressed.Clear();
            meter.Consume();
            if (synergy != null)
                power = synergy.ApplySuperFart(power);

            var forward = raft.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var target = (forward * balance.superFartForward + Vector3.up * balance.superFartUp) * power;
            var body = raft.Body;
            raft.ApplyThrust(target - body.velocity, body.worldCenterOfMass, ForceMode.VelocityChange);

            Services.TimeController.SlowMotion(balance.superFartSlowMoScale, balance.superFartSlowMoDuration);
            GameEvents.RaiseSuperFart(power);
        }
    }
}
