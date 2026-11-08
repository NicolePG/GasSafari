using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H21 Rápidos (docs/05): corriente muy fuerte con rocas (las rocas son colliders del nivel). Mientras la balsa está dentro
    // de la zona la acelera hacia transform.forward 'accel' m/s² (8 por defecto) multiplicados por 'intensity': 0.5 para los
    // rápidos de clase 1, 1 para los de clase 2 y el rápido largo. No daña por sí mismo.
    [RequireComponent(typeof(BoxCollider))]
    public class RapidsZone : HazardBase
    {
        [SerializeField] private float intensity = 1f;

        public float Accel => Config.Get("accel", 8f) * intensity;

        public void Configure(float strength) => intensity = strength;

        protected override void OnRaftStay(RaftController raft)
        {
            var direction = transform.forward;
            direction.y = 0f;
            direction.Normalize();
            var body = raft.Body;
            // OnTriggerStay corre en cada paso de física: impulso = aceleración x masa x paso.
            PushRaft(raft, direction * (Accel * body.mass * Time.fixedDeltaTime), body.worldCenterOfMass);
        }
    }
}
