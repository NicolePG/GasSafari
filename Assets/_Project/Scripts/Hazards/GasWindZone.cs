using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H08 Viento (docs/05): ráfagas laterales hacia transform.right con aviso 1 s antes (warningTime). Cada ráfaga dura
    // 'gust' (2 s) con 3-5 m/s² (sube dentro de la ráfaga) y deja 'calm' s de calma. Solo dentro del trigger.
    // Se aplica por RaftFlowReceiver.Wind (Gloria reduce la componente lateral).
    public class GasWindZone : HazardBase
    {
        [Tooltip("s de desfase del ciclo")] [SerializeField] private float phase;

        private RaftFlowReceiver receiver;
        private float clock;
        private bool gusting;
        private float gustTime;

        public float MinAccel => Config.Get("accelMin", 3f);
        public float MaxAccel => Config.Get("accelMax", 5f);
        public float Gust => Config.Get("gust", 2f);
        // s de calma entre ráfagas (provisional: docs/05 no lo fija).
        public float Calm => Config.Get("calm", 3f);
        public bool IsGusting => gusting;
        public Vector3 Direction
        {
            get
            {
                var d = transform.right;
                d.y = 0f;
                return d.normalized;
            }
        }

        private void Start() => clock = -phase;

        protected override void OnRaftEnter(RaftController raft) => receiver = raft.GetComponent<RaftFlowReceiver>();

        protected override void OnRaftExit(RaftController raft)
        {
            if (receiver != null)
                receiver.Wind = Vector3.zero;
            receiver = null;
        }

        protected override void Update()
        {
            base.Update();
            if (gusting)
                return;
            clock += Time.deltaTime;
            // El aviso empieza warningTime antes de que acabe la calma.
            if (!IsWarning && clock >= Calm - Config.warningTime)
                Warn();
        }

        protected override void OnWarningEnded()
        {
            gusting = true;
            gustTime = 0f;
        }

        private void FixedUpdate()
        {
            if (!gusting)
                return;
            gustTime += Time.fixedDeltaTime;
            float k = Mathf.Clamp01(gustTime / Gust);
            if (receiver != null)
                receiver.Wind = Direction * Mathf.Lerp(MinAccel, MaxAccel, Mathf.Sin(k * Mathf.PI));
            if (gustTime < Gust)
                return;
            gusting = false;
            clock = 0f;
            if (receiver != null)
                receiver.Wind = Vector3.zero;
        }
    }
}
