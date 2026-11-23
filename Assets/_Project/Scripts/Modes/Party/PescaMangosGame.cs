using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.PowerUps;
using UnityEngine;

namespace GasSafari.Modes
{
    // Pesca de Mangos (docs/06 §1): cada jugador en su mini balsa individual; 90 s para recoger más mangos que nadie.
    // Los mangos son el power-up P03 de siempre (también dan gas); provisional: siempre hay mangosAtOnce flotando y cada
    // uno recogido reaparece en otro punto libre de la piscina.
    public class PescaMangosGame : PartyGameBase
    {
        private const float CLEAR_OF_RAFTS = 3f;
        private const float CLEAR_OF_MANGOS = 2.5f;

        [Tooltip("Salida de cada lugar (J1..J4), mirando al centro")]
        [SerializeField] private Transform[] spawns = new Transform[4];
        [Tooltip("Modelos MiniRaft_J1..J4")]
        [SerializeField] private GameObject[] miniModels = new GameObject[4];
        [SerializeField] private GameObject mangoPrefab;
        [Tooltip("Mitad del área donde aparecen los mangos (m)")]
        [SerializeField] private Vector2 areaHalfSize = new Vector2(10f, 10f);
        [SerializeField] private float mangoHeight = 0.6f;

        private readonly List<PowerUpBase> mangos = new List<PowerUpBase>();
        private readonly List<float> respawnAt = new List<float>();
        private readonly List<int> single = new List<int>(1);

        public IReadOnlyList<PowerUpBase> Mangos => mangos;
        public int Collected { get; private set; }

        public void ConfigureArena(Transform[] raftSpawns, GameObject[] models, GameObject mango, Vector2 halfSize)
        {
            spawns = raftSpawns;
            miniModels = models;
            mangoPrefab = mango;
            areaHalfSize = halfSize;
        }

        private void OnEnable() => GameEvents.OnPowerUp += OnPowerUp;
        private void OnDisable() => GameEvents.OnPowerUp -= OnPowerUp;

        protected override void Spawn()
        {
            for (int i = 0; i < players.Count; i++)
            {
                int place = Mathf.Clamp(players[i].Index, 0, spawns.Length - 1);
                single.Clear();
                single.Add(i);
                var raft = SpawnRaft(spawns[place].position, spawns[place].rotation, single, data.miniRaftRadius,
                    place < miniModels.Length ? miniModels[place] : null);
                MiniRaft.Tint(raft, players[i].Color);
            }
            for (int i = 0; i < data.mangosAtOnce; i++)
            {
                var mango = Instantiate(mangoPrefab, FreePoint(), Quaternion.identity, transform).GetComponent<PowerUpBase>();
                mangos.Add(mango);
                respawnAt.Add(-1f);
            }
        }

        private void OnPowerUp(PowerUpData powerUp, PlayerAvatar taker)
        {
            int player = IndexOf(taker);
            if (player < 0 || State != PartyState.Playing)
                return;
            Collected++;
            AddScore(player, 1);
        }

        protected override void Tick(float deltaTime)
        {
            for (int i = 0; i < mangos.Count; i++)
            {
                if (!mangos[i].IsCollected)
                    continue;
                if (respawnAt[i] < 0f)
                    respawnAt[i] = Elapsed + data.mangoRespawnSeconds;
                else if (Elapsed >= respawnAt[i])
                {
                    respawnAt[i] = -1f;
                    mangos[i].Recycle(FreePoint());
                }
            }
        }

        // Un punto del área lejos de las balsas y de los otros mangos (si no se encuentra en 12 intentos, el último).
        private Vector3 FreePoint()
        {
            var origin = transform.position;
            Vector3 point = origin;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                point = origin + new Vector3(Random.Range(-areaHalfSize.x, areaHalfSize.x), mangoHeight, Random.Range(-areaHalfSize.y, areaHalfSize.y));
                if (IsClear(point))
                    break;
            }
            return point;
        }

        private bool IsClear(Vector3 point)
        {
            for (int i = 0; i < rafts.Count; i++)
            {
                if (Flat(rafts[i].transform.position - point).magnitude < CLEAR_OF_RAFTS)
                    return false;
            }
            for (int i = 0; i < mangos.Count; i++)
            {
                if (!mangos[i].IsCollected && Flat(mangos[i].transform.position - point).magnitude < CLEAR_OF_MANGOS)
                    return false;
            }
            return true;
        }

        public override string ResultText(int player) => scores[player].ToString();

        // ---------------------------------------------------------------- bots
        // Van al mango libre más cercano; si otra balsa está mucho más cerca de ese, buscan otro.
        public override bool TryGetHeading(BotBrain bot, Vector3 raftPosition, out Vector3 heading)
        {
            heading = Vector3.zero;
            if (IndexOf(bot.Avatar) < 0)
                return false;
            var myRaft = bot.Seat.Raft != null ? bot.Seat.Raft.Controller : null;
            PowerUpBase best = null;
            float bestCost = float.MaxValue;
            for (int i = 0; i < mangos.Count; i++)
            {
                if (mangos[i].IsCollected)
                    continue;
                var position = mangos[i].transform.position;
                float mine = Flat(position - raftPosition).magnitude;
                float rival = float.MaxValue;
                for (int r = 0; r < rafts.Count; r++)
                {
                    if (rafts[r] != myRaft)
                        rival = Mathf.Min(rival, Flat(position - rafts[r].transform.position).magnitude);
                }
                float cost = mine + (rival < mine * 0.5f ? 8f : 0f);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = mangos[i];
                }
            }
            var target = best != null ? best.transform.position : transform.position;
            heading = Flat(target - raftPosition);
            if (heading.sqrMagnitude < 0.01f)
                heading = Vector3.forward;
            heading.Normalize();
            return true;
        }
    }
}
