using GasSafari.Hazards;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Tramo de la persecución de W1-5 (docs/04): al entrar la balsa, la grúa H24 empieza a atacar (un número de golpes
    // o sin límite con intervalo fijo) o se detiene.
    [RequireComponent(typeof(Collider))]
    public class CraneZone : MonoBehaviour
    {
        [SerializeField] private CraneBoss crane;
        [Tooltip("Golpes; 0 = sin límite")] [SerializeField] private int strikes;
        [Tooltip("s fijos entre golpes; 0 = patrón de H24")] [SerializeField] private float interval;
        [SerializeField] private bool stop;

        public void Configure(CraneBoss boss, int count, float seconds, bool stopAttacks)
        {
            crane = boss;
            strikes = count;
            interval = seconds;
            stop = stopAttacks;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (crane == null || other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent<RaftController>(out var raft))
                return;
            crane.SetRaft(raft);
            if (stop)
                crane.StopAttacks();
            else
                crane.StartAttacks(strikes, interval);
        }
    }
}
