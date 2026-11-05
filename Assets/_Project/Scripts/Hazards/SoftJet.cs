using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Chorro lateral suave (W1-1, «fuentes que empujan»; docs/04, sin ID en docs/05): mientras la balsa está dentro de su zona
    // la acelera hacia transform.right (parámetro 'accel' en m/s², provisional). No hace daño.
    public class SoftJet : HazardBase
    {
        protected override void OnRaftStay(RaftController raft)
        {
            var direction = transform.right;
            direction.y = 0f;
            direction.Normalize();
            var body = raft.Body;
            // OnTriggerStay corre en cada paso de física: impulso = aceleración x masa x paso.
            PushRaft(raft, direction * (Config.Get("accel", 2f) * body.mass * Time.fixedDeltaTime), body.worldCenterOfMass);
        }
    }
}
