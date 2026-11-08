using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Final de W5-5 (docs/04): al llegar la balsa a la rampa final, el cocodrilo gigante se coloca justo detrás y cierra la boca
    // (momento estrella). Un solo disparo por intento; GiantCrocChaser.Snap hace el trabajo.
    [RequireComponent(typeof(BoxCollider))]
    public class CrocFinaleZone : MonoBehaviour
    {
        [SerializeField] private GiantCrocChaser croc;

        public bool Played { get; private set; }

        public void Configure(GiantCrocChaser chaser) => croc = chaser;

        private void OnEnable() => Core.GameEvents.OnRespawn += Rearm;
        private void OnDisable() => Core.GameEvents.OnRespawn -= Rearm;

        private void Rearm() => Played = false;

        private void OnTriggerEnter(Collider other)
        {
            if (Played || croc == null)
                return;
            var raft = other.GetComponentInParent<RaftController>();
            if (raft == null)
                return;
            Played = true;
            croc.Snap(raft);
        }
    }
}
