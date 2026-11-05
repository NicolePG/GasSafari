using System;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Meta (docs/10 §5): detiene el cronómetro cuando la balsa atraviesa el trigger.
    [RequireComponent(typeof(Collider))]
    public class FinishGate : MonoBehaviour
    {
        public event Action<RaftController> Crossed;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<RaftController>(out var raft))
                Crossed?.Invoke(raft);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
