using UnityEngine;

namespace GasSafari.Hazards
{
    // H01 Pared / orilla (docs/05): rebote físico con material de rebote 0.3. No hace daño; se supera con toques para alinear.
    public class BounceWall : MonoBehaviour
    {
        // docs/05: rebote 0.3
        public const float BOUNCE = 0.3f;

        [SerializeField] private HazardConfig config;

        private void Awake()
        {
            var material = new PhysicMaterial("H01_Wall")
            {
                bounciness = config != null ? config.Get("bounce", BOUNCE) : BOUNCE,
                dynamicFriction = 0.1f,
                staticFriction = 0.1f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.sharedMaterial = material;
        }
    }
}
