using System;
using UnityEngine;

namespace GasSafari.Modes
{
    // Portería de Balsa-Gol: un trigger detrás de la línea. Avisa cuando entra la pelota.
    [RequireComponent(typeof(Collider))]
    public class GoalZone : MonoBehaviour
    {
        [Tooltip("Equipo que defiende esta portería (0 o 1): el gol es para el otro")]
        [SerializeField] private int team;

        public int Team => team;
        public event Action<GoalZone> BallEntered;

        public void Configure(int defendingTeam) => team = defendingTeam;

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PartyBall>() != null)
                BallEntered?.Invoke(this);
        }
    }
}
