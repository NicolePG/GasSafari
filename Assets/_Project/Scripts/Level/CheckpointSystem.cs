using System;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Checkpoints y reaparición del equipo (docs/02 §10). El primero de la lista es la salida.
    [RequireComponent(typeof(LevelTimer))]
    public class CheckpointSystem : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;
        [SerializeField] private RaftController raft;
        [SerializeField] private Checkpoint[] checkpoints;

        private readonly List<Checkpoint> active = new List<Checkpoint>();
        private LevelTimer timer;
        private RaftChambers chambers;
        private RaftStateMonitor monitor;
        private RaftEffects effects;
        private RaftSeats seats;
        private int savedChambers;
        private float fadeTimer;

        public int CurrentIndex { get; private set; }
        public Checkpoint Current => active[CurrentIndex];
        public IReadOnlyList<Checkpoint> Active => active;
        public bool IsRespawning { get; private set; }
        // Para la Ayuda Real (docs/06 §3).
        public int RespawnsAtCurrent { get; private set; }

        // La interfaz hace el fundido a negro mientras dura respawnFade.
        public event Action RespawnStarted;

        private void Awake()
        {
            timer = GetComponent<LevelTimer>();
        }

        private void Start()
        {
            if (raft == null)
                raft = FindFirstObjectByType<RaftController>();
            CacheRaft();
            ApplyFilter(Services.Session.Difficulty.checkpointFilter);
        }

        private void OnEnable()
        {
            GameEvents.OnRaftSunk += RequestRespawn;
            GameEvents.OnRaftCapsized += RequestRespawn;
        }

        private void OnDisable()
        {
            GameEvents.OnRaftSunk -= RequestRespawn;
            GameEvents.OnRaftCapsized -= RequestRespawn;
        }

        public void Configure(BalanceConfig balanceConfig, RaftController levelRaft, Checkpoint[] levelCheckpoints)
        {
            balance = balanceConfig;
            raft = levelRaft;
            checkpoints = levelCheckpoints;
            CacheRaft();
        }

        private void CacheRaft()
        {
            chambers = raft.GetComponent<RaftChambers>();
            monitor = raft.GetComponent<RaftStateMonitor>();
            effects = raft.GetComponent<RaftEffects>();
            seats = raft.GetComponent<RaftSeats>();
        }

        public void ApplyFilter(CheckpointFilter filter)
        {
            active.Clear();
            for (int i = 0; i < checkpoints.Length; i++)
            {
                var cp = checkpoints[i];
                bool keep = i == 0
                            || (filter == CheckpointFilter.NormalPlusFamiliarOnly)
                            || (filter == CheckpointFilter.Normal && !cp.FamiliarOnly)
                            || (filter == CheckpointFilter.HardKeepOnly && cp.HardKeep);
                cp.Bind(keep ? this : null);
                if (keep)
                    active.Add(cp);
            }
            CurrentIndex = 0;
            savedChambers = chambers.IntactMask;
        }

        public void Reach(Checkpoint checkpoint, RaftController who)
        {
            if (who != raft || IsRespawning)
                return;
            int index = active.IndexOf(checkpoint);
            if (index <= CurrentIndex)
                return;

            CurrentIndex = index;
            RespawnsAtCurrent = 0;
            savedChambers = chambers.IntactMask;
            GameEvents.RaiseCheckpoint(index);
        }

        // Ayuda Real: un checkpoint extra a 'fraction' del camino hacia el siguiente. Pasa a ser el actual y el equipo reaparece ahí.
        public bool InsertExtraAhead(float fraction)
        {
            if (CurrentIndex + 1 >= active.Count)
                return false;
            var from = Current.transform;
            var to = active[CurrentIndex + 1].transform;
            var go = new GameObject("Checkpoint_Extra");
            go.transform.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, fraction), Quaternion.Slerp(from.rotation, to.rotation, fraction));
            // Queda en la escena del nivel (si no, sobrevive a descargarla), colgado de este sistema.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            go.transform.SetParent(transform, true);
            // Solo marca la pose de reaparición: no hace falta que nadie lo "toque" (Checkpoint pide un colisionador).
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.enabled = false;
            var extra = go.AddComponent<Checkpoint>();
            active.Insert(CurrentIndex + 1, extra);
            Advance(CurrentIndex + 1);
            return true;
        }

        // Ayuda Real: saltar la sección, reaparecer en el siguiente checkpoint.
        public bool SkipToNext()
        {
            if (CurrentIndex + 1 >= active.Count)
                return false;
            Advance(CurrentIndex + 1);
            return true;
        }

        private void Advance(int index)
        {
            CurrentIndex = index;
            RespawnsAtCurrent = 0;
            savedChambers = chambers.IntactMask;
            GameEvents.RaiseCheckpoint(index);
            RequestRespawn();
        }

        // Hundimiento, vuelco o reinicio pedido desde la pausa.
        public void RequestRespawn()
        {
            if (IsRespawning)
                return;
            IsRespawning = true;
            fadeTimer = balance.respawnFade;
            RespawnStarted?.Invoke();
        }

        private void Update()
        {
            if (!IsRespawning)
                return;
            fadeTimer -= Time.deltaTime;
            if (fadeTimer <= 0f)
                Respawn();
        }

        private void Respawn()
        {
            var pose = Current.transform;
            var body = raft.Body;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = pose.position;
            body.rotation = pose.rotation;
            raft.transform.SetPositionAndRotation(pose.position, pose.rotation);

            chambers.SetState(savedChambers);
            monitor.ResetState();
            effects.ClearAll();

            var crew = seats.Crew;
            for (int i = 0; i < crew.Count; i++)
            {
                crew[i].GetComponent<AvatarFall>().ReturnToRaft();
                crew[i].GetComponent<GasTank>().SetFraction(1f);
            }

            timer.AddPenalty(Services.Session.Difficulty.respawnPenalty);
            RespawnsAtCurrent++;
            IsRespawning = false;
            GameEvents.RaiseRespawn();
        }
    }
}
