using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H32 Red de pesca (docs/05): mientras la balsa está dentro, waterDrag x3. El Rugido la rompe (Fragile).
    // Vuelve al reaparecer. Avisa al acercarse la balsa (aviso por cercanía de HazardBase) mientras no está rota.
    public class FishingNet : HazardBase, IBreakable
    {
        private RaftBuoyancy caught;
        private Vector3 center;
        private float radius;

        public float DragMultiplier => Config.Get("dragMultiplier", 3f);
        public bool Broken { get; private set; }
        protected override bool WarnsOnApproach => !Broken;
        public override float WarningRadius => radius;
        public override Vector3 WarningCenter => center;

        private void Awake() => MeasureColliders(out center, out radius);

        private void OnEnable() => GameEvents.OnRespawn += Restore;

        private void OnDisable()
        {
            GameEvents.OnRespawn -= Restore;
            Release();
        }

        protected override void OnRaftEnter(RaftController raft)
        {
            if (Broken)
                return;
            caught = raft.Buoyancy;
            caught.ZoneDragMultiplier = DragMultiplier;
        }

        protected override void OnRaftExit(RaftController raft) => Release();

        public void Break(Vector3 source)
        {
            Broken = true;
            Release();
            SetVisible(false);
        }

        public void Restore()
        {
            Broken = false;
            SetVisible(true);
        }

        private void Release()
        {
            if (caught != null)
                caught.ZoneDragMultiplier = 1f;
            caught = null;
        }

        private void SetVisible(bool visible)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = visible;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = visible;
        }
    }
}
