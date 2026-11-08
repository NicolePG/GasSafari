using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Enciende un objeto cuando la balsa entra en la zona (lluvia de confeti del escenario final de W3-5) y lo apaga al
    // reaparecer antes de la zona.
    [RequireComponent(typeof(BoxCollider))]
    public class ActivateOnRaft : MonoBehaviour
    {
        [SerializeField] private GameObject target;

        public void Configure(GameObject objectToActivate) => target = objectToActivate;

        private void OnEnable() => GameEvents.OnRespawn += Reset;
        private void OnDisable() => GameEvents.OnRespawn -= Reset;

        private void Reset()
        {
            if (target != null)
                target.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (target != null && other.GetComponentInParent<RaftController>() != null)
                target.SetActive(true);
        }
    }
}
