using System;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Trigger de la lección 3 (rodear la boya): avisa cuando la balsa lo cruza. El orden lo lleva TutorialLesson.
    [RequireComponent(typeof(Collider))]
    public class TutorialGate : MonoBehaviour
    {
        public event Action<TutorialGate> Passed;

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<RaftController>(out _))
                Passed?.Invoke(this);
        }
    }
}
