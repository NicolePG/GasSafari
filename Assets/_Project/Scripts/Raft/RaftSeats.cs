using System.Collections.Generic;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Asientos en el borde de la balsa (docs/02 §2). 0° = proa, sentido horario visto desde arriba.
    [RequireComponent(typeof(RaftController))]
    public class RaftSeats : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;
        [Tooltip("Altura local de los asientos respecto del centro de la balsa")]
        [SerializeField] private float seatHeight;

        private readonly List<PlayerSeat> seats = new List<PlayerSeat>();

        public RaftController Controller { get; private set; }
        public IReadOnlyList<PlayerSeat> Seats => seats;

        private void Awake()
        {
            Controller = GetComponent<RaftController>();
        }

        public void Register(PlayerSeat seat)
        {
            if (!seats.Contains(seat))
                seats.Add(seat);
        }

        public void Unregister(PlayerSeat seat)
        {
            seats.Remove(seat);
        }

        public float[] GetStartAngles(int players)
        {
            return balance.startSeatAngles[players - 1].angles;
        }

        public Vector3 LocalSeatPosition(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad) * balance.raftRadius, seatHeight, Mathf.Cos(rad) * balance.raftRadius);
        }

        // Recorta el giro de un asiento para respetar la separación mínima con los demás.
        public float ClampMove(PlayerSeat seat, float delta)
        {
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] != seat)
                    delta = ClampDelta(seat.Angle, seats[i].Angle, delta, balance.minSeatSeparation);
            }
            return delta;
        }

        public static float ClampDelta(float from, float other, float delta, float minSeparation)
        {
            if (delta > 0f)
            {
                float gap = Mathf.Repeat(other - from, 360f) - minSeparation;
                return Mathf.Min(delta, Mathf.Max(0f, gap));
            }
            if (delta < 0f)
            {
                float gap = Mathf.Repeat(from - other, 360f) - minSeparation;
                return Mathf.Max(delta, -Mathf.Max(0f, gap));
            }
            return 0f;
        }
    }
}
