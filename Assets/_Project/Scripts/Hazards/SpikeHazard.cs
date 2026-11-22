using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H10 Pinchos (docs/05): revientan la cámara que los toca (alwaysDamagesChambers: también en Familiar).
    // La gaseosa pasa por encima de los bajos (la balsa flota más alta y no los toca).
    // Avisa al acercarse la balsa (aviso por cercanía de HazardBase): HUD, Melman y bots.
    public class SpikeHazard : HazardBase
    {
        private Vector3 center;
        private float radius;

        protected override bool WarnsOnApproach => true;
        public override float WarningRadius => radius;
        public override Vector3 WarningCenter => center;

        private void Awake() => MeasureColliders(out center, out radius);

        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            DamageRaft(raft, point);
        }
    }
}
