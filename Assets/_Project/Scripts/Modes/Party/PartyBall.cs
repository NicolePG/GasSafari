using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Modes
{
    // Pelota de playa gigante de Balsa-Gol (docs/06 §1): flota sobre WaterSurface y la mueven los choques de las balsas.
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class PartyBall : MonoBehaviour
    {
        [Tooltip("Empuje hacia arriba sumergida entera, en múltiplos de su peso")]
        [SerializeField] private float buoyancy = 2.5f;
        [SerializeField] private float waterDrag = 0.6f;
        [SerializeField] private float waterAngularDrag = 0.8f;

        private Rigidbody rb;
        private float radius;

        public Rigidbody Body => rb;
        public float Radius => radius;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        }

        public void Setup(float ballRadius, float mass)
        {
            transform.localScale = Vector3.one * (ballRadius / GetComponent<SphereCollider>().radius);
            radius = ballRadius;
            GetComponent<Rigidbody>().mass = mass;
        }

        public void ResetTo(Vector3 position)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            transform.position = position;
        }

        private void FixedUpdate()
        {
            var center = rb.worldCenterOfMass;
            if (!WaterSurface.TryGetHeight(center, out float water))
            {
                rb.drag = 0.05f;
                return;
            }
            float submerged = Mathf.Clamp01((water - (center.y - radius)) / (2f * radius));
            rb.drag = submerged > 0f ? waterDrag : 0.05f;
            rb.angularDrag = submerged > 0f ? waterAngularDrag : 0.05f;
            if (submerged > 0f)
                rb.AddForce(-Physics.gravity * (buoyancy * submerged), ForceMode.Acceleration);
        }
    }
}
