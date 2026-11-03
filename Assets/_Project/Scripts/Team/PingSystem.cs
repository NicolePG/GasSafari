using System;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Team
{
    // Pings de intención del equipo de una balsa y cuenta 3-2-1 (docs/02 §11).
    [RequireComponent(typeof(RaftSeats), typeof(SyncDetector))]
    public class PingSystem : MonoBehaviour
    {
        private const int COUNTDOWN_START = 3;

        [SerializeField] private BalanceConfig balance;

        private readonly PingType[] lastPing = new PingType[GameSession.MAX_PLAYERS];
        private readonly float[] cooldown = new float[GameSession.MAX_PLAYERS];
        private readonly float[] lastNowTime = new float[GameSession.MAX_PLAYERS];
        private readonly bool[] noGasShown = new bool[GameSession.MAX_PLAYERS];
        private RaftSeats seats;
        private SyncDetector sync;
        private float stepTimer;

        public bool IsCountingDown => CountdownValue >= 0;
        // 3, 2, 1 y 0 = ¡YA!; -1 sin cuenta.
        public int CountdownValue { get; private set; } = -1;

        public event Action<int> CountdownTick;

        private void Awake()
        {
            seats = GetComponent<RaftSeats>();
            sync = GetComponent<SyncDetector>();
            for (int i = 0; i < lastNowTime.Length; i++)
                lastNowTime[i] = float.NegativeInfinity;
        }

        private void Update()
        {
            var crew = seats.Crew;
            int count = Mathf.Min(crew.Count, GameSession.MAX_PLAYERS);
            for (int i = 0; i < count; i++)
                UpdateMember(i, crew[i].Avatar);
            UpdateCountdown();
        }

        private void UpdateMember(int i, PlayerAvatar avatar)
        {
            cooldown[i] -= Time.deltaTime;
            var ping = avatar.Command.Ping;
            if (ping != PingType.None && ping != lastPing[i] && cooldown[i] <= 0f)
            {
                cooldown[i] = balance.pingCooldown;
                Fire(i, avatar, ping);
            }
            lastPing[i] = ping;

            bool low = avatar.Gas.Normalized < balance.noGasPingThreshold;
            if (low && !noGasShown[i])
                GameEvents.RaisePing(avatar, PingType.NoGas);
            noGasShown[i] = low;
        }

        private void Fire(int index, PlayerAvatar avatar, PingType ping)
        {
            GameEvents.RaisePing(avatar, ping);
            if (ping != PingType.Now)
                return;

            lastNowTime[index] = Time.time;
            int calling = 0;
            for (int i = 0; i < lastNowTime.Length; i++)
            {
                if (Time.time - lastNowTime[i] <= balance.pingNowWindow)
                    calling++;
            }
            if (calling >= 2 && !IsCountingDown)
                StartCountdown();
        }

        private void StartCountdown()
        {
            for (int i = 0; i < lastNowTime.Length; i++)
                lastNowTime[i] = float.NegativeInfinity;
            CountdownValue = COUNTDOWN_START;
            stepTimer = balance.pingCountdownStep;
            CountdownTick?.Invoke(CountdownValue);
        }

        private void UpdateCountdown()
        {
            if (!IsCountingDown)
                return;

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f)
                return;

            if (CountdownValue == 0)
            {
                CountdownValue = -1;
                return;
            }

            CountdownValue--;
            stepTimer = balance.pingCountdownStep;
            CountdownTick?.Invoke(CountdownValue);
            if (CountdownValue == 0)
            {
                // ¡YA!: la ventana ampliada cubre el grupo que se arma en este momento.
                float duration = sync.Window + balance.pingSyncBonus;
                sync.SetWindowBonus(SyncBonusSource.PingCountdown, balance.pingSyncBonus, duration);
            }
        }
    }
}
