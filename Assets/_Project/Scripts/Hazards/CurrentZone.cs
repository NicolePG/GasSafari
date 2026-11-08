using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H31 Corriente cruzada (docs/05): aceleración constante de 4 m/s² hacia transform.right dentro de la zona; dos zonas
    // perpendiculares que se cruzan hacen la corriente que choca.
    [RequireComponent(typeof(BoxCollider))]
    public class CurrentZone : HazardBase
    {
        private float lastStep = -1f;

        public float Acceleration => Config.Get("accel", 4f);

        protected override void OnRaftStay(RaftController raft)
        {
            if (Mathf.Approximately(lastStep, Time.fixedTime))
                return;
            lastStep = Time.fixedTime;
            var dir = transform.right;
            dir.y = 0f;
            raft.ApplyThrust(dir.normalized * Acceleration, raft.Body.worldCenterOfMass, ForceMode.Acceleration);
        }
    }
}
