using UnityEngine;

namespace GasSafari.Level
{
    // Puerta de salida (docs/10 §5): su transform es la pose inicial de la balsa. El cronómetro arranca tras la cuenta 3-2-1.
    public class StartGate : MonoBehaviour
    {
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(4f, 2f, 0.2f));
        }
    }
}
