using System.Collections.Generic;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Asientos en el borde de la balsa (docs/02 §2). 0° = proa, sentido horario visto desde arriba.
    [RequireComponent(typeof(RaftController))]
    public class RaftSeats : MonoBehaviour
    {
        private const int FREE_ANGLE_STEP = 5;

        [SerializeField] private BalanceConfig balance;
        [Tooltip("Altura local de los asientos respecto del centro de la balsa")]
        [SerializeField] private float seatHeight;

        private readonly List<PlayerSeat> seats = new List<PlayerSeat>();
        private readonly List<PlayerSeat> crew = new List<PlayerSeat>();

        public RaftController Controller { get; private set; }
        // Sentados ahora mismo.
        public IReadOnlyList<PlayerSeat> Seats => seats;
        // Todo el equipo de esta balsa, incluidos los que están en el agua.
        public IReadOnlyList<PlayerSeat> Crew => crew;

        private void Awake()
        {
            Controller = GetComponent<RaftController>();
        }

        public void Register(PlayerSeat seat)
        {
            if (!seats.Contains(seat))
                seats.Add(seat);
            if (!crew.Contains(seat))
                crew.Add(seat);
        }

        public void Unregister(PlayerSeat seat)
        {
            seats.Remove(seat);
        }

        public float[] GetStartAngles(int players)
        {
            return balance.startSeatAngles[players - 1].angles;
        }

        // Ángulo del borde más cercano a un punto del mundo.
        public float AngleTowards(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            return Mathf.Repeat(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, 360f);
        }

        // Distancia horizontal de un punto al borde (negativa si está dentro).
        public float DistanceToEdge(Vector3 worldPoint)
        {
            var offset = worldPoint - transform.position;
            offset.y = 0f;
            return offset.magnitude - balance.raftRadius;
        }

        // Sobre la cubierta y a más de 'margin' del borde, medido en el espacio de la balsa (sirve inclinada).
        public bool IsOverDeck(Vector3 worldPoint, float margin)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            float radial = new Vector2(local.x, local.z).magnitude;
            return local.y > 0f && radial < balance.raftRadius - margin;
        }

        // El ángulo libre más cercano al pedido, respetando la separación mínima.
        public float FindFreeAngle(float desired)
        {
            for (int step = 0; step <= 180; step += FREE_ANGLE_STEP)
            {
                if (IsFree(desired + step))
                    return Mathf.Repeat(desired + step, 360f);
                if (IsFree(desired - step))
                    return Mathf.Repeat(desired - step, 360f);
            }
            return Mathf.Repeat(desired, 360f);
        }

        private bool IsFree(float angle)
        {
            for (int i = 0; i < seats.Count; i++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angle, seats[i].Angle)) < balance.minSeatSeparation)
                    return false;
            }
            return true;
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
