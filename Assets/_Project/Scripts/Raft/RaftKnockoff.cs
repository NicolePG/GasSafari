using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Un choque fuerte de la balsa tira al agua a los animales que no están agarrados (docs/02 §9).
    // Cuenta solo la velocidad con que la balsa golpea la superficie (componente sobre la normal), no la fuerza
    // del contacto: un roce a lo largo de una pared o un choque lento no tira a nadie, sea cual sea la masa de la balsa.
    [RequireComponent(typeof(RaftController), typeof(RaftSeats))]
    public class RaftKnockoff : MonoBehaviour
    {
        private readonly List<PlayerSeat> buffer = new List<PlayerSeat>();
        private RaftSeats seats;

        private void Awake()
        {
            seats = GetComponent<RaftSeats>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            int layer = collision.gameObject.layer;
            if (layer != GameLayers.Environment && layer != GameLayers.Hazard && layer != GameLayers.Raft)
                return;

            float impact = NormalImpactSpeed(collision);
            if (impact <= 0f)
                return;

            buffer.Clear();
            buffer.AddRange(seats.Seats);
            for (int i = 0; i < buffer.Count; i++)
                buffer[i].GetComponent<AvatarFall>().Knockoff(impact, impact * 0.5f);
        }

        // Mayor componente de la velocidad relativa contra la normal de algún contacto (m/s).
        public static float NormalImpactSpeed(Collision collision)
        {
            float best = 0f;
            for (int i = 0; i < collision.contactCount; i++)
            {
                float along = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(i).normal));
                if (along > best)
                    best = along;
            }
            return best;
        }
    }
}
