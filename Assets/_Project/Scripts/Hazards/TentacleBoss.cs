using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H25 Tentáculos (docs/05): golpean el agua en un patrón fijo (los puntos en orden) cada 3 / 2 / 1.5 s según la fase.
    // Cada golpe avisa (el tentáculo se alza warningTime) y empuja 35 a la balsa dentro de 'radius'.
    public class TentacleBoss : HazardBase, IPhasedBoss
    {
        private static readonly float[] Intervals = { 3f, 2f, 1.5f };
        private const float RAISE = 2.5f;

        [Tooltip("Tentáculos en el orden del patrón; cada uno golpea en su posición")] [SerializeField] private Transform[] tentacles = System.Array.Empty<Transform>();

        private readonly Collider[] hits = new Collider[8];
        private Vector3[] rest;
        private int phase = -1;
        private int next;
        private float timer;

        public int Phase => phase;
        public int Strikes { get; private set; }
        public float Hit => Config.Get("hit", 35f);
        // m de radio del golpe (provisional: docs/05 no lo fija).
        public float Radius => Config.Get("radius", 3f);
        public static float IntervalFor(int phase) => Intervals[Mathf.Clamp(phase, 0, Intervals.Length - 1)];

        public void Configure(Transform[] parts)
        {
            tentacles = parts;
        }

        private void Awake()
        {
            rest = new Vector3[tentacles.Length];
            for (int i = 0; i < tentacles.Length; i++)
                rest[i] = tentacles[i].localPosition;
        }

        private void OnEnable() => GameEvents.OnRespawn += ResetPattern;
        private void OnDisable() => GameEvents.OnRespawn -= ResetPattern;

        public void SetPhase(int value)
        {
            if (value == phase)
                return;
            phase = value;
            timer = phase >= 0 ? IntervalFor(phase) : 0f;
        }

        public void ResetPattern()
        {
            CancelWarning();
            next = 0;
            Strikes = 0;
            for (int i = 0; i < tentacles.Length; i++)
                tentacles[i].localPosition = rest[i];
            timer = phase >= 0 ? IntervalFor(phase) : 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (phase < 0 || tentacles.Length == 0)
                return;
            if (IsWarning)
            {
                // El tentáculo que va a golpear se alza mientras avisa.
                var t = tentacles[next];
                t.localPosition = rest[next] + Vector3.up * (RAISE * WarningProgress);
                return;
            }
            timer -= Time.deltaTime * HazardSpeed;
            if (timer <= Config.warningTime)
                Warn();
        }

        protected override void OnWarningEnded()
        {
            var t = tentacles[next];
            t.localPosition = rest[next];
            var point = t.position;
            Strikes++;
            if (Services.IsReady)
                Services.Audio.Play(SoundId.WaterSplashBig, point);
            int count = Physics.OverlapSphereNonAlloc(point, Radius, hits, 1 << GameLayers.Raft, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].attachedRigidbody == null || !hits[i].attachedRigidbody.TryGetComponent<RaftController>(out var raft))
                    continue;
                var away = raft.Body.worldCenterOfMass - point;
                away.y = 0f;
                PushRaft(raft, away.normalized * Hit, raft.Body.worldCenterOfMass);
                break;
            }
            next = (next + 1) % tentacles.Length;
            timer = IntervalFor(phase);
        }
    }
}
