using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Estampida de cocodrilos (docs/04 W3-4): al entrar la balsa en la zona, despierta de golpe a todos los cocodrilos
    // dormidos que tenga asignados. La cebolla (P06) y el Rugido los espantan.
    [RequireComponent(typeof(BoxCollider))]
    public class CrocStampede : MonoBehaviour
    {
        [SerializeField] private Crocodile[] herd;

        private bool triggered;

        public bool Triggered => triggered;

        public void Configure(Crocodile[] crocodiles) => herd = crocodiles;

        private void OnEnable() => Core.GameEvents.OnRespawn += Rearm;
        private void OnDisable() => Core.GameEvents.OnRespawn -= Rearm;

        private void Rearm() => triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered)
                return;
            var raft = other.GetComponentInParent<RaftController>();
            if (raft == null)
                return;
            triggered = true;
            for (int i = 0; i < herd.Length; i++)
            {
                if (herd[i] != null)
                    herd[i].Wake(raft);
            }
        }
    }
}
