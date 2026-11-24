using System;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Carrera por Equipos local (docs/06 §1): dos balsas en la misma pista, cada una con su equipo (TeamRaceSeries), salen
    // de sus TeamRaceSpawn (M7-12), chocan entre sí, comparten los power-ups y reaparecen en su propio checkpoint. Gana la
    // primera balsa en la meta; la serie es al mejor de 3. Pantalla dividida vertical: Azul a la izquierda, Naranja a la
    // derecha. Sin estrellas, récords, fantasma, frijolitos ni Ayuda Real; no cuenta como nivel terminado.
    // Tiene que engancharse antes del Start del LevelManager (LevelFlow lo hace al cargar la escena) porque crea las balsas.
    [DefaultExecutionOrder(150)]
    public class TeamRaceMode : GameModeBase
    {
        public const int BLUE = 0;
        public const int ORANGE = 1;
        public static readonly Color[] TeamColors = { new Color(0.22f, 0.52f, 1f), new Color(1f, 0.55f, 0.12f) };
        public static readonly string[] TeamKeys = { "party.team.blue", "party.team.orange" };

        // Separación de cada balsa al reaparecer en un checkpoint, si no hay salidas TeamRaceSpawn de donde sacarla (m).
        private const float FALLBACK_LATERAL = 3f;
        private const float DISTANCE_REFRESH = 0.2f;

        private readonly List<PlayerSlot> blueCrew = new List<PlayerSlot>();
        private readonly List<PlayerSlot> orangeCrew = new List<PlayerSlot>();
        private readonly List<PlayerAvatar> orangeAvatars = new List<PlayerAvatar>();
        private readonly RaftController[] rafts = new RaftController[TeamRaceSeries.TEAMS];
        private readonly float[] distances = new float[TeamRaceSeries.TEAMS];
        private readonly int[] hints = new int[TeamRaceSeries.TEAMS];
        private CheckpointSystem checkpoints;
        private FinishGate gate;
        private FlowSpline flow;
        private TeamSplitScreen split;
        private TeamRaceHud hud;
        private RaftFlowReceiver orangeFlow;
        private bool orangeFrozen = true;
        private bool ready;
        private float refreshTimer;

        public override string ModeId => ProgressionRules.MODE_TEAM_RACE;
        public override bool AllowsRoyalAssist => false;
        public override bool ShowsLevelResults => false;

        public IReadOnlyList<RaftController> Rafts => rafts;
        public IReadOnlyList<PlayerSlot> BlueCrew => blueCrew;
        public IReadOnlyList<PlayerSlot> OrangeCrew => orangeCrew;
        public IReadOnlyList<PlayerAvatar> OrangeAvatars => orangeAvatars;
        public TeamSplitScreen Split => split;
        public TeamRaceHud Hud => hud;
        public CheckpointSystem Checkpoints => checkpoints;
        // Equipo ganador de esta carrera (-1 mientras se corre) y su tiempo.
        public int Winner { get; private set; } = -1;
        public float WinnerTime { get; private set; }
        public bool IsReady => ready;

        public event Action<int> RaceWon;

        // Metros recorridos por la balsa de ese equipo sobre el cauce, y quién va adelante.
        public float Distance(int team) => team >= 0 && team < distances.Length ? distances[team] : 0f;
        public int Leader => distances[ORANGE] > distances[BLUE] ? ORANGE : BLUE;
        public float CourseLength => flow != null ? flow.Length : 0f;

        public int TeamOf(RaftController raft)
        {
            for (int i = 0; i < rafts.Length; i++)
            {
                if (rafts[i] != null && rafts[i] == raft)
                    return i;
            }
            return -1;
        }

        protected override void OnLevelLoaded()
        {
            var level = Level;
            if (level.HasBegun)
            {
                Debug.LogError("TeamRaceMode: el nivel ya creó su balsa; el modo se engancha antes del Start del LevelManager");
                return;
            }
            var session = Services.IsReady ? Services.Session.Players : (IReadOnlyList<PlayerSlot>)Array.Empty<PlayerSlot>();
            TeamRaceSeries.BuildCrews(session, blueCrew, orangeCrew);
            Lanes(out var bluePose, out var orangePose, out float lateral);

            level.CountsAsLevelFinish = false;
            level.Spawner.SetStartPose(bluePose.position, bluePose.rotation);
            level.Begin(blueCrew);
            rafts[BLUE] = level.Raft;
            orangeAvatars.Clear();
            rafts[ORANGE] = level.Spawner.SpawnExtra(orangePose.position, orangePose.rotation, orangeCrew, orangeAvatars);
            orangeFlow = rafts[ORANGE].GetComponent<RaftFlowReceiver>();
            SetOrangeFrozen(true);

            checkpoints = FindFirstObjectByType<CheckpointSystem>();
            if (checkpoints != null)
            {
                checkpoints.BindRaft(rafts[BLUE]);
                checkpoints.AddRacer(rafts[ORANGE]);
                checkpoints.SetLane(rafts[BLUE], bluePose.position, bluePose.rotation, -lateral);
                checkpoints.SetLane(rafts[ORANGE], orangePose.position, orangePose.rotation, lateral);
                checkpoints.ApplyPenalty = false;
            }
            gate = FindFirstObjectByType<FinishGate>();
            if (gate != null)
                gate.Crossed += OnCrossed;
            flow = FindFirstObjectByType<FlowSpline>();
            level.PausedChanged += OnPausedChanged;
            // Repetir desde la pausa vuelve a correr esta pista sin ganador.
            TeamRaceSeries.ForgetCurrent();
            ready = true;
        }

        // Pose de salida de cada balsa (sus TeamRaceSpawn o, si faltan, a los lados de la puerta) y la separación lateral.
        private static void Lanes(out Pose blue, out Pose orange, out float lateral)
        {
            var spawns = TeamRaceSpawn.FindAll();
            if (spawns[BLUE] != null && spawns[ORANGE] != null)
            {
                blue = new Pose(spawns[BLUE].Position, spawns[BLUE].Rotation);
                orange = new Pose(spawns[ORANGE].Position, spawns[ORANGE].Rotation);
                lateral = Vector3.Distance(blue.position, orange.position) * 0.5f;
                return;
            }
            Debug.LogWarning("TeamRaceMode: el nivel no tiene sus 2 TeamRaceSpawn (M7-12); salen a los lados de la puerta");
            var gateStart = FindFirstObjectByType<StartGate>();
            var position = gateStart != null ? gateStart.Position : Vector3.zero;
            var rotation = gateStart != null ? gateStart.Rotation : Quaternion.identity;
            var right = rotation * Vector3.right;
            lateral = FALLBACK_LATERAL;
            blue = new Pose(position - right * lateral, rotation);
            orange = new Pose(position + right * lateral, rotation);
        }

        private void Start()
        {
            if (!ready)
                return;
            split = gameObject.AddComponent<TeamSplitScreen>();
            split.Build(rafts[BLUE], rafts[ORANGE]);
            SetupLevelHud();
            hud = new GameObject("TeamRaceHud").AddComponent<TeamRaceHud>();
            hud.transform.SetParent(transform, false);
            hud.Bind(this);
            RefreshDistances();
        }

        // El HUD del nivel muestra a los 4 animales (cada equipo en su mitad) y esconde lo que es de una sola balsa.
        private void SetupLevelHud()
        {
            var levelHud = FindFirstObjectByType<UI.LevelHud>();
            if (levelHud != null)
            {
                var avatars = new List<PlayerAvatar>();
                var places = new List<int>();
                var numbers = new List<int>();
                AddPanels(Level.Avatars, blueCrew, 0, avatars, places, numbers);
                AddPanels(orangeAvatars, orangeCrew, 1, avatars, places, numbers);
                levelHud.ShowPlayers(avatars, places, numbers);
                if (levelHud.Chambers != null)
                    levelHud.Chambers.gameObject.SetActive(false);
                if (levelHud.Progress != null)
                    levelHud.Progress.gameObject.SetActive(false);
            }
            // Las flechas de peligro se calculan sobre la pantalla entera (provisional, docs/06).
            foreach (var arrows in FindObjectsByType<UI.HudHazardArrows>(FindObjectsSortMode.None))
                arrows.gameObject.SetActive(false);
        }

        private static void AddPanels(IReadOnlyList<PlayerAvatar> crew, List<PlayerSlot> slots, int column,
            List<PlayerAvatar> avatars, List<int> places, List<int> numbers)
        {
            for (int i = 0; i < crew.Count && i < slots.Count; i++)
            {
                avatars.Add(crew[i]);
                places.Add(column + 2 * i);
                numbers.Add(slots[i].Index + 1);
            }
        }

        protected override void OnExit()
        {
            if (gate != null)
                gate.Crossed -= OnCrossed;
            if (Level != null)
                Level.PausedChanged -= OnPausedChanged;
        }

        protected override void OnStateChanged(LevelState state)
        {
            switch (state)
            {
                case LevelState.Running:
                case LevelState.Respawning:
                    if (Winner < 0)
                        SetOrangeFrozen(false);
                    break;
                case LevelState.Finished:
                    SetOrangeFrozen(true);
                    break;
                case LevelState.Results:
                    if (hud != null)
                        hud.ShowResults();
                    break;
            }
        }

        private void OnPausedChanged(bool paused)
        {
            bool running = Level.State == LevelState.Running || Level.State == LevelState.Respawning;
            SetLocked(paused || !running || Winner >= 0);
        }

        private void SetOrangeFrozen(bool frozen)
        {
            orangeFrozen = frozen;
            SetLocked(frozen);
            if (orangeFlow != null)
                orangeFlow.enabled = !frozen;
        }

        private void SetLocked(bool locked)
        {
            for (int i = 0; i < orangeAvatars.Count; i++)
            {
                if (orangeAvatars[i] != null)
                    orangeAvatars[i].InputLocked = locked || orangeFrozen;
            }
        }

        // La primera balsa que cruza la meta gana la carrera; la meta de la Azul la cierra también el LevelManager.
        private void OnCrossed(RaftController raft)
        {
            int team = TeamOf(raft);
            if (team < 0 || Winner >= 0)
                return;
            if (Level.State != LevelState.Running && Level.State != LevelState.Respawning && Level.State != LevelState.Finished)
                return;
            Winner = team;
            WinnerTime = Level.Timer.Total;
            TeamRaceSeries.RecordRace(team, WinnerTime);
            SetOrangeFrozen(true);
            Level.EndRun();
            RaceWon?.Invoke(team);
        }

        private void Update()
        {
            if (!ready)
                return;
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
                return;
            refreshTimer = DISTANCE_REFRESH;
            RefreshDistances();
        }

        private void RefreshDistances()
        {
            if (flow == null)
                return;
            for (int i = 0; i < rafts.Length; i++)
            {
                if (rafts[i] != null)
                    distances[i] = flow.GetDistance(rafts[i].transform.position, ref hints[i]);
            }
        }

        // Sin progreso ni resultados del nivel: la serie lleva la cuenta.
        public override int PreviousBestStars(string levelId) => 0;

        public override void Commit(LevelData level, LevelResults results) { }
    }
}
