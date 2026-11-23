using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Rey de la Balsa (docs/06 §1): la balsa gira sin parar en el centro de la piscina redonda. Un resorte suave la
    // devuelve al centro (provisional) para que los pedos la muevan sin estrellarla siempre contra el borde.
    [RequireComponent(typeof(RaftController))]
    public class RaftSpinner : MonoBehaviour
    {
        private const float SPIN_GAIN = 4f;
        private const float CENTER_SPRING = 1.2f;
        private const float CENTER_DAMPING = 0.8f;

        private Rigidbody rb;
        private Vector3 center;
        private float targetSpin;

        public float TargetSpin => targetSpin;

        public void Setup(float degreesPerSecond, Vector3 anchor)
        {
            rb = GetComponent<RaftController>().Body;
            targetSpin = degreesPerSecond * Mathf.Deg2Rad;
            center = anchor;
        }

        private void FixedUpdate()
        {
            if (rb == null)
                return;
            float error = targetSpin - rb.angularVelocity.y;
            rb.AddTorque(Vector3.up * (error * SPIN_GAIN), ForceMode.Acceleration);
            var offset = center - rb.position;
            offset.y = 0f;
            var velocity = rb.velocity;
            velocity.y = 0f;
            rb.AddForce(offset * CENTER_SPRING - velocity * CENTER_DAMPING, ForceMode.Acceleration);
        }
    }
}
