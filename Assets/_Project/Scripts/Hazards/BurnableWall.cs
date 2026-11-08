using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Muro Burnable (docs/05 H12 y las cajas de cartón de W1-4): bloquea hasta que el locoto lo quema; tarda 'burnTime'
    // (0.5 s) en desaparecer. Vuelve al reaparecer en un checkpoint.
    public class BurnableWall : HazardBase, IBurnable
    {
        private float burnTimer = -1f;

        public float BurnTime => Config.Get("burnTime", 0.5f);
        public bool Burned { get; private set; }
        public bool IsBurning => burnTimer >= 0f;

        private void OnEnable() => GameEvents.OnRespawn += Restore;
        private void OnDisable() => GameEvents.OnRespawn -= Restore;

        public void Burn(Vector3 source)
        {
            if (Burned || IsBurning)
                return;
            burnTimer = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBurning)
                return;
            burnTimer += Time.deltaTime;
            float k = Mathf.Clamp01(burnTimer / Mathf.Max(0.01f, BurnTime));
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1f, 0.1f, 1f), k);
            if (k < 1f)
                return;
            burnTimer = -1f;
            Burned = true;
            SetSolid(false);
        }

        public void Restore()
        {
            burnTimer = -1f;
            Burned = false;
            transform.localScale = Vector3.one;
            SetSolid(true);
        }

        private void SetSolid(bool solid)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.enabled = solid;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = solid;
        }
    }
}
