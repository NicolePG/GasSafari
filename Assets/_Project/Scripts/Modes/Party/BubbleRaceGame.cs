using System.Collections.Generic;
using GasSafari.Audio;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.PowerUps;
using UnityEngine;

namespace GasSafari.Modes
{
    // Carrera de Burbujas (docs/06 §1): cada jugador en su mini balsa individual con gaseosa infinita (flota 1 m sobre el
    // agua, docs/05 P05) da 3 vueltas a una pista ovalada con obstáculos. Provisional: al llegar el primero, los demás
    // tienen finishGraceSeconds; la carrera tiene un tope de raceLimitSeconds y, si se acaba, cuenta lo que avanzó cada uno.
    // Una mini balsa tiene un solo animal y su gas no alcanza para 3 vueltas a buen ritmo: hay frijoles (P01) sobre la
    // pista que reaparecen pickupRespawnSeconds después de recogerlos.
    public class BubbleRaceGame : PartyGameBase
    {
        private const float LOOK_AHEAD = 4.5f;
        private const float LOOK_PER_SPEED = 0.5f;
        private const float RESCUE_HEIGHT = 0.6f;

        [SerializeField] private StadiumTrack track = new StadiumTrack();
        [Tooltip("Metros de pista (s) de la línea de salida y meta")]
        [SerializeField] private float startS = 10f;
        [Tooltip("Salida de cada lugar (J1..J4) en la parrilla")]
        [SerializeField] private Transform[] spawns = new Transform[4];
        [Tooltip("Modelos MiniRaft_J1..J4")]
        [SerializeField] private GameObject[] miniModels = new GameObject[4];
        [Tooltip("Línea de carrera de los bots: (s, lat) en orden de s; esquiva los obstáculos")]
        [SerializeField] private Vector2[] racingLine = { new Vector2(0f, 0f) };
        [SerializeField] private GameObject beansPrefab;
        [Tooltip("Frijoles sobre la pista: (s, lat)")]
        [SerializeField] private Vector2[] beanSpots = new Vector2[0];

        private readonly List<PowerUpBase> beans = new List<PowerUpBase>();
        private readonly List<float> beanRespawn = new List<float>();

        private readonly List<int> single = new List<int>(1);
        private AvatarFall[] falls = new AvatarFall[0];
        private float[] lastS = new float[0];
        private float[] distance = new float[0];
        private int[] lapsDone = new int[0];
        private float[] finishTime = new float[0];
        private int[] place = new int[0];
        private float[] stuckMark = new float[0];
        private float[] stuckDistance = new float[0];
        private int finishers;
        private float graceEnd = -1f;
        private bool finalLapAnnounced;

        public StadiumTrack Track => track;
        public float StartS => startS;
        public int Rescues { get; private set; }
        public float Distance(int player) => distance[player];
        public int LapsDone(int player) => lapsDone[player];
        public bool HasFinished(int player) => finishTime[player] >= 0f;
        public float FinishTime(int player) => finishTime[player];
        public int Finishers => finishers;
        public float RaceLength => data.laps * track.Perimeter;

        public IReadOnlyList<PowerUpBase> Beans => beans;
        public int BeansTaken { get; private set; }

        public void ConfigureArena(StadiumTrack stadium, float startLine, Transform[] raftSpawns, GameObject[] models, Vector2[] line,
            GameObject beansPickup, Vector2[] beansOnTrack)
        {
            beansPrefab = beansPickup;
            beanSpots = beansOnTrack;
            track = stadium;
            startS = startLine;
            spawns = raftSpawns;
            miniModels = models;
            racingLine = line;
        }

        protected override void Spawn()
        {
            int n = players.Count;
            falls = new AvatarFall[n];
            lastS = new float[n];
            distance = new float[n];
            lapsDone = new int[n];
            finishTime = new float[n];
            place = new int[n];
            stuckMark = new float[n];
            stuckDistance = new float[n];
            for (int i = 0; i < n; i++)
            {
                int slot = Mathf.Clamp(players[i].Index, 0, spawns.Length - 1);
                single.Clear();
                single.Add(i);
                var raft = SpawnRaft(spawns[slot].position, spawns[slot].rotation, single, data.miniRaftRadius,
                    slot < miniModels.Length ? miniModels[slot] : null);
                MiniRaft.Tint(raft, players[i].Color);
                raft.Effects.Hover(data.bubbleHover, float.PositiveInfinity);
                falls[i] = avatars[i] != null ? avatars[i].GetComponent<AvatarFall>() : null;
                track.Project(spawns[slot].position, out lastS[i], out _);
                distance[i] = track.Delta(startS, lastS[i]);
                finishTime[i] = -1f;
            }
            if (beansPrefab == null)
                return;
            foreach (var spot in beanSpots)
            {
                var bean = Instantiate(beansPrefab, BeanPosition(spot), Quaternion.identity, transform).GetComponent<PowerUpBase>();
                beans.Add(bean);
                beanRespawn.Add(-1f);
            }
        }

        // A la altura de las balsas, que flotan sobre el agua.
        private Vector3 BeanPosition(Vector2 spot) => track.Point(spot.x, spot.y) + Vector3.up * (data.bubbleHover + 0.6f);

        private void OnEnable() => GameEvents.OnPowerUp += OnPowerUp;
        private void OnDisable() => GameEvents.OnPowerUp -= OnPowerUp;

        private void OnPowerUp(PowerUpData powerUp, PlayerAvatar taker)
        {
            if (IndexOf(taker) >= 0)
                BeansTaken++;
        }

        private void RespawnBeans()
        {
            for (int i = 0; i < beans.Count; i++)
            {
                if (!beans[i].IsCollected)
                    continue;
                if (beanRespawn[i] < 0f)
                    beanRespawn[i] = Elapsed + data.pickupRespawnSeconds;
                else if (Elapsed >= beanRespawn[i])
                {
                    beanRespawn[i] = -1f;
                    beans[i].Recycle(BeanPosition(beanSpots[i]));
                }
            }
        }

        protected override void Tick(float deltaTime)
        {
            for (int i = 0; i < rafts.Count; i++)
            {
                TrackProgress(i);
                ReturnFromWater(i);
                CheckStuck(i);
            }
            RespawnBeans();
            bool everyone = finishers >= players.Count;
            bool graceOver = graceEnd >= 0f && Elapsed >= graceEnd;
            bool limit = data.raceLimitSeconds > 0f && Elapsed >= data.raceLimitSeconds;
            if (everyone || graceOver || limit)
            {
                if (!everyone)
                    Announce("party.time_up");
                Finish();
            }
        }

        private void TrackProgress(int i)
        {
            track.Project(rafts[i].Body.position, out float s, out _);
            distance[i] += track.Delta(lastS[i], s);
            lastS[i] = s;
            if (HasFinished(i))
                return;
            int laps = Mathf.Max(0, Mathf.FloorToInt(distance[i] / track.Perimeter));
            if (laps <= lapsDone[i])
                return;
            lapsDone[i] = laps;
            NotifyScores();
            if (Services.IsReady && Services.Audio != null)
                Services.Audio.Play(SoundId.Checkpoint, rafts[i].Body.position);
            if (lapsDone[i] >= data.laps)
                CrossFinish(i);
            else if (lapsDone[i] == data.laps - 1 && !finalLapAnnounced)
            {
                finalLapAnnounced = true;
                Announce("party.final_lap");
            }
        }

        // Los que llegan siguen andando (las balsas quietas en la recta de meta tapaban a los demás).
        private void CrossFinish(int i)
        {
            finishTime[i] = Elapsed;
            place[i] = ++finishers;
            if (finishers == 1 && players.Count > 1)
            {
                graceEnd = Elapsed + data.finishGraceSeconds;
                Announce("party.first_finish", PlayerName(players[i]), Mathf.RoundToInt(data.finishGraceSeconds));
            }
        }

        // Provisional: la burbuja devuelve al animal a su mini balsa (que flota a 1 m, no se puede trepar nadando).
        private void ReturnFromWater(int i)
        {
            var fall = falls[i];
            if (fall != null && fall.IsOverboard && fall.TimeOverboard >= data.overboardReturnSeconds)
                fall.ReturnToRaft();
        }

        // Provisional: una balsa trabada contra un obstáculo o al revés vuelve a la línea de carrera donde estaba.
        private void CheckStuck(int i)
        {
            if (Elapsed - stuckMark[i] < data.stuckSeconds)
                return;
            if (distance[i] - stuckDistance[i] < 1f && !HasFinished(i))
            {
                track.Project(rafts[i].Body.position, out float s, out _);
                var point = track.Point(s, LineLat(s)) + Vector3.up * RESCUE_HEIGHT;
                ResetRaft(rafts[i], point, Quaternion.LookRotation(track.Direction(s)));
                Rescues++;
            }
            stuckMark[i] = Elapsed;
            stuckDistance[i] = distance[i];
        }

        // Los que llegaron, por orden de llegada; después, los que avanzaron más.
        protected override int[] ComputeRanks()
        {
            int n = players.Count;
            var ranks = new int[n];
            for (int i = 0; i < n; i++)
            {
                int better = 0;
                for (int j = 0; j < n; j++)
                    better += Ahead(j, i) ? 1 : 0;
                ranks[i] = better + 1;
            }
            return ranks;
        }

        private bool Ahead(int a, int b)
        {
            if (HasFinished(a) != HasFinished(b))
                return HasFinished(a);
            if (HasFinished(a))
                return finishTime[a] < finishTime[b];
            return distance[a] > distance[b];
        }

        // Desplazamiento lateral de la línea de carrera en 's' (interpolado, la vuelta se cierra).
        public float LineLat(float s)
        {
            if (racingLine == null || racingLine.Length == 0)
                return 0f;
            s = track.Wrap(s);
            int n = racingLine.Length;
            for (int k = 0; k < n; k++)
            {
                var from = racingLine[k];
                var to = k + 1 < n ? racingLine[k + 1] : new Vector2(racingLine[0].x + track.Perimeter, racingLine[0].y);
                if (s >= from.x && s < to.x)
                    return Mathf.Lerp(from.y, to.y, (s - from.x) / Mathf.Max(0.01f, to.x - from.x));
            }
            var first = racingLine[0];
            var last = racingLine[n - 1];
            float span = first.x + track.Perimeter - last.x;
            return Mathf.Lerp(last.y, first.y, (s + track.Perimeter - last.x) / Mathf.Max(0.01f, span) % 1f);
        }

        // ---------------------------------------------------------------- HUD
        public override void GetStatus(out string key, out object[] args, out string literal)
        {
            key = null;
            args = null;
            literal = graceEnd >= 0f ? PartyHud.Clock(Mathf.Max(0f, graceEnd - Elapsed)) : PartyHud.Clock(Elapsed);
        }

        public override void FillBoard(List<PartyHud.Entry> board)
        {
            for (int i = 0; i < players.Count; i++)
                board.Add(new PartyHud.Entry { Color = players[i].Color, Label = PlayerName(players[i]), Value = Progress(i) });
        }

        public override string ResultText(int player)
        {
            if (!HasFinished(player))
                return Progress(player);
            float t = finishTime[player];
            int minutes = Mathf.FloorToInt(t / 60f);
            return minutes + ":" + (t - minutes * 60f).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string Progress(int i)
        {
            var table = UI.LocalizedText.Table;
            if (HasFinished(i))
                return table != null ? table.Format("party.rank", place[i]) : place[i] + ".º";
            int lap = Mathf.Clamp(lapsDone.Length > i ? lapsDone[i] + 1 : 1, 1, data.laps);
            return lap + "/" + data.laps;
        }

        // ---------------------------------------------------------------- bots
        // Siguen la línea de carrera unos metros por delante (más lejos cuanto más rápido van).
        public override bool TryGetHeading(BotBrain bot, Vector3 raftPosition, out Vector3 heading)
        {
            heading = Vector3.zero;
            if (IndexOf(bot.Avatar) < 0 || bot.Seat.Raft == null)
                return false;
            float speed = Flat(bot.Seat.Raft.Controller.Body.velocity).magnitude;
            track.Project(raftPosition, out float s, out _);
            float ahead = s + LOOK_AHEAD + speed * LOOK_PER_SPEED;
            heading = Flat(track.Point(ahead, LineLat(ahead)) - raftPosition);
            if (heading.sqrMagnitude < 0.01f)
                heading = track.Direction(s);
            heading.Normalize();
            return true;
        }
    }
}
