using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H19 Géiser (docs/05): cada 'cycle' s (3) avisa con vapor 'warningTime' y, si la balsa está encima, la lanza hacia
    // arriba a 'launch' m/s (12). Sirve de trampolín y de aire para cruzar abismos.
    [RequireComponent(typeof(BoxCollider))]
    public class Geyser : HazardBase
    {
        [SerializeField] private Transform steam;
        [SerializeField] private float phase;
        [Tooltip("Géiser final: avisa en cuanto entra la balsa en vez de seguir el ciclo")]
        [SerializeField] private bool armOnEnter;

        private float timer;
        private RaftController inside;

        public float Cycle => Config.Get("cycle", 3f);
        public float Launch => Config.Get("launch", 12f);
        public int Launches { get; private set; }

        public void Configure(Transform steamColumn, float startPhase)
        {
            steam = steamColumn;
            phase = startPhase;
        }

        private void Awake() => timer = phase;

        protected override void OnRaftEnter(RaftController raft)
        {
            inside = raft;
            if (armOnEnter && !IsWarning)
                Warn();
        }
        protected override void OnRaftExit(RaftController raft)
        {
            if (raft == inside)
                inside = null;
        }

        protected override void Update()
        {
            base.Update();
            timer += Time.deltaTime * HazardSpeed;
            if (!armOnEnter && timer >= Cycle)
            {
                timer = 0f;
                Warn();
            }
            if (steam != null)
            {
                float k = IsWarning ? WarningProgress : 0f;
                steam.localScale = new Vector3(1f, Mathf.Lerp(0.05f, 1f, k), 1f);
            }
        }

        protected override void OnWarningEnded()
        {
            if (steam != null)
                steam.localScale = new Vector3(1f, 0.05f, 1f);
            if (inside == null)
                return;
            var body = inside.Body;
            var v = body.velocity;
            // Lanzamiento vertical: velocidad vertical = launch (los pedos del aire hacen el resto).
            PushRaft(inside, Vector3.up * ((Launch - Mathf.Max(0f, v.y)) * body.mass), body.worldCenterOfMass);
            Launches++;
        }
    }
}
