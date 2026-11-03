using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Punto de control (docs/02 §10). Su transform es la pose donde reaparece la balsa.
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Extra: solo existe en Familiar")]
        [SerializeField] private bool familiarOnly;
        [Tooltip("Se mantiene en Amistades Rotas")]
        [SerializeField] private bool hardKeep;

        private CheckpointSystem system;

        public bool FamiliarOnly => familiarOnly;
        public bool HardKeep => hardKeep;

        public void Bind(CheckpointSystem owner)
        {
            system = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (system == null || other.attachedRigidbody == null)
                return;
            if (other.attachedRigidbody.TryGetComponent<RaftController>(out var raft))
                system.Reach(this, raft);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = familiarOnly ? Color.green : hardKeep ? Color.red : Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
