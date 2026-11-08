using UnityEngine;

namespace GasSafari.VFX
{
    // Partículas de ambiente (lluvia, hojas, luciérnagas) que acompañan a la cámara en lugar de cubrir todo el nivel.
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 12f, 8f);

        private Transform target;

        private void LateUpdate()
        {
            if (target == null)
            {
                var cam = Camera.main;
                if (cam == null)
                    return;
                target = cam.transform;
            }
            var forward = target.forward;
            forward.y = 0f;
            transform.position = target.position + Vector3.up * offset.y + forward.normalized * offset.z;
        }
    }
}
