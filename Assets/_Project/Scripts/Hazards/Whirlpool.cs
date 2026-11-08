using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H09 Remolino (docs/05): dentro de su radio (6-15 m, el del trigger esférico) atrae la balsa al centro con 4 m/s²
    // y la hace girar a 60°/s. Se escapa con un sync fuerte hacia afuera o con el Panzazo de Gloria.
    [RequireComponent(typeof(SphereCollider))]
    public class Whirlpool : HazardBase
    {
        // 1/s: lo rápido que la velocidad de giro de la balsa se acerca a la del remolino.
        private const float SPIN_GAIN = 2f;

        private float lastStep = -1f;

        public float Attraction => Config.Get("attraction", 4f);
        public float SpinDegrees => Config.Get("spin", 60f);
        public float Radius
        {
            get
            {
                var sphere = GetComponent<SphereCollider>();
                return sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            }
        }

        protected override void OnRaftStay(RaftController raft)
        {
            if (Mathf.Approximately(lastStep, Time.fixedTime))
                return;
            lastStep = Time.fixedTime;
            var body = raft.Body;
            var toCenter = transform.position - body.worldCenterOfMass;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.01f)
                raft.ApplyThrust(toCenter.normalized * Attraction, body.worldCenterOfMass, ForceMode.Acceleration);

            float target = SpinDegrees * Mathf.Deg2Rad;
            float spin = body.angularVelocity.y;
            body.AddTorque(Vector3.up * ((target - spin) * SPIN_GAIN), ForceMode.Acceleration);
        }
    }
}
