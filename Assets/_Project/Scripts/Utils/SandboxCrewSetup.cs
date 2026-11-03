using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Utils
{
    // Escenas de prueba: sienta a los animales ya colocados en sus posiciones iniciales.
    // En los niveles lo hace RaftSpawner.
    public class SandboxCrewSetup : MonoBehaviour
    {
        [SerializeField] private RaftSeats raft;
        [SerializeField] private PlayerSeat[] crew;

        private void Start()
        {
            var angles = raft.GetStartAngles(crew.Length);
            for (int i = 0; i < crew.Length; i++)
                crew[i].Sit(raft, angles[i]);
        }
    }
}
