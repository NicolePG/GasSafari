using System;
using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Input;
using GasSafari.PowerUps;
using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Level
{
    // Una lección del T-0 en la escena (docs/04 T-0, docs/07 §8): zona que la empieza, barrera que se abre al cumplir
    // la condición del TutorialStep y piezas opcionales (triggers de la boya, chorro). En multijugador la cumple
    // cualquier jugador, salvo la de sincronía, que es del equipo.
    public class TutorialLesson : MonoBehaviour
    {
        [SerializeField] private TutorialStep step;
        [Tooltip("Trigger que empieza la lección (y, en la 9, la zona de la rampa)")] [SerializeField] private Collider zone;
        [SerializeField] private BubbleBarrier barrier;
        [Tooltip("Lección 3: triggers alrededor de la boya, en orden")] [SerializeField] private TutorialGate[] gates = Array.Empty<TutorialGate>();
        [Tooltip("Lección 8: chorro que empuja")] [SerializeField] private HoseJet jet;

        private readonly Dictionary<PlayerAvatar, float> seatStart = new Dictionary<PlayerAvatar, float>();
        private readonly Dictionary<PlayerAvatar, float> timers = new Dictionary<PlayerAvatar, float>();
        private IReadOnlyList<PlayerAvatar> avatars;
        private RaftController raft;
        private int nextGate;
        private bool raftInZone;
        private bool pinged;

        public TutorialStep Step => step;
        public BubbleBarrier Barrier => barrier;
        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }
        public bool RaftInZone => raftInZone;

        public event Action<TutorialLesson> Entered;
        public event Action<TutorialLesson> Completed;

        public void Configure(TutorialStep lessonStep, Collider lessonZone, BubbleBarrier lessonBarrier, TutorialGate[] lessonGates, HoseJet lessonJet)
        {
            step = lessonStep;
            zone = lessonZone;
            barrier = lessonBarrier;
            gates = lessonGates ?? Array.Empty<TutorialGate>();
            jet = lessonJet;
        }

        public void Bind(IReadOnlyList<PlayerAvatar> crew, RaftController levelRaft)
        {
            avatars = crew;
            raft = levelRaft;
        }

        private void OnEnable()
        {
            for (int i = 0; i < gates.Length; i++)
                gates[i].Passed += OnGate;
            GameEvents.OnFart += OnFart;
            GameEvents.OnSync += OnSync;
            GameEvents.OnPowerUp += OnPowerUp;
            GameEvents.OnPing += OnPing;
            GameEvents.OnSuperFart += OnSuperFart;
        }

        private void OnDisable()
        {
            for (int i = 0; i < gates.Length; i++)
                gates[i].Passed -= OnGate;
            GameEvents.OnFart -= OnFart;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnPowerUp -= OnPowerUp;
            GameEvents.OnPing -= OnPing;
            GameEvents.OnSuperFart -= OnSuperFart;
        }

        // La empieza el director (al entrar en la zona o al seguir de la lección anterior).
        public void Begin()
        {
            if (IsActive || IsComplete)
                return;
            IsActive = true;
            nextGate = 0;
            pinged = false;
            seatStart.Clear();
            timers.Clear();
            if (barrier != null)
                barrier.Close();
        }

        // Reaparición en el checkpoint de la lección: el progreso parcial se pierde, lo cumplido se mantiene.
        public void ResetProgress()
        {
            nextGate = 0;
            pinged = false;
            seatStart.Clear();
            timers.Clear();
        }

        public void Complete()
        {
            if (IsComplete)
                return;
            IsComplete = true;
            IsActive = false;
            if (barrier != null)
                barrier.Open();
            Completed?.Invoke(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsRaft(other))
                return;
            raftInZone = true;
            Entered?.Invoke(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsRaft(other))
                raftInZone = false;
        }

        private static bool IsRaft(Collider other) =>
            other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<RaftController>(out _);

        private void Update()
        {
            if (!IsActive || avatars == null)
                return;
            switch (step.condition)
            {
                case TutorialCondition.MoveSeat: TrackSeats(); break;
                case TutorialCondition.Blast:
                case TutorialCondition.GrabInJet:
                    TrackHeld();
                    break;
            }
        }

        private void TrackSeats()
        {
            for (int i = 0; i < avatars.Count; i++)
            {
                var avatar = avatars[i];
                if (avatar == null || IsBot(avatar) || !avatar.TryGetComponent<PlayerSeat>(out var seat) || !seat.IsSeated)
                    continue;
                if (!seatStart.TryGetValue(avatar, out float start))
                {
                    seatStart[avatar] = seat.Angle;
                    continue;
                }
                if (Mathf.Abs(Mathf.DeltaAngle(start, seat.Angle)) >= step.amount)
                    Complete();
            }
        }

        // Una condición que un jugador debe sostener 'amount' segundos seguidos.
        private bool IsHolding(PlayerAvatar avatar)
        {
            if (step.condition == TutorialCondition.Blast)
                return avatar.TryGetComponent<FartThruster>(out var thruster) && thruster.IsBlasting;
            return jet != null && jet.RaftInside && avatar.TryGetComponent<AvatarStateMachine>(out var states) && states.IsGrabbing;
        }

        private void TrackHeld()
        {
            for (int i = 0; i < avatars.Count; i++)
            {
                var avatar = avatars[i];
                if (avatar == null || IsBot(avatar))
                    continue;
                timers.TryGetValue(avatar, out float t);
                t = IsHolding(avatar) ? t + Time.deltaTime : 0f;
                timers[avatar] = t;
                if (t >= step.amount)
                {
                    Complete();
                    return;
                }
            }
        }

        private void OnGate(TutorialGate gate)
        {
            if (!IsActive || step.condition != TutorialCondition.RoundBuoy)
                return;
            if (nextGate < gates.Length && gates[nextGate] == gate)
                nextGate++;
            if (nextGate >= gates.Length)
                Complete();
        }

        private void OnFart(PlayerAvatar who, FartType type, float impulse)
        {
            if (!IsActive || !OnOurRaft(who) || IsBot(who))
                return;
            if (step.condition == TutorialCondition.Puff && type == FartType.Puff)
                Complete();
            else if (step.condition == TutorialCondition.ChargedOnRamp && type == FartType.Charged && raftInZone
                     && who.TryGetComponent<FartCharger>(out var charger) && impulse >= charger.ImpulseFor(step.amount) - 0.01f)
                Complete();
        }

        private void OnSync(int players, float multiplier)
        {
            if (IsActive && step.condition == TutorialCondition.Sync && players >= 2)
                Complete();
        }

        private void OnPowerUp(PowerUpData data, PlayerAvatar taker)
        {
            if (IsActive && step.condition == TutorialCondition.PickupBeans && data != null && data.id == "P01")
                Complete();
        }

        private void OnPing(PlayerAvatar who, PingType type)
        {
            if (!IsActive || step.condition != TutorialCondition.PingNow || type != PingType.Now || IsBot(who))
                return;
            pinged = true;
            // En solo (un humano) basta con el ping; con más, hace falta la cuenta 3-2-1 (CountdownTick).
            if (HumanCount() <= 1)
                Complete();
        }

        // Lo conecta el director a PingSystem.CountdownTick de la balsa.
        public void OnCountdown(int value)
        {
            if (IsActive && step.condition == TutorialCondition.PingNow && pinged)
                Complete();
        }

        private void OnSuperFart(float power)
        {
            if (IsActive && step.condition == TutorialCondition.SuperFart)
                Complete();
        }

        private bool OnOurRaft(PlayerAvatar who)
        {
            return raft == null || (who.TryGetComponent<PlayerSeat>(out var seat) && seat.Raft != null && seat.Raft.Controller == raft);
        }

        private static bool IsBot(PlayerAvatar avatar) => avatar.TryGetComponent<BotBrain>(out _);

        private int HumanCount()
        {
            int humans = 0;
            if (avatars == null)
                return 1;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (avatars[i] != null && !IsBot(avatars[i]))
                    humans++;
            }
            return Mathf.Max(1, humans);
        }
    }
}
