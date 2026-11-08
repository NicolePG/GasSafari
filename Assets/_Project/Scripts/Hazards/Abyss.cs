using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H22 Abismo (docs/05): hueco sin agua (WaterGap del nivel); caer = reaparición en el checkpoint. Este trigger va debajo.
    [RequireComponent(typeof(BoxCollider))]
    public class Abyss : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<RaftController>() == null)
                return;
            var checkpoints = FindFirstObjectByType<CheckpointSystem>();
            if (checkpoints != null)
                checkpoints.RequestRespawn();
        }
    }
}
