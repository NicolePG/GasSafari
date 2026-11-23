using System.Collections.Generic;
using GasSafari.Bots;
using UnityEngine;

namespace GasSafari.Modes
{
    // Balsa-Gol (docs/06 §1): 2 balsas (1 vs 1 o 2 vs 2) empujan una pelota de playa gigante a la portería rival.
    // 3 min o 5 goles. Equipos: J1 y J3 en la balsa azul, J2 y J4 en la naranja. Empate si el tiempo acaba igualado.
    public class BalsaGolGame : PartyGameBase
    {
        public static readonly Color[] TeamColors = { new Color(0.2f, 0.55f, 1f), new Color(1f, 0.55f, 0.12f) };
        private static readonly string[] TeamKeys = { "party.team.blue", "party.team.orange" };

        [Tooltip("Salida de cada equipo, mirando al centro")]
        [SerializeField] private Transform[] teamSpawns = new Transform[2];
        [SerializeField] private Transform ballSpawn;
        [SerializeField] private PartyBall ball;
        [SerializeField] private GoalZone[] goals = new GoalZone[2];
        [Tooltip("Color de cada equipo para la bandera de su balsa")]
        [SerializeField] private Material[] teamMaterials = new Material[2];

        private readonly int[] goalsFor = new int[2];
        private readonly List<int> crew = new List<int>();
        private readonly Raft.RaftController[] teamRafts = new Raft.RaftController[2];
        private int[] teamOf = new int[0];
        private float goalPause;
        private float stallTimer;

        public PartyBall Ball => ball;
        public int GoalsOf(int team) => goalsFor[team];
        public int TeamOf(int player) => teamOf[player];
        public Raft.RaftController TeamRaft(int team) => teamRafts[team];
        public bool IsGoalPause => goalPause > 0f;
        public int NeutralDrops { get; private set; }

        public void ConfigureArena(Transform[] spawns, Transform ballStart, PartyBall partyBall, GoalZone[] goalZones, Material[] materials)
        {
            teamMaterials = materials;
            teamSpawns = spawns;
            ballSpawn = ballStart;
            ball = partyBall;
            goals = goalZones;
        }

        private void OnEnable()
        {
            foreach (var goal in goals)
            {
                if (goal != null)
                    goal.BallEntered += OnGoal;
            }
        }

        private void OnDisable()
        {
            foreach (var goal in goals)
            {
                if (goal != null)
                    goal.BallEntered -= OnGoal;
            }
        }

        protected override void Spawn()
        {
            teamOf = new int[players.Count];
            for (int i = 0; i < players.Count; i++)
                teamOf[i] = i % 2;
            for (int team = 0; team < 2; team++)
            {
                crew.Clear();
                for (int i = 0; i < players.Count; i++)
                {
                    if (teamOf[i] == team)
                        crew.Add(i);
                }
                teamRafts[team] = SpawnRaft(teamSpawns[team].position, teamSpawns[team].rotation, crew);
                if (team < teamMaterials.Length && teamMaterials[team] != null)
                    AddFlag(teamRafts[team].transform, teamMaterials[team]);
            }
            ball.Setup(data.ballRadius, data.ballMass);
            ball.ResetTo(ballSpawn.position);
        }

        // Bandera del color del equipo en el centro de la balsa (sin colisión): se distingue cuál es cuál desde la cámara.
        private static void AddFlag(Transform raft, Material material)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "TeamFlag";
            Destroy(pole.GetComponent<Collider>());
            pole.transform.SetParent(raft, false);
            pole.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = material;
            var cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloth.name = "Cloth";
            Destroy(cloth.GetComponent<Collider>());
            cloth.transform.SetParent(raft, false);
            cloth.transform.localPosition = new Vector3(0f, 1.95f, 0.45f);
            cloth.transform.localScale = new Vector3(0.05f, 0.55f, 0.85f);
            cloth.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void OnGoal(GoalZone zone)
        {
            if (State != PartyState.Playing || goalPause > 0f)
                return;
            int scorer = 1 - zone.Team;
            goalsFor[scorer]++;
            for (int i = 0; i < players.Count; i++)
                scores[i] = goalsFor[teamOf[i]];
            NotifyScores();
            Announce("party.goal_shout");
            if (data.targetScore > 0 && goalsFor[scorer] >= data.targetScore)
            {
                Finish();
                return;
            }
            goalPause = data.goalPauseSeconds;
            SetLocked(true);
        }

        protected override void Tick(float deltaTime)
        {
            if (goalPause <= 0f)
            {
                CheckStall(deltaTime);
                return;
            }
            goalPause -= deltaTime;
            if (goalPause > 0f)
                return;
            Kickoff();
            SetLocked(false);
        }

        // Provisional (docs/06 no lo define): pelota atascada contra un borde o un poste → vuelve al centro.
        private void CheckStall(float deltaTime)
        {
            stallTimer = ball.Body.velocity.magnitude < data.ballStallSpeed ? stallTimer + deltaTime : 0f;
            if (data.ballStallSeconds <= 0f || stallTimer < data.ballStallSeconds)
                return;
            stallTimer = 0f;
            NeutralDrops++;
            // Un poco corrida del centro: si no, las dos balsas vuelven a chocar de frente sobre ella.
            ball.ResetTo(ballSpawn.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-5f, 5f)));
            Announce("party.ball_reset");
        }

        // Saque: balsas en su salida y pelota al centro.
        private void Kickoff()
        {
            for (int team = 0; team < 2; team++)
                ResetRaft(teamRafts[team], teamSpawns[team].position, teamSpawns[team].rotation);
            ball.ResetTo(ballSpawn.position);
            stallTimer = 0f;
        }

        protected override void AnnounceWinner()
        {
            if (goalsFor[0] == goalsFor[1])
                Announce("party.draw");
            else
                Announce("party.winner", TeamName(goalsFor[0] > goalsFor[1] ? 0 : 1));
        }

        public static string TeamName(int team)
        {
            var table = UI.LocalizedText.Table;
            return table != null ? table.Get(TeamKeys[team]) : TeamKeys[team];
        }

        public override void FillBoard(List<PartyHud.Entry> board)
        {
            for (int team = 0; team < 2; team++)
                board.Add(new PartyHud.Entry { Color = TeamColors[team], Label = TeamName(team), Value = goalsFor[team].ToString() });
        }

        public override string ResultText(int player)
        {
            int team = teamOf[player];
            return goalsFor[team] + " - " + goalsFor[1 - team];
        }

        // ---------------------------------------------------------------- bots
        // Se colocan detrás de la pelota, en la línea hacia la portería rival, y empujan; si quedan del lado equivocado,
        // la rodean por el costado en que están para no meterla en la propia.
        public override bool TryGetHeading(BotBrain bot, Vector3 raftPosition, out Vector3 heading)
        {
            heading = Vector3.zero;
            int player = IndexOf(bot.Avatar);
            if (player < 0 || ball == null)
                return false;
            var target = BotTarget(teamOf[player], raftPosition, bot.Seat.Raft != null ? bot.Seat.Raft.Controller.Radius : 2f);
            heading = Flat(target - raftPosition);
            if (heading.sqrMagnitude < 0.01f)
                heading = Flat(ball.transform.position - raftPosition);
            heading.Normalize();
            return true;
        }

        public Vector3 BotTarget(int team, Vector3 raftPosition, float raftRadius)
        {
            var ballPosition = Flat(ball.transform.position);
            var position = Flat(raftPosition);
            var goal = Flat(goals[1 - team].transform.position);
            var toGoal = (goal - ballPosition).normalized;
            float reach = ball.Radius + raftRadius;
            // Rival pegado a la pelota del lado de la portería: empujar de frente acaba en empate de fuerzas; se la
            // saca de costado (60°) por el lado contrario al rival.
            var rival = teamRafts[1 - team] != null ? Flat(teamRafts[1 - team].transform.position) - ballPosition : Vector3.zero;
            if (rival.sqrMagnitude > 0f && rival.magnitude < reach + 2.5f && Vector3.Dot(rival, toGoal) > 0f)
            {
                float rivalSide = Mathf.Sign(Vector3.Cross(toGoal, rival).y);
                toGoal = Quaternion.AngleAxis(-rivalSide * 60f, Vector3.up) * toGoal;
            }
            var offset = position - ballPosition;
            float along = Vector3.Dot(offset, toGoal);
            var lateral = offset - toGoal * along;
            if (along < -reach * 0.5f)
            {
                // Detrás de la pelota: si está bien alineado, atraviesa la pelota hacia la portería.
                if (lateral.magnitude < reach * 0.6f)
                    return ballPosition + toGoal * 3f;
                return ballPosition - toGoal * (reach + 1.5f);
            }
            var side = Vector3.Cross(Vector3.up, toGoal);
            float sign = Vector3.Dot(offset, side) >= 0f ? 1f : -1f;
            return ballPosition - toGoal * (reach + 2f) + side * (sign * (reach + 2.5f));
        }
    }
}
