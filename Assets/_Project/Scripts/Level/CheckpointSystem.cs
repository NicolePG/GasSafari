using System;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Checkpoints y reaparición del equipo (docs/02 §10). El primero de la lista es la salida.
    // Lleva el avance de la balsa del nivel y, en Carrera por Equipos, el de las demás balsas (AddRacer): cada una reaparece
    // en su último checkpoint. Lo público sin balsa (CurrentIndex, IsRespawning, RespawnStarted...) es de la balsa del nivel.
    [RequireComponent(typeof(LevelTimer))]
    public class CheckpointSystem : MonoBehaviour
    {
        // Red de seguridad: si la balsa sale del cauce (un géiser de W4-3 la lanzó por encima de la pared) y cae al vacío sin
        // volcarse, nada la hundía y el nivel quedaba sin terminar. Por debajo del punto más bajo del recorrido, reaparece.
        private const float OUT_OF_WORLD_DROP = 30f;
        // Al reaparecer corrida a un lado del checkpoint, deja este aire con la pared (m).
        private const float WALL_AIR = 0.3f;

        private sealed class Racer
        {
            public readonly RaftController Raft;
            public readonly RaftChambers Chambers;
            public readonly RaftStateMonitor Monitor;
            public readonly RaftEffects Effects;
            public readonly RaftSeats Seats;
            public int Index;
            public int RespawnsAtCurrent;
            public int SavedChambers;
            public bool Respawning;
            public float Fade;
            public bool HasStart;
            public Vector3 StartPosition;
            public Quaternion StartRotation;
            public float Lateral;

            public Racer(RaftController raft)
            {
                Raft = raft;
                Chambers = raft.GetComponent<RaftChambers>();
                Monitor = raft.GetComponent<RaftStateMonitor>();
                Effects = raft.GetComponent<RaftEffects>();
                Seats = raft.GetComponent<RaftSeats>();
                SavedChambers = Chambers.IntactMask;
            }
        }

        [SerializeField] private BalanceConfig balance;
        [SerializeField] private RaftController raft;
        [SerializeField] private Checkpoint[] checkpoints;

        private readonly List<Checkpoint> active = new List<Checkpoint>();
        private readonly List<Racer> racers = new List<Racer>();
        private Racer main;
        private LevelTimer timer;
        private float outOfWorldY = float.NegativeInfinity;

        public int CurrentIndex => main != null ? main.Index : 0;
        public Checkpoint Current => active[CurrentIndex];
        public IReadOnlyList<Checkpoint> Active => active;
        public bool IsRespawning => main != null && main.Respawning;
        // Para la Ayuda Real (docs/06 §3).
        public int RespawnsAtCurrent => main != null ? main.RespawnsAtCurrent : 0;
        // Penalización de tiempo de la dificultad al reaparecer (Carrera por Equipos no la usa: el tiempo perdido ya cuenta).
        public bool ApplyPenalty { get; set; } = true;

        // La interfaz hace el fundido a negro mientras dura respawnFade.
        public event Action RespawnStarted;
        // Cualquier balsa que terminó de reaparecer (la del nivel y las de AddRacer).
        public event Action<RaftController> RaftRespawned;

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
            outOfWorldY = LowestCourseHeight() - OUT_OF_WORLD_DROP;
        }

        private float LowestCourseHeight()
        {
            float lowest = float.PositiveInfinity;
            for (int i = 0; i < checkpoints.Length; i++)
                lowest = Mathf.Min(lowest, checkpoints[i].transform.position.y);
            var flow = FindFirstObjectByType<FlowSpline>();
            if (flow != null && flow.SampleCount > 0)
                lowest = Mathf.Min(lowest, flow.MinHeight);
            return float.IsPositiveInfinity(lowest) ? float.NegativeInfinity : lowest;
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < racers.Count; i++)
            {
                var racer = racers[i];
                if (!racer.Respawning && racer.Raft != null && racer.Raft.Body.position.y < outOfWorldY)
                    Begin(racer);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnRaftLost += OnRaftLost;
        }

        private void OnDisable()
        {
            GameEvents.OnRaftLost -= OnRaftLost;
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
            if (raft == null || (main != null && main.Raft == raft))
                return;
            var racer = new Racer(raft);
            if (main != null)
                racers.Remove(main);
            main = racer;
            racers.Insert(0, main);
        }

        // ---------------------------------------------------------------- Carrera por Equipos
        // La balsa del nivel cuando hay más de una en la escena (Start la buscaría por tipo y podría tomar otra).
        public void BindRaft(RaftController levelRaft)
        {
            raft = levelRaft;
            CacheRaft();
        }

        // Otra balsa que avanza por los mismos checkpoints y reaparece en el suyo, sin tocar el estado del nivel.
        public void AddRacer(RaftController other)
        {
            if (other == null || Find(other) != null)
                return;
            var racer = new Racer(other);
            racer.Index = 0;
            racers.Add(racer);
        }

        // Pose de salida de esa balsa (reaparece ahí mientras no pase el primer checkpoint) y cuánto se corre a su lado en
        // los demás checkpoints (+ derecha, - izquierda), para que dos balsas no reaparezcan una encima de la otra.
        public void SetLane(RaftController who, Vector3 startPosition, Quaternion startRotation, float lateral)
        {
            var racer = Find(who);
            if (racer == null)
                return;
            racer.HasStart = true;
            racer.StartPosition = startPosition;
            racer.StartRotation = startRotation;
            racer.Lateral = lateral;
        }

        // Checkpoint alcanzado por esa balsa (0 = salida, -1 si no la lleva este sistema).
        public int IndexOf(RaftController who)
        {
            var racer = Find(who);
            return racer != null ? racer.Index : -1;
        }

        public bool IsRaftRespawning(RaftController who)
        {
            var racer = Find(who);
            return racer != null && racer.Respawning;
        }

        private Racer Find(RaftController who)
        {
            for (int i = 0; i < racers.Count; i++)
            {
                if (racers[i].Raft == who)
                    return racers[i];
            }
            return null;
        }

        // ---------------------------------------------------------------- checkpoints
        // Infinito (docs/06 §1): los tramos agregan sus checkpoints mientras se juega. Antes de Start entran con el filtro de
        // la dificultad; después quedan activos (el generador los marca hardKeep, así existen en todas las dificultades).
        public void Append(Checkpoint checkpoint)
        {
            var list = new Checkpoint[checkpoints.Length + 1];
            Array.Copy(checkpoints, list, checkpoints.Length);
            list[checkpoints.Length] = checkpoint;
            checkpoints = list;
            if (active.Count == 0)
                return;
            checkpoint.Bind(this);
            active.Add(checkpoint);
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
            for (int i = 0; i < racers.Count; i++)
            {
                racers[i].Index = 0;
                racers[i].SavedChambers = racers[i].Chambers.IntactMask;
            }
        }

        public void Reach(Checkpoint checkpoint, RaftController who)
        {
            var racer = Find(who);
            if (racer == null || racer.Respawning)
                return;
            int index = active.IndexOf(checkpoint);
            if (index <= racer.Index)
                return;

            racer.Index = index;
            racer.RespawnsAtCurrent = 0;
            racer.SavedChambers = racer.Chambers.IntactMask;
            if (racer == main)
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
            int inserted = CurrentIndex + 1;
            active.Insert(inserted, extra);
            for (int i = 0; i < racers.Count; i++)
            {
                if (racers[i] != main && racers[i].Index >= inserted)
                    racers[i].Index++;
            }
            Advance(inserted);
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
            main.Index = index;
            main.RespawnsAtCurrent = 0;
            main.SavedChambers = main.Chambers.IntactMask;
            GameEvents.RaiseCheckpoint(index);
            RequestRespawn();
        }

        // ---------------------------------------------------------------- reaparición
        // Hundimiento, vuelco o reinicio pedido desde la pausa (balsa del nivel).
        public void RequestRespawn() => Begin(main);

        // La balsa que se hundió, cayó a un abismo o la atrapó un peligro. null = la del nivel.
        public void RequestRespawn(RaftController who) => Begin(who == null ? main : Find(who));

        private void OnRaftLost(RaftController who) => RequestRespawn(who);

        private void Begin(Racer racer)
        {
            if (racer == null || racer.Respawning)
                return;
            racer.Respawning = true;
            racer.Fade = balance.respawnFade;
            if (racer == main)
                RespawnStarted?.Invoke();
        }

        private void Update()
        {
            for (int i = 0; i < racers.Count; i++)
            {
                var racer = racers[i];
                if (!racer.Respawning)
                    continue;
                racer.Fade -= Time.deltaTime;
                if (racer.Fade <= 0f)
                    Respawn(racer);
            }
        }

        private void Respawn(Racer racer)
        {
            RespawnPose(racer, out var position, out var rotation);
            var body = racer.Raft.Body;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = position;
            body.rotation = rotation;
            racer.Raft.transform.SetPositionAndRotation(position, rotation);

            racer.Chambers.SetState(racer.SavedChambers);
            racer.Monitor.ResetState();
            racer.Effects.ClearAll();

            var crew = racer.Seats.Crew;
            for (int i = 0; i < crew.Count; i++)
            {
                crew[i].GetComponent<AvatarFall>().ReturnToRaft();
                crew[i].GetComponent<GasTank>().SetFraction(1f);
            }

            racer.RespawnsAtCurrent++;
            racer.Respawning = false;
            if (racer == main)
            {
                if (ApplyPenalty)
                    timer.AddPenalty(Services.Session.Difficulty.respawnPenalty);
                GameEvents.RaiseRespawn();
            }
            RaftRespawned?.Invoke(racer.Raft);
        }

        // En la salida, la de su carril; en un checkpoint, corrida a su lado lo que deje libre la pared más cercana.
        private void RespawnPose(Racer racer, out Vector3 position, out Quaternion rotation)
        {
            if (racer.Index == 0 && racer.HasStart)
            {
                position = racer.StartPosition;
                rotation = racer.StartRotation;
                return;
            }
            var pose = active[racer.Index].transform;
            position = pose.position;
            rotation = pose.rotation;
            if (Mathf.Approximately(racer.Lateral, 0f))
                return;
            var side = rotation * Vector3.right * Mathf.Sign(racer.Lateral);
            float wanted = Mathf.Abs(racer.Lateral);
            float room = wanted + racer.Raft.Radius + WALL_AIR;
            if (Physics.Raycast(position + Vector3.up, side, out var hit, room, 1 << GameLayers.Environment, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(0f, hit.distance - racer.Raft.Radius - WALL_AIR);
            position += side * wanted;
        }
    }
}
