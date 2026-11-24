using System;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using UnityEngine;

namespace GasSafari.Modes
{
    // Río de Infinito y del Desafío Diario (docs/06 §1) en la escena Infinito: pone los tramos uno tras otro (entrada de
    // uno = salida del anterior), alarga el cauce (FlowSpline.SetPath), agrega un checkpoint cada N tramos y quita los
    // tramos que quedan detrás del checkpoint actual. Infinito genera por delante de la balsa sin fin; el Diario arma los
    // 1500 m al cargar, con la semilla de la fecha, y pone la meta al final.
    [DefaultExecutionOrder(-200)]
    public class EndlessTrack : MonoBehaviour
    {
        private const float WATER_RECENTER = 120f;

        [SerializeField] private EndlessConfig config;
        [SerializeField] private FlowSpline flow;
        [SerializeField] private CheckpointSystem checkpoints;
        [SerializeField] private FinishGate finish;
        [SerializeField] private Transform water;
        [SerializeField] private GameObject checkpointPrefab;

        // Pruebas: semilla fija para Infinito o fecha fija para el Diario.
        public static int? SeedOverride;
        public static DateTime? DateOverride;

        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> accel = new List<float>();
        private readonly List<EndlessChunk> live = new List<EndlessChunk>();
        private readonly List<ChunkPick> picks = new List<ChunkPick>();
        private readonly List<int> checkpointChunks = new List<int>();
        private EndlessPlanner planner;
        private Transform staging;
        private Transform activeRoot;
        private Transform checkpointRoot;
        private Vector3 exitPosition;
        private float exitYaw;
        private float placedLength;
        private int nextActivate;
        private int hint;
        private bool built;

        public static EndlessTrack Current { get; private set; }
        public EndlessConfig Config => config;
        public bool IsDaily { get; private set; }
        public int Seed { get; private set; }
        public string DateKey { get; private set; }
        // Metros a lo largo del cauce donde está la balsa, y el más lejano al que llegó.
        public float Distance { get; private set; }
        public float MaxDistance { get; private set; }
        public int Tier => planner != null ? planner.TierAt(MaxDistance) : 1;
        public float CourseLength => IsDaily ? placedLength : float.PositiveInfinity;
        public IReadOnlyList<ChunkPick> Picks => picks;
        public IReadOnlyList<EndlessChunk> LiveChunks => live;
        public int ChunksPlaced => picks.Count;
        public int CheckpointCount => checkpointChunks.Count;

        // Sube la dificultad (1-5) al pasar cada 'difficultyStep' m.
        public event Action<int> TierChanged;

        public void Configure(EndlessConfig endlessConfig, FlowSpline flowSpline, CheckpointSystem checkpointSystem, FinishGate gate,
            Transform waterPlane, GameObject checkpoint)
        {
            config = endlessConfig;
            flow = flowSpline;
            checkpoints = checkpointSystem;
            finish = gate;
            water = waterPlane;
            checkpointPrefab = checkpoint;
        }

        private void Awake()
        {
            Current = this;
            if (config == null)
                config = EndlessConfig.Instance;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        // Antes que CheckpointSystem y LevelManager: el cauce y los checkpoints ya existen cuando nace la balsa.
        private void Start()
        {
            Build();
        }

        public void Build()
        {
            if (built)
                return;
            built = true;
            string runMode = Services.IsReady ? Services.Session.RunMode : ProgressionRules.MODE_ENDLESS;
            IsDaily = runMode == ProgressionRules.MODE_DAILY;
            var date = DateOverride ?? DateTime.Today;
            DateKey = DailyChallenge.DateKey(date);
            Seed = IsDaily ? DailyChallenge.SeedFor(date) : SeedOverride ?? Environment.TickCount;

            staging = NewGroup("Staging", false);
            activeRoot = NewGroup("Tramos", true);
            checkpointRoot = NewGroup("Checkpoints", true);
            checkpointChunks.Add(0); // Checkpoint_0 de la plantilla: la salida

            planner = config.CreatePlanner(Seed);
            if (IsDaily)
            {
                var plan = config.PlanDaily(Seed);
                for (int i = 0; i < plan.Count; i++)
                    Place(plan[i]);
            }
            else
            {
                while (placedLength < config.aheadDistance * 1.5f)
                    Place(planner.Next());
            }
            flow.SetPath(points, accel, config.channelWidth);
            PlaceFinish();
            ActivateNear(0f);
        }

        private Transform NewGroup(string name, bool active)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.SetActive(active);
            return go.transform;
        }

        // El tramo nace inactivo en su lugar: sus peligros despiertan recién al activarse, ya en su posición.
        private void Place(ChunkPick pick)
        {
            var data = config.chunks[pick.Spec];
            var prefab = data.PrefabFor(pick.Mirrored);
            var rotation = Quaternion.Euler(0f, exitYaw, 0f);
            var chunk = Instantiate(prefab, exitPosition, rotation, staging);
            chunk.name = picks.Count.ToString("00") + "_" + prefab.name;
            chunk.Data = data;
            chunk.Index = picks.Count;

            var path = chunk.Path;
            var pathAccel = chunk.Accel;
            for (int i = points.Count == 0 ? 0 : 1; i < path.Length; i++)
            {
                points.Add(exitPosition + rotation * path[i]);
                accel.Add(i < pathAccel.Length ? pathAccel[i] : 0f);
            }
            if (picks.Count > 0 && picks.Count % Mathf.Max(1, config.checkpointEvery) == 0)
                AddCheckpoint(exitPosition, rotation, picks.Count);

            exitPosition += rotation * chunk.ExitLocal;
            exitYaw += chunk.ExitYawLocal;
            placedLength += data.length;
            picks.Add(pick);
            live.Add(chunk);
        }

        private void AddCheckpoint(Vector3 position, Quaternion rotation, int chunkIndex)
        {
            var go = Instantiate(checkpointPrefab, position + Vector3.up * 0.5f, rotation, checkpointRoot);
            go.name = "Checkpoint_" + checkpointChunks.Count;
            var checkpoint = go.GetComponent<Checkpoint>();
            checkpoint.Configure(false, true);
            checkpoints.Append(checkpoint);
            checkpointChunks.Add(chunkIndex);
        }

        private void PlaceFinish()
        {
            var box = finish.GetComponent<BoxCollider>();
            if (!IsDaily)
            {
                box.enabled = false;
                return;
            }
            var forward = Quaternion.Euler(0f, exitYaw, 0f) * Vector3.forward;
            finish.transform.SetPositionAndRotation(exitPosition - forward * 2f + Vector3.up * 2f, Quaternion.LookRotation(forward));
            box.size = new Vector3(config.channelWidth + 2f, 6f, 2f);
            box.enabled = true;
        }

        private void Update()
        {
            if (!built)
                return;
            var level = LevelManager.Current;
            var raft = level != null ? level.Raft : null;
            if (raft == null)
                return;

            var position = raft.Body.position;
            Distance = flow.GetDistance(position, ref hint);
            if (Distance > MaxDistance && !checkpoints.IsRespawning)
            {
                int before = Tier;
                MaxDistance = Distance;
                if (Tier != before)
                    TierChanged?.Invoke(Tier);
            }

            if (!IsDaily && placedLength - Distance < config.aheadDistance * 1.5f)
            {
                Place(planner.Next());
                flow.SetPath(points, accel, config.channelWidth);
            }
            ActivateNear(Distance);
            Recycle();
            FollowWater(position);
        }

        private void ActivateNear(float distance)
        {
            while (nextActivate < live.Count)
            {
                var chunk = live[nextActivate];
                if (chunk == null || picks[chunk.Index].Start - distance > config.aheadDistance)
                    break;
                chunk.transform.SetParent(activeRoot, true);
                nextActivate++;
            }
        }

        // Fuera los tramos anteriores al del checkpoint actual (se conservan keepBehind para la cámara al reaparecer).
        private void Recycle()
        {
            int current = Mathf.Clamp(checkpoints.CurrentIndex, 0, checkpointChunks.Count - 1);
            int keepFrom = checkpointChunks[current] - config.keepBehind;
            while (live.Count > 0 && live[0] != null && live[0].Index < keepFrom && nextActivate > 0)
            {
                Destroy(live[0].gameObject);
                live.RemoveAt(0);
                nextActivate--;
            }
        }

        // El plano de agua (y su nivel base) sigue a la balsa: los tramos son planos a la altura 0.
        private void FollowWater(Vector3 position)
        {
            if (water == null)
                return;
            var offset = position - water.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > WATER_RECENTER * WATER_RECENTER)
                water.position = new Vector3(position.x, water.position.y, position.z);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SeedOverride = null;
            DateOverride = null;
            Current = null;
        }
    }
}
