using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Rey de la Balsa (docs/06 §1): todos en una balsa que gira en una piscina redonda; cada pedo empuja al agua a los
    // animales a menos de 2.5 m (quien se agarra aguanta, docs/02 §7). El que cae queda fuera de la ronda. Gana la ronda el
    // último a bordo; mejor de 3. Provisional: una ronda dura como mucho roundSeconds (sin ganador) y hay maxRounds rondas.
    public class ReyBalsaGame : PartyGameBase
    {
        private const float BETWEEN_ROUNDS = 3f;
        private const float HIDE_AFTER_FALL = 1f;
        private const float PUSH_LIFT = 1.5f;

        [SerializeField] private Transform center;

        private bool[] isOut = new bool[0];
        private float[] hideTimer = new float[0];
        private float[] botGrab = new float[0];
        private float[] botCooldown = new float[0];
        private float[] botAim = new float[0];
        private readonly List<float> roundLengths = new List<float>();
        private readonly List<int> everyone = new List<int>();
        private RaftController raft;
        private int round;
        private float roundTime;
        private float betweenTimer;

        public RaftController Raft => raft;
        public int Round => round;
        public bool IsOut(int player) => isOut[player];
        public bool BetweenRounds => betweenTimer > 0f;
        public int RoundsPlayed { get; private set; }
        // Segundos de juego de cada ronda terminada (pruebas y ajuste).
        public IReadOnlyList<float> RoundLengths => roundLengths;

        public void ConfigureArena(Transform poolCenter) => center = poolCenter;

        private void OnEnable()
        {
            GameEvents.OnFart += OnFart;
            GameEvents.OnPlayerFell += OnFell;
        }

        private void OnDisable()
        {
            GameEvents.OnFart -= OnFart;
            GameEvents.OnPlayerFell -= OnFell;
        }

        protected override void Spawn()
        {
            int n = players.Count;
            isOut = new bool[n];
            hideTimer = new float[n];
            botGrab = new float[n];
            botCooldown = new float[n];
            botAim = new float[n];
            everyone.Clear();
            for (int i = 0; i < n; i++)
                everyone.Add(i);
            raft = SpawnRaft(center.position, center.rotation, everyone);
            raft.gameObject.AddComponent<RaftSpinner>().Setup(data.spinDegreesPerSecond, center.position);
        }

        protected override void OnPlay() => StartRound();

        private void StartRound()
        {
            round++;
            roundTime = 0f;
            for (int i = 0; i < isOut.Length; i++)
            {
                isOut[i] = false;
                hideTimer[i] = 0f;
                botAim[i] = -1f;
            }
            SpreadSeats();
            Announce("party.round", round);
        }

        // Provisional: cada ronda empieza con todos repartidos por igual en el borde (con 4, a 90°: 2.8 m, fuera del
        // alcance de 2.5 m). Con las salidas de docs/02 (60° entre vecinos, 2 m) el primer pedo ya tiraba a alguien.
        private void SpreadSeats()
        {
            var seats = raft.GetComponent<RaftSeats>();
            int n = avatars.Count;
            for (int i = 0; i < n; i++)
            {
                var seat = avatars[i] != null ? avatars[i].GetComponent<PlayerSeat>() : null;
                if (seat != null && seat.IsSeated)
                    seat.Leave();
            }
            for (int i = 0; i < n; i++)
            {
                var seat = avatars[i] != null ? avatars[i].GetComponent<PlayerSeat>() : null;
                if (seat != null && !avatars[i].GetComponent<AvatarFall>().IsOverboard)
                    seat.Sit(seats, 180f + i * 360f / n);
            }
        }

        protected override void Tick(float deltaTime)
        {
            for (int i = 0; i < hideTimer.Length; i++)
            {
                if (hideTimer[i] <= 0f)
                    continue;
                hideTimer[i] -= deltaTime;
                if (hideTimer[i] <= 0f)
                    Hide(i);
            }
            if (betweenTimer > 0f)
            {
                betweenTimer -= deltaTime;
                if (betweenTimer <= 0f)
                {
                    ResetRound();
                    StartRound();
                    SetLocked(false);
                }
                return;
            }
            roundTime += deltaTime;
            if (data.roundSeconds > 0f && roundTime >= data.roundSeconds)
                EndRound(-1);
        }

        // ---------------------------------------------------------------- empujón por pedo
        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            if (State != PartyState.Playing || betweenTimer > 0f)
                return;
            int pusher = IndexOf(avatar);
            if (pusher < 0 || isOut[pusher] || !avatar.GetComponent<PlayerSeat>().IsSeated)
                return;
            var from = avatar.transform.position;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (i == pusher || isOut[i] || avatars[i] == null)
                    continue;
                var seat = avatars[i].GetComponent<PlayerSeat>();
                if (!seat.IsSeated || avatars[i].GetComponent<AvatarStateMachine>().IsGrabbing)
                    continue;
                var away = Flat(avatars[i].transform.position - from);
                if (away.magnitude > data.pushRadius)
                    continue;
                if (away.sqrMagnitude < 0.01f)
                    away = Flat(avatars[i].transform.position - raft.transform.position);
                avatars[i].GetComponent<AvatarFall>().Fall(away.normalized * data.pushSpeed + Vector3.up * PUSH_LIFT);
            }
        }

        private void OnFell(PlayerAvatar avatar)
        {
            if (State != PartyState.Playing || betweenTimer > 0f)
                return;
            int i = IndexOf(avatar);
            if (i < 0 || isOut[i] || State != PartyState.Playing)
                return;
            isOut[i] = true;
            avatar.InputLocked = true;
            hideTimer[i] = HIDE_AFTER_FALL;
            NotifyScores();

            int alive = 0, last = -1;
            for (int j = 0; j < isOut.Length; j++)
            {
                if (isOut[j])
                    continue;
                alive++;
                last = j;
            }
            if (alive <= 1)
                EndRound(alive == 1 ? last : -1);
        }

        // Fuera de la ronda: desaparece del agua para que no vuelva a subir (ni sola ni nadando).
        private void Hide(int player)
        {
            var avatar = avatars[player];
            if (avatar == null || !avatar.gameObject.activeSelf)
                return;
            if (avatar.GetComponent<PlayerSeat>().IsSeated)
                avatar.GetComponent<AvatarFall>().Fall(Vector3.up);
            avatar.gameObject.SetActive(false);
        }

        private void EndRound(int winner)
        {
            RoundsPlayed++;
            roundLengths.Add(roundTime);
            if (winner >= 0)
            {
                scores[winner]++;
                Announce("party.round_won", PlayerName(players[winner]));
            }
            else
            {
                Announce("party.round_draw");
            }
            NotifyScores();
            bool champion = winner >= 0 && scores[winner] >= data.roundsToWin;
            if (champion || (data.maxRounds > 0 && round >= data.maxRounds))
            {
                Finish();
                return;
            }
            betweenTimer = BETWEEN_ROUNDS;
            SetLocked(true);
        }

        // Todos de vuelta a la balsa, en el centro y con el gas lleno.
        private void ResetRound()
        {
            for (int i = 0; i < avatars.Count; i++)
            {
                if (avatars[i] != null && !avatars[i].gameObject.activeSelf)
                    avatars[i].gameObject.SetActive(true);
            }
            ResetRaft(raft, center.position, center.rotation);
        }

        // ---------------------------------------------------------------- HUD
        public override void GetStatus(out string key, out object[] args, out string literal)
        {
            key = "party.round";
            args = new object[] { Mathf.Max(1, round) };
            literal = null;
        }

        public override void FillBoard(List<PartyHud.Entry> board)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var color = players[i].Color;
                if (isOut.Length > i && isOut[i])
                    color.a = 0.3f;
                board.Add(new PartyHud.Entry { Color = color, Label = PlayerName(players[i]), Value = scores[i].ToString() });
            }
        }

        // ---------------------------------------------------------------- bots
        // Se acercan por el borde al rival más cercano y se tiran un pedo cuando lo tienen a tiro; si un rival está
        // cerca, a veces se agarran un momento (no pueden disparar mientras tanto).
        public override bool Drive(BotBrain bot, ref PlayerCommand command)
        {
            int me = IndexOf(bot.Avatar);
            var seat = bot.Seat;
            if (me < 0 || !seat.IsSeated)
                return false;
            command.Aim = 0f;
            command.FartHeld = false;
            command.Swim = Vector2.zero;
            float dt = bot.Config.decideInterval;
            botCooldown[me] -= dt;
            if (botGrab[me] > 0f)
            {
                botGrab[me] -= dt;
                command.GrabHeld = botGrab[me] > 0f;
                command.SeatMove = 0f;
                return true;
            }
            command.GrabHeld = false;
            command.HoldHeld = false;

            int target = -1;
            float best = float.MaxValue;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (i == me || isOut[i] || avatars[i] == null || !avatars[i].GetComponent<PlayerSeat>().IsSeated)
                    continue;
                float d = Flat(avatars[i].transform.position - bot.Avatar.transform.position).magnitude;
                if (d < best)
                {
                    best = d;
                    target = i;
                }
            }
            if (target < 0)
            {
                command.SeatMove = 0f;
                return true;
            }

            bool inRange = best <= data.pushRadius * 0.9f;
            // Reacción de bot (provisional): al tener a alguien a tiro tarda 0.8-1.8 s en tirarse el pedo y, mientras,
            // se agarra a ratos (20 % por decisión, 0.6-1.5 s) por si el otro dispara primero.
            if (!inRange)
                botAim[me] = -1f;
            else if (botAim[me] < 0f)
                botAim[me] = Random.Range(0.8f, 1.8f);
            else
                botAim[me] -= dt;
            if (inRange && botAim[me] <= 0f && botCooldown[me] <= 0f && bot.Gas.Gas >= balance.puffCost && !bot.Thruster.IsCoughing)
            {
                bot.Source.PressFart();
                botCooldown[me] = Random.Range(0.6f, 1.2f);
                botAim[me] = Random.Range(0.8f, 1.8f);
            }
            else if (inRange && Random.value < 0.2f)
            {
                botGrab[me] = Random.Range(0.6f, 1.5f);
            }

            float targetAngle = avatars[target].GetComponent<PlayerSeat>().Angle;
            float error = Mathf.DeltaAngle(seat.Angle, targetAngle);
            float step = balance.seatMoveSpeed * dt;
            // SeatMove positivo reduce el ángulo (PlayerSeat.Update).
            command.SeatMove = inRange ? 0f : -Mathf.Clamp(error / step, -1f, 1f);
            return true;
        }
    }
}
