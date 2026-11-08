using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Flotación simple de un objeto suelto (cajas que caen al agua, cajas a la deriva): un punto en el centro de masa,
    // empuje proporcional a la profundidad con amortiguación, y arrastre de agua. Sin efecto con el Rigidbody cinemático.
    [RequireComponent(typeof(Rigidbody))]
    public class FloatingBody : MonoBehaviour
    {
        [Tooltip("m de profundidad con la que flota en equilibrio")] [SerializeField] private float floatDepth = 0.5f;
        [Tooltip("Amortiguación vertical (1/s)")] [SerializeField] private float damping = 2f;
        [SerializeField] private float waterDrag = 1f;
        [SerializeField] private float waterAngularDrag = 1.5f;

        private Rigidbody body;

        private void Awake() => body = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            if (body.isKinematic)
                return;
            var center = body.worldCenterOfMass;
            if (!WaterSurface.TryGetHeight(center, out float water))
            {
                body.drag = 0f;
                return;
            }
            float depth = water - center.y;
            if (depth <= 0f)
            {
                body.drag = 0.05f;
                return;
            }
            float g = -Physics.gravity.y;
            // En equilibrio a floatDepth: aceleración g * depth / floatDepth; más amortiguación de la velocidad vertical.
            float accel = g * Mathf.Min(depth / floatDepth, 3f) - body.velocity.y * damping;
            body.AddForce(Vector3.up * accel, ForceMode.Acceleration);
            body.drag = waterDrag;
            body.angularDrag = waterAngularDrag;
        }
    }
}
