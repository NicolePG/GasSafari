using System.Collections.Generic;
using GasSafari.Audio;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Coco Caliente (docs/06 §1): todos en una balsa compartida; uno tiene el coco-bomba y, al tirarse un pedo, el coco pasa
    // al animal que tenga más cerca. Cuando se acaba la mecha explota y lanza al agua a quien lo tiene: queda fuera. Rondas
    // hasta que quede uno. Provisional: mecha al azar entre fuseMin y fuseMax s por ronda, quien recibe el coco espera
    // passCooldown antes de pasarlo, y cada ronda empieza con el coco en un jugador al azar de los que siguen.
    public class CocoCalienteGame : PartyGameBase
    {
        private const float BETWEEN_ROUNDS = 3f;
        private const float HIDE_AFTER_FALL = 1f;
        private const float BLAST_LIFT = 4f;

        [SerializeField] private Transform center;
        [SerializeField] private HotCoconut coconut;

        private bool[] isOut = new bool[0];
        private float[] hideTimer = new float[0];
        private float[] botDelay = new float[0];
        private readonly List<int> everyone = new List<int>();
        private readonly List<int> eliminated = new List<int>();
        private RaftController raft;
        private int holder = -1;
        private int round;
        private float fuse;
        private float fuseLength;
        private float holdTime;
        private float betweenTimer;

        public RaftController Raft => raft;
        public HotCoconut Coconut => coconut;
        public int Holder => holder;
        public int Round => round;
        public int Passes { get; private set; }
        public float FuseLeft => fuse;
        public bool BetweenRounds => betweenTimer > 0f;
        public bool IsOut(int player) => isOut[player];
        // Orden en que fueron cayendo (el primero quedó último).
        public IReadOnlyList<int> Eliminated => eliminated;

        public void ConfigureArena(Transform poolCenter, HotCoconut bomb)
        {
            center = poolCenter;
            coconut = bomb;
        }

        private void OnEnable() => GameEvents.OnFart += OnFart;
        private void OnDisable() => GameEvents.OnFart -= OnFart;

        protected override void Spawn()
        {
            int n = players.Count;
            isOut = new bool[n];
            hideTimer = new float[n];
            botDelay = new float[n];
            everyone.Clear();
            for (int i = 0; i < n; i++)
                everyone.Add(i);
            raft = SpawnRaft(center.position, center.rotation, everyone);
            // Sin giro: el resorte solo la devuelve al centro de la piscina.
            raft.gameObject.AddComponent<RaftSpinner>().Setup(0f, center.position);
            coconut.gameObject.SetActive(false);
        }

        protected override void OnPlay() => StartRound();

        private void StartRound()
        {
            round++;
            fuseLength = Random.Range(data.fuseMinSeconds, data.fuseMaxSeconds);
            fuse = fuseLength;
            SpreadSeats();
            var alive = new List<int>();
            for (int i = 0; i < isOut.Length; i++)
            {
                if (!isOut[i])
                    alive.Add(i);
            }
            coconut.gameObject.SetActive(true);
            GiveTo(alive[Random.Range(0, alive.Count)], false);
            Announce("party.round", round);
        }

        // Todos los que siguen, repartidos por igual en el borde (como en Rey de la Balsa).
        private void SpreadSeats()
        {
            var seats = raft.GetComponent<RaftSeats>();
            int alive = 0;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (isOut[i] || avatars[i] == null)
                    continue;
                alive++;
                var seat = avatars[i].GetComponent<PlayerSeat>();
                if (seat.IsSeated)
                    seat.Leave();
            }
            int k = 0;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (isOut[i] || avatars[i] == null)
                    continue;
                avatars[i].GetComponent<PlayerSeat>().Sit(seats, 180f + k * 360f / alive);
                k++;
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
            if (holder < 0)
                return;
            holdTime += deltaTime;
            fuse -= deltaTime;
            coconut.SetFuse(fuse / Mathf.Max(0.01f, fuseLength), fuse);
            if (fuse <= 0f)
                Explode();
        }

        // ---------------------------------------------------------------- pasar el coco
        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            if (State != PartyState.Playing || betweenTimer > 0f || holder < 0)
                return;
            if (IndexOf(avatar) != holder || holdTime < data.passCooldown || !avatar.GetComponent<PlayerSeat>().IsSeated)
                return;
            int target = Nearest(holder);
            if (target >= 0)
                GiveTo(target, true);
        }

        // El animal sentado más cercano a 'from' que sigue en la ronda.
        private int Nearest(int from)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            var origin = avatars[from].transform.position;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (i == from || isOut[i] || avatars[i] == null || !avatars[i].GetComponent<PlayerSeat>().IsSeated)
                    continue;
                float d = Flat(avatars[i].transform.position - origin).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }
            return best;
        }

        private void GiveTo(int player, bool thrown)
        {
            holder = player;
            holdTime = 0f;
            botDelay[player] = -1f;
            if (thrown)
            {
                Passes++;
                if (Services.IsReady && Services.Audio != null)
                    Services.Audio.Play(SoundId.PickupCoconut, avatars[player].transform.position);
            }
            coconut.Follow(avatars[player].transform, thrown);
            NotifyScores();
        }

        private void Explode()
        {
            int victim = holder;
            holder = -1;
            var avatar = avatars[victim];
            var position = coconut.transform.position;
            coconut.Explode();
            if (Services.IsReady)
            {
                if (Services.Audio != null)
                {
                    Services.Audio.Play(SoundId.RaftHitHard, position);
                    Services.Audio.Play(SoundId.WaterSplashBig, position);
                }
                if (Services.Vfx != null)
                    Services.Vfx.Play(VFX.VfxId.FartMega, position, Quaternion.identity, new Color(1f, 0.55f, 0.15f));
            }
            var away = Flat(avatar.transform.position - raft.transform.position);
            if (away.sqrMagnitude < 0.01f)
                away = Vector3.forward;
            var fall = avatar.GetComponent<AvatarFall>();
            if (!fall.IsOverboard)
                fall.Fall(away.normalized * data.blastSpeed + Vector3.up * BLAST_LIFT);
            isOut[victim] = true;
            avatar.InputLocked = true;
            hideTimer[victim] = HIDE_AFTER_FALL;
            eliminated.Add(victim);
            scores[victim] = round - 1;
            NotifyScores();

            int alive = 0, last = -1;
            for (int i = 0; i < isOut.Length; i++)
            {
                if (isOut[i])
                    continue;
                alive++;
                last = i;
            }
            Announce("party.coconut_out", PlayerName(players[victim]));
            if (alive <= 1)
            {
                if (last >= 0)
                    scores[last] = round;
                Finish();
                return;
            }
            betweenTimer = BETWEEN_ROUNDS;
            SetLocked(true);
        }

        private void Hide(int player)
        {
            var avatar = avatars[player];
            if (avatar == null || !avatar.gameObject.activeSelf)
                return;
            if (avatar.GetComponent<PlayerSeat>().IsSeated)
                avatar.GetComponent<AvatarFall>().Fall(Vector3.up);
            avatar.gameObject.SetActive(false);
        }

        // Los que siguen vuelven a la balsa, en el centro y con el gas lleno; los eliminados no vuelven.
        private void ResetRound()
        {
            for (int i = 0; i < hideTimer.Length; i++)
            {
                if (isOut[i] && hideTimer[i] > 0f)
                {
                    hideTimer[i] = 0f;
                    Hide(i);
                }
            }
            // No se usa ResetRaft: devolvería a la balsa también a los eliminados.
            var body = raft.Body;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = center.position;
            body.rotation = center.rotation;
            raft.transform.SetPositionAndRotation(center.position, center.rotation);
            for (int i = 0; i < avatars.Count; i++)
            {
                if (isOut[i] || avatars[i] == null)
                    continue;
                var fall = avatars[i].GetComponent<AvatarFall>();
                if (fall.IsOverboard)
                    fall.ReturnToRaft();
                avatars[i].Gas.SetFraction(1f);
            }
        }

        // Puntaje = rondas que aguantó; el último en pie, una más. Los eliminados en la misma ronda no existen (uno por ronda).
        protected override int[] ComputeRanks() => PartyCup.Rank(scores);

        // ---------------------------------------------------------------- HUD
        // Arriba la ronda; quién tiene el coco se ve en el marcador ("¡Coco!") y sobre su cabeza.
        public override void GetStatus(out string key, out object[] args, out string literal)
        {
            key = "party.round";
            args = new object[] { Mathf.Max(1, round) };
            literal = null;
        }

        public override void FillBoard(List<PartyHud.Entry> board)
        {
            var table = UI.LocalizedText.Table;
            for (int i = 0; i < players.Count; i++)
            {
                var color = players[i].Color;
                if (isOut.Length > i && isOut[i])
                    color.a = 0.3f;
                string value = i == holder ? (table != null ? table.Get("party.coconut_tag") : "¡Coco!") : string.Empty;
                board.Add(new PartyHud.Entry { Color = color, Label = PlayerName(players[i]), Value = value });
            }
        }

        public override string ResultText(int player)
        {
            var table = UI.LocalizedText.Table;
            return table != null ? table.Format("party.rounds_survived", scores[player]) : scores[player].ToString();
        }

        // ---------------------------------------------------------------- bots
        // Con el coco se acercan al más cercano y, tras una reacción de 0.3-1 s, se tiran el pedo; sin el coco se alejan
        // de quien lo tiene por el borde de la balsa.
        public override bool Drive(BotBrain bot, ref PlayerCommand command)
        {
            int me = IndexOf(bot.Avatar);
            var seat = bot.Seat;
            if (me < 0 || !seat.IsSeated)
                return false;
            command.Aim = 0f;
            command.FartHeld = false;
            command.HoldHeld = false;
            command.GrabHeld = false;
            command.Swim = Vector2.zero;
            command.SeatMove = 0f;
            if (holder < 0 || betweenTimer > 0f || isOut[me])
                return true;
            float dt = bot.Config.decideInterval;
            float step = balance.seatMoveSpeed * dt;
            if (me == holder)
            {
                if (holdTime >= data.passCooldown)
                {
                    if (botDelay[me] < 0f)
                        botDelay[me] = Random.Range(0.3f, 1f);
                    botDelay[me] -= dt;
                    if (botDelay[me] <= 0f && bot.Gas.Gas >= balance.puffCost && !bot.Thruster.IsCoughing)
                    {
                        bot.Source.PressFart();
                        botDelay[me] = Random.Range(0.3f, 0.6f);
                    }
                }
                int target = Nearest(me);
                if (target >= 0)
                {
                    float error = Mathf.DeltaAngle(seat.Angle, avatars[target].GetComponent<PlayerSeat>().Angle);
                    command.SeatMove = -Mathf.Clamp(error / step, -1f, 1f);
                }
                return true;
            }
            var holderSeat = avatars[holder].GetComponent<PlayerSeat>();
            if (holderSeat.IsSeated)
            {
                float away = Mathf.DeltaAngle(seat.Angle, holderSeat.Angle + 180f);
                command.SeatMove = -Mathf.Clamp(away / step, -1f, 1f);
            }
            return true;
        }
    }
}
