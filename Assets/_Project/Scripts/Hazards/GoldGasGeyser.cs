using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H36 Géiser de gas dorado (docs/05): al pasar la balsa por encima llena el medidor de equipo al 100 %, para que un
    // abismo que exige el Súper Pedo se pueda cruzar. No daña ni empuja. Rellena como mucho una vez cada 'cooldown' s.
    [RequireComponent(typeof(BoxCollider))]
    public class GoldGasGeyser : HazardBase
    {
        [SerializeField] private Transform jet;

        private float nextFill;
        private float burst;

        public float Cooldown => Config.Get("cooldown", 4f);
        public int Fills { get; private set; }

        public void Configure(Transform jetColumn)
        {
            jet = jetColumn;
        }

        protected override void OnRaftEnter(RaftController raft)
        {
            if (Time.time < nextFill || !raft.TryGetComponent<TeamMeter>(out var meter))
                return;
            nextFill = Time.time + Cooldown;
            meter.Set(meter.Max);
            Fills++;
            burst = 1f;
        }

        protected override void Update()
        {
            base.Update();
            // El chorro late en reposo y se dispara al rellenar.
            burst = Mathf.MoveTowards(burst, 0f, Time.deltaTime * 1.5f);
            if (jet != null)
                jet.localScale = new Vector3(1f, Mathf.Lerp(0.35f + 0.05f * Mathf.Sin(Time.time * 3f), 1f, burst), 1f);
        }
    }
}
