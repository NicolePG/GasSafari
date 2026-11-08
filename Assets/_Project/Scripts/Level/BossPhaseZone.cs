using GasSafari.Hazards;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Tramo de un jefe con fases (W2-5 tentáculos): al entrar la balsa, el jefe pasa a 'phase' (-1 = se detiene).
    [RequireComponent(typeof(Collider))]
    public class BossPhaseZone : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour boss;
        [SerializeField] private int phase;

        public void Configure(MonoBehaviour phasedBoss, int bossPhase)
        {
            boss = phasedBoss;
            phase = bossPhase;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (boss is IPhasedBoss phased && other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<RaftController>(out _))
                phased.SetPhase(phase);
        }
    }
}
