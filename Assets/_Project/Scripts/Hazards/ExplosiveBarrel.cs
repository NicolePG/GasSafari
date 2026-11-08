using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H06 Barril explosivo (docs/05): al tocarlo la balsa se enciende la mecha (aviso = warningTime 1.5 s) y explota en un
    // radio de 4 m: revienta 1 cámara (la más cercana) y empuja 35. El Rugido lo rompe antes sin explosión (Fragile).
    public class ExplosiveBarrel : HazardBase, IBreakable
    {
        private readonly Collider[] hits = new Collider[8];

        public float Radius => Config.Get("radius", 4f);
        public float Push => Config.Get("push", 35f);
        public bool Gone { get; private set; }

        private void OnEnable() => GameEvents.OnRespawn += Restore;
        private void OnDisable() => GameEvents.OnRespawn -= Restore;

        protected override void OnRaftEnter(RaftController raft)
        {
            if (!Gone && !IsWarning)
                Warn();
        }

        protected override void OnWarn()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.material.SetColor("_BaseColor", new Color(1f, 0.3f, 0.1f)); // la mecha encendida
        }

        protected override void OnWarningEnded()
        {
            if (Gone)
                return;
            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, hits, 1 << GameLayers.Raft, QueryTriggerInteraction.Ignore);
            RaftController hitRaft = null;
            for (int i = 0; i < count && hitRaft == null; i++)
            {
                if (hits[i].attachedRigidbody != null)
                    hits[i].attachedRigidbody.TryGetComponent(out hitRaft);
            }
            if (hitRaft != null)
            {
                var closest = hitRaft.Body.ClosestPointOnBounds(transform.position);
                DamageRaft(hitRaft, closest);
                var away = hitRaft.Body.worldCenterOfMass - transform.position;
                away.y = 0f;
                PushRaft(hitRaft, away.normalized * Push, hitRaft.Body.worldCenterOfMass);
            }
            if (Services.IsReady)
                Services.Audio.Play(SoundId.RaftHitHard, transform.position); // provisional: docs/08 no tiene explosión
            SetGone(true);
        }

        public void Break(Vector3 source)
        {
            CancelWarning();
            SetGone(true);
        }

        public void Restore()
        {
            CancelWarning();
            SetGone(false);
        }

        private void SetGone(bool gone)
        {
            Gone = gone;
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = !gone;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = !gone;
        }
    }
}
