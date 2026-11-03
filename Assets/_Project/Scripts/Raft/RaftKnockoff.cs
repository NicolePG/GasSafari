using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Un choque fuerte de la balsa tira al agua a los animales que no están agarrados (docs/02 §9).
    [RequireComponent(typeof(RaftController), typeof(RaftSeats))]
    public class RaftKnockoff : MonoBehaviour
    {
        private readonly List<PlayerSeat> buffer = new List<PlayerSeat>();
        private RaftController raft;
        private RaftSeats seats;
        private Vector3 lastVelocity;

        private void Awake()
        {
            raft = GetComponent<RaftController>();
            seats = GetComponent<RaftSeats>();
        }

        private void FixedUpdate()
        {
            lastVelocity = raft.Body.velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            int layer = collision.gameObject.layer;
            if (layer != GameLayers.Environment && layer != GameLayers.Hazard && layer != GameLayers.Raft)
                return;

            // Los animales siguen con la velocidad que la balsa perdió en el choque.
            var kick = lastVelocity - raft.Body.velocity;
            float impulse = collision.impulse.magnitude;

            buffer.Clear();
            buffer.AddRange(seats.Seats);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].GetComponent<AvatarFall>().Knockoff(impulse, kick);
        }
    }
}
