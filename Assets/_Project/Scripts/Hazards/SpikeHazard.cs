using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H10 Pinchos (docs/05): revientan la cámara que los toca (alwaysDamagesChambers: también en Familiar).
    // La gaseosa pasa por encima de los bajos (la balsa flota más alta y no los toca).
    public class SpikeHazard : HazardBase
    {
        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            DamageRaft(raft, point);
        }
    }
}
