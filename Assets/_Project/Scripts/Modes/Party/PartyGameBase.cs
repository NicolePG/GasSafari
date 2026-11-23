using System;
using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    public enum PartyState
    {
        Intro,
        Countdown,
        Playing,
        Over,
        Results
    }

    // Base de un minijuego de Fiesta (docs/06 §1, skill game-modes): participantes con los bots que completan, balsas,
    // cuenta 3-2-1, tiempo, puntaje por jugador y puesto final, que suma a la Fiesta (PartyCup). Cada minijuego pone sus
    // reglas encima y, como objetivo de los bots (IBotGoal), les dice qué hacer.
    [DefaultExecutionOrder(-90)]
    public abstract class PartyGameBase : MonoBehaviour, IBotGoal
    {
        private const int COUNTDOWN_FROM = 3;

        [SerializeField] protected PartyGameData data;
        [SerializeField] protected RaftSpawner spawner;
        [SerializeField] protected BalanceConfig balance;
        [SerializeField] private float introSeconds = 1.5f;
        [Tooltip("Segundos entre el final y el panel de resultados")]
        [SerializeField] private float overSeconds = 3f;

        protected readonly List<PlayerSlot> players = new List<PlayerSlot>();
        // Mismo orden que players.
        protected readonly List<PlayerAvatar> avatars = new List<PlayerAvatar>();
        protected readonly List<RaftController> rafts = new List<RaftController>();
        protected int[] scores = new int[0];
        private readonly List<PlayerSlot> crewBuffer = new List<PlayerSlot>();
        private readonly List<PlayerAvatar> spawnBuffer = new List<PlayerAvatar>();
        private readonly LevelCountdown countdown = new LevelCountdown();
        private float stateTimer;
        private bool begun;
        private PartyHud hud;

        public static PartyGameBase Current { get; private set; }
        // Las pruebas lo apagan para elegir datos y jugadores antes de empezar (Begin a mano).
        public static bool AutoBegin { get; set; } = true;
        public PartyGameData Data => data;
        public PartyState State { get; private set; }
        public float Elapsed { get; private set; }
        public float TimeLeft => data != null && data.durationSeconds > 0f ? Mathf.Max(0f, data.durationSeconds - Elapsed) : 0f;
        public IReadOnlyList<PlayerSlot> Players => players;
        public IReadOnlyList<PlayerAvatar> Avatars => avatars;
        public IReadOnlyList<RaftController> Rafts => rafts;
        public IReadOnlyList<int> Scores => scores;
        // Puesto de cada jugador al terminar (1 = mejor; empates compartidos).
        public int[] Ranks { get; private set; }
        public int CountdownValue => countdown.Value;
        public PartyHud Hud => hud;

        public event Action<PartyState> StateChanged;
        public event Action<int> CountdownTick;
        public event Action ScoresChanged;
        // Texto grande del centro: clave y argumentos.
        public event Action<string, object[]> Announced;

        public void Configure(PartyGameData gameData, RaftSpawner raftSpawner, BalanceConfig balanceConfig)
        {
            data = gameData;
            spawner = raftSpawner;
            balance = balanceConfig;
        }

        public void UseData(PartyGameData gameData) => data = gameData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            AutoBegin = true;
        }

        protected virtual void Awake()
        {
            Current = this;
        }

        private void Start()
        {
            if (!begun && AutoBegin)
                Begin(PartyCup.BuildRoster(Services.IsReady ? Services.Session.Players : Array.Empty<PlayerSlot>(), data));
        }

        protected virtual void OnDestroy()
        {
            if (BotBrain.Goal == (IBotGoal)this)
                BotBrain.Goal = null;
            if (Current == this)
                Current = null;
        }

        // Crea balsas y animales y deja el minijuego en la introducción. Start lo llama con la sesión; las pruebas, a mano.
        public void Begin(IReadOnlyList<PlayerSlot> roster)
        {
            begun = true;
            PartyCup.Current = data;
            players.Clear();
            players.AddRange(roster);
            scores = new int[players.Count];
            avatars.Clear();
            for (int i = 0; i < players.Count; i++)
                avatars.Add(null);
            BotBrain.Goal = this;
            Spawn();
            SetLocked(true);
            hud = new GameObject("PartyHud").AddComponent<PartyHud>();
            hud.transform.SetParent(transform, false);
            hud.Bind(this);
            if (Services.IsReady && Services.Music != null)
                Services.Music.Play("mus_party");
            stateTimer = introSeconds;
            SetState(PartyState.Intro);
        }

        // ---------------------------------------------------------------- reglas de cada minijuego
        protected abstract void Spawn();
        protected virtual void OnPlay() { }
        protected virtual void Tick(float deltaTime) { }
        protected virtual void TimeUp() => Finish();
        protected virtual int[] ComputeRanks() => PartyCup.Rank(scores);

        // Nombre del ganador (o "¡Empate!") en el centro de la pantalla.
        protected virtual void AnnounceWinner()
        {
            int winners = 0, first = -1;
            for (int i = 0; i < Ranks.Length; i++)
            {
                if (Ranks[i] != 1)
                    continue;
                winners++;
                if (first < 0)
                    first = i;
            }
            if (winners == 1)
                Announce("party.winner", PlayerName(players[first]));
            else
                Announce("party.draw");
        }

        // ---------------------------------------------------------------- HUD
        // Texto de arriba: por defecto el tiempo que queda.
        public virtual void GetStatus(out string key, out object[] args, out string literal)
        {
            key = null;
            args = null;
            literal = data != null && data.durationSeconds > 0f ? PartyHud.Clock(TimeLeft) : string.Empty;
        }

        // Marcador: por defecto una entrada por jugador con su puntaje.
        public virtual void FillBoard(List<PartyHud.Entry> board)
        {
            for (int i = 0; i < players.Count; i++)
                board.Add(new PartyHud.Entry { Color = players[i].Color, Label = PlayerName(players[i]), Value = scores[i].ToString() });
        }

        // Resultado del jugador en la tabla final (goles, rondas, mangos).
        public virtual string ResultText(int player) => scores[player].ToString();

        public virtual bool TryGetHeading(BotBrain bot, Vector3 raftPosition, out Vector3 heading)
        {
            heading = Vector3.zero;
            return false;
        }

        public virtual bool Drive(BotBrain bot, ref PlayerCommand command) => false;

        // ---------------------------------------------------------------- ciclo
        private void Update()
        {
            if (!begun)
                return;
            switch (State)
            {
                case PartyState.Intro:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        countdown.Start(COUNTDOWN_FROM, balance != null ? balance.pingCountdownStep : 0.6f);
                        SetState(PartyState.Countdown);
                        CountdownTick?.Invoke(countdown.Value);
                    }
                    break;
                case PartyState.Countdown:
                    if (countdown.Advance(Time.deltaTime))
                    {
                        CountdownTick?.Invoke(countdown.Value);
                        if (countdown.Value <= 0)
                        {
                            countdown.Stop();
                            SetLocked(false);
                            SetState(PartyState.Playing);
                            OnPlay();
                        }
                    }
                    break;
                case PartyState.Playing:
                    Elapsed += Time.deltaTime;
                    if (data.durationSeconds > 0f && Elapsed >= data.durationSeconds)
                        TimeUp();
                    else
                        Tick(Time.deltaTime);
                    break;
                case PartyState.Over:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        SetState(PartyState.Results);
                    break;
            }
        }

        // Fin del minijuego: puestos, puntos de la Fiesta y logro "Animal de Fiesta" (docs/06 §5).
        protected void Finish()
        {
            if (State == PartyState.Over || State == PartyState.Results)
                return;
            Ranks = ComputeRanks();
            PartyCup.Award(players, Ranks);
            if (Services.IsReady && Services.Achievements != null)
                Services.Achievements.RecordPartyGame(data.id);
            SetLocked(true);
            stateTimer = overSeconds;
            SetState(PartyState.Over);
            AnnounceWinner();
        }

        // ---------------------------------------------------------------- ayudas
        protected void SetState(PartyState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        protected void SetLocked(bool locked)
        {
            for (int i = 0; i < avatars.Count; i++)
            {
                if (avatars[i] != null)
                    avatars[i].InputLocked = locked;
            }
        }

        protected void AddScore(int player, int amount)
        {
            if (player < 0 || player >= scores.Length)
                return;
            scores[player] += amount;
            ScoresChanged?.Invoke();
        }

        protected void NotifyScores() => ScoresChanged?.Invoke();

        protected void Announce(string key, params object[] args) => Announced?.Invoke(key, args);

        public int IndexOf(PlayerAvatar avatar)
        {
            for (int i = 0; i < avatars.Count; i++)
            {
                if (avatars[i] == avatar)
                    return i;
            }
            return -1;
        }

        // Una balsa con los jugadores pedidos (índices de players). radius > 0 la vuelve mini balsa individual.
        protected RaftController SpawnRaft(Vector3 position, Quaternion rotation, IReadOnlyList<int> crew, float radius = 0f, GameObject miniModel = null)
        {
            crewBuffer.Clear();
            for (int i = 0; i < crew.Count; i++)
                crewBuffer.Add(players[crew[i]]);
            spawnBuffer.Clear();
            var raft = spawner.SpawnExtra(position, rotation, crewBuffer, spawnBuffer, radius <= 0f);
            if (radius > 0f)
                MiniRaft.Shrink(raft, radius, miniModel);
            for (int i = 0; i < crew.Count && i < spawnBuffer.Count; i++)
                avatars[crew[i]] = spawnBuffer[i];
            rafts.Add(raft);
            return raft;
        }

        // Lleva una balsa a un punto, quieta, con todos sus animales sentados y el gas lleno.
        protected static void ResetRaft(RaftController raft, Vector3 position, Quaternion rotation)
        {
            var body = raft.Body;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = position;
            body.rotation = rotation;
            raft.transform.SetPositionAndRotation(position, rotation);
            var crew = raft.GetComponent<RaftSeats>().Crew;
            for (int i = 0; i < crew.Count; i++)
            {
                var fall = crew[i].GetComponent<AvatarFall>();
                if (fall.IsOverboard)
                    fall.ReturnToRaft();
                crew[i].Avatar.Gas.SetFraction(1f);
            }
        }

        public static string PlayerName(PlayerSlot slot)
        {
            var table = UI.LocalizedText.Table;
            if (slot.Character != null)
                return table != null ? table.Get(slot.Character.NameKey) : slot.Character.displayName;
            return table != null ? table.Format("hud.player_tag", slot.Index + 1) : "J" + (slot.Index + 1);
        }

        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
