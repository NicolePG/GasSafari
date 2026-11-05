using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H27 Trampolín / toldo / fuente (docs/05): rebota la balsa hacia arriba a 10 m/s. Se arma de nuevo al salir la balsa
    // de la zona. 'forwardSpeed' suma velocidad horizontal hacia el frente del objeto (fuente central de W1-1).
    public class Trampoline : HazardBase
    {
        [SerializeField] private Transform squash;
        [Tooltip("m/s extra hacia transform.forward (0 = solo vertical)")] [SerializeField] private float forwardSpeed;

        private float lastLaunch = -10f;
        private bool armed = true;
        private float squashTime;

        public float LaunchSpeed => Config.Get("launchSpeed", 10f);

        public void Configure(Transform squashTransform, float forward)
        {
            squash = squashTransform;
            forwardSpeed = forward;
        }

        protected override void OnRaftEnter(RaftController raft) => TryLaunch(raft);
        protected override void OnRaftStay(RaftController raft) => TryLaunch(raft);

        protected override void OnRaftExit(RaftController raft)
        {
            armed = true;
        }

        private void TryLaunch(RaftController raft)
        {
            if (!armed || Time.time - lastLaunch < Config.Get("cooldown", 0.5f))
                return;
            armed = false;
            lastLaunch = Time.time;

            var body = raft.Body;
            var velocity = body.velocity;
            // Velocidad vertical final = launchSpeed (sin torque: el impulso va al centro de masa).
            var delta = new Vector3(0f, LaunchSpeed - velocity.y, 0f);
            if (forwardSpeed > 0f)
            {
                var flat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                delta += flat * forwardSpeed;
            }
            PushRaft(raft, delta * body.mass, body.worldCenterOfMass);

            squashTime = 0.3f;
            if (Services.IsReady)
                Services.Audio.Play(SoundId.RaftBounce, transform.position);
        }

        protected override void Update()
        {
            base.Update();
            if (squash == null)
                return;
            squashTime = Mathf.Max(0f, squashTime - Time.deltaTime);
            float k = squashTime / 0.3f;
            // Aplasta y estira: baja y ensancha al principio, rebota al final.
            float y = 1f - 0.25f * Mathf.Sin(k * Mathf.PI);
            squash.localScale = new Vector3(1f / Mathf.Sqrt(y), y, 1f / Mathf.Sqrt(y));
        }
    }
}
