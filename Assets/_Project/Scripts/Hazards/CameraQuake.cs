using GasSafari.Raft;
using GasSafari.VFX;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Temblores (docs/04 W4-5): mientras la balsa está en la zona, la cámara se sacude cada 'interval' s.
    [RequireComponent(typeof(BoxCollider))]
    public class CameraQuake : MonoBehaviour
    {
        [SerializeField] private float interval = 7f;
        [SerializeField] private float force = 0.5f;

        private bool inside;
        private float timer;

        public int Quakes { get; private set; }

        public void Configure(float seconds, float strength)
        {
            interval = seconds;
            force = strength;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<RaftController>() != null)
                inside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<RaftController>() != null)
                inside = false;
        }

        private void Update()
        {
            if (!inside)
                return;
            timer += Time.deltaTime;
            if (timer < interval)
                return;
            timer = 0f;
            var rig = FindFirstObjectByType<RaftCameraRig>();
            if (rig != null)
                rig.Shake(force);
            Quakes++;
        }
    }
}
