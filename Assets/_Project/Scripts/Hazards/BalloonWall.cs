using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H33 Pared de globos (docs/05): bloquea el paso; revienta con un choque de impulso mayor a 30 (N·s), con un grupo
    // de sync con la balsa dentro de su zona de aviso o con el Rugido (Fragile). Vuelve al reaparecer.
    public class BalloonWall : HazardBase, IBreakable
    {
        private readonly Collider[] nearby = new Collider[4];

        public float ImpactToBreak => Config.Get("impactToBreak", 30f);
        // Radio de la zona de aviso (m); provisional, docs/05 no lo fija.
        public float NearDistance => Config.Get("nearDistance", 6f);
        public bool Broken { get; private set; }

        private void OnEnable()
        {
            GameEvents.OnSync += OnSync;
            GameEvents.OnRespawn += Restore;
        }

        private void OnDisable()
        {
            GameEvents.OnSync -= OnSync;
            GameEvents.OnRespawn -= Restore;
        }

        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            if (!Broken && collision.impulse.magnitude > ImpactToBreak)
                Break(raft.transform.position);
        }

        private void OnSync(int players, float multiplier)
        {
            if (Broken || players < 2)
                return;
            int count = Physics.OverlapSphereNonAlloc(transform.position, NearDistance, nearby, 1 << GameLayers.Raft,
                QueryTriggerInteraction.Ignore);
            if (count > 0)
                Break(transform.position);
        }

        public void Break(Vector3 source)
        {
            if (Broken)
                return;
            Broken = true;
            SetSolid(false);
            if (Services.IsReady)
                Services.Audio.Play(SoundId.CrateBreak, transform.position); // provisional: docs/08 no tiene sonido de globos
        }

        public void Restore()
        {
            Broken = false;
            SetSolid(true);
        }

        // Los hijos visuales y el collider sólido se apagan; el componente sigue escuchando.
        private void SetSolid(bool solid)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = solid;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = solid;
        }
    }
}
