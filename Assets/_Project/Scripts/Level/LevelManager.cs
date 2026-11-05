using System;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    public enum LevelState
    {
        Intro,
        Countdown,
        Running,
        Respawning,
        Finished,
        Results
    }

    // Máquina de estados del nivel (docs/10 §5): Intro → Countdown → Running → Respawning → Finished → Results.
    // La pausa se superpone con IsPaused. Antes de Running la balsa no recibe corriente ni comandos.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(LevelTimer))]
    public class LevelManager : MonoBehaviour
    {
        private const int COUNTDOWN_START = 3;

        [SerializeField] private LevelData data;
        [SerializeField] private BalanceConfig balance;
        [SerializeField] private RaftSpawner spawner;
        [SerializeField] private FinishGate finishGate;
        [SerializeField] private CheckpointSystem checkpoints;
        [Tooltip("Duración de la introducción antes de la cuenta; la cámara puede terminarla antes con SkipIntro")]
        [SerializeField] private float introDuration;
        [Tooltip("Segundos entre cruzar la meta y la pantalla de resultados")]
        [SerializeField] private float finishDelay = 2f;

        private readonly LevelCountdown countdown = new LevelCountdown();
        private LevelTimer timer;
        private RaftFlowReceiver flow;
        private float stateTimer;
        private bool hasBegun;

        public static LevelManager Current { get; private set; }

        public LevelData Data => data;
        public LevelState State { get; private set; }
        public bool IsPaused { get; private set; }
        public RaftController Raft => spawner.Raft;
        public LevelTimer Timer => timer;
        // 3, 2, 1 y 0 = ¡YA!; -1 fuera de la cuenta.
        public int CountdownValue => countdown.Value;
        public IReadOnlyList<PlayerAvatar> Avatars => spawner.Avatars;

        public event Action<LevelState> StateChanged;
        public event Action<int> CountdownTick;
        public event Action<bool> PausedChanged;
        // Tiempo total con penalizaciones.
        public event Action<float> Finished;

        private void Awake()
        {
            Current = this;
            timer = GetComponent<LevelTimer>();
        }

        private void Start()
        {
            if (!hasBegun)
                Begin(Services.IsReady ? Services.Session.Players : Array.Empty<PlayerSlot>());
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
            Unsubscribe();
        }

        public void Configure(LevelData levelData, BalanceConfig balanceConfig, RaftSpawner levelSpawner,
            FinishGate gate, CheckpointSystem checkpointSystem)
        {
            data = levelData;
            balance = balanceConfig;
            spawner = levelSpawner;
            finishGate = gate;
            checkpoints = checkpointSystem;
        }

        // Crea la balsa y deja el nivel en Intro. Start lo llama solo con los jugadores de la sesión.
        public void Begin(IReadOnlyList<PlayerSlot> slots)
        {
            hasBegun = true;
            Subscribe();
            spawner.Spawn(slots);
            flow = spawner.Raft.GetComponent<RaftFlowReceiver>();
            timer.ResetTimer();
            SetFrozen(true);
            stateTimer = introDuration;
            SetState(LevelState.Intro);
        }

        private void Subscribe()
        {
            finishGate.Crossed += OnFinishCrossed;
            GameEvents.OnRespawn += OnRespawned;
            if (checkpoints != null)
                checkpoints.RespawnStarted += OnRespawnStarted;
        }

        private void Unsubscribe()
        {
            if (finishGate != null)
                finishGate.Crossed -= OnFinishCrossed;
            GameEvents.OnRespawn -= OnRespawned;
            if (checkpoints != null)
                checkpoints.RespawnStarted -= OnRespawnStarted;
        }

        public void SkipIntro()
        {
            if (State == LevelState.Intro)
                stateTimer = 0f;
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused)
                return;
            IsPaused = paused;
            spawner.SetInputLocked(paused || State != LevelState.Running && State != LevelState.Respawning);
            PausedChanged?.Invoke(paused);
        }

        private void Update()
        {
            if (!hasBegun || IsPaused)
                return;

            switch (State)
            {
                case LevelState.Intro:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        StartCountdown();
                    break;
                case LevelState.Countdown:
                    UpdateCountdown();
                    break;
                case LevelState.Finished:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        SetState(LevelState.Results);
                    break;
            }
        }

        private void StartCountdown()
        {
            countdown.Start(COUNTDOWN_START, balance.pingCountdownStep);
            SetState(LevelState.Countdown);
            CountdownTick?.Invoke(countdown.Value);
        }

        private void UpdateCountdown()
        {
            if (!countdown.Advance(Time.deltaTime))
                return;

            CountdownTick?.Invoke(countdown.Value);
            if (countdown.Value > 0)
                return;

            countdown.Stop();
            SetFrozen(false);
            timer.StartTimer();
            SetState(LevelState.Running);
        }

        private void OnFinishCrossed(RaftController who)
        {
            if (who != spawner.Raft || (State != LevelState.Running && State != LevelState.Respawning))
                return;

            timer.StopTimer();
            spawner.SetInputLocked(true);
            stateTimer = finishDelay;
            SetState(LevelState.Finished);
            Finished?.Invoke(timer.Total);
            GameEvents.RaiseLevelFinished(timer.Total);
        }

        private void OnRespawnStarted()
        {
            if (State == LevelState.Running)
                SetState(LevelState.Respawning);
        }

        private void OnRespawned()
        {
            if (State == LevelState.Respawning)
                SetState(LevelState.Running);
        }

        // Antes de la salida: sin comandos y sin corriente.
        private void SetFrozen(bool frozen)
        {
            spawner.SetInputLocked(frozen);
            if (flow != null)
                flow.enabled = !frozen;
        }

        private void SetState(LevelState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
