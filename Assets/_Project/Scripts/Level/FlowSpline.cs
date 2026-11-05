using UnityEngine;
using UnityEngine.Splines;

namespace GasSafari.Level
{
    // Cauce del nivel (docs/10 §5): dirección = tangente de la spline; intensidad por tramo según una curva.
    // Se hornea en muestras para consultar sin asignaciones; cada consulta parte del último tramo usado.
    [RequireComponent(typeof(SplineContainer))]
    public class FlowSpline : MonoBehaviour
    {
        private const float SAMPLE_SPACING = 1f;
        private const int LOCAL_SEARCH = 8;

        [Tooltip("Ancho del cauce en metros")]
        [SerializeField] private float width;
        [Tooltip("Aceleración de la corriente (m/s²) según la posición normalizada: 0 = salida, 1 = meta")]
        [SerializeField] private AnimationCurve intensity = new AnimationCurve();

        private Vector3[] positions;
        private Vector3[] directions;
        private float[] accelerations;

        public float Width => width;
        public float Length { get; private set; }
        public int SampleCount => positions.Length;

        private void Awake()
        {
            Bake();
        }

        public void Configure(float channelWidth, AnimationCurve curve)
        {
            width = channelWidth;
            intensity = curve;
            Bake();
        }

        public void Bake()
        {
            var container = GetComponent<SplineContainer>();
            Length = container.CalculateLength();
            int count = Mathf.Max(2, Mathf.CeilToInt(Length / SAMPLE_SPACING) + 1);
            positions = new Vector3[count];
            directions = new Vector3[count];
            accelerations = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                positions[i] = container.EvaluatePosition(t);
                accelerations[i] = intensity.Evaluate(t);
            }

            // Tangente por diferencias entre muestras vecinas: no se anula en nodos con tangente cero.
            for (int i = 0; i < count; i++)
            {
                var dir = positions[Mathf.Min(i + 1, count - 1)] - positions[Mathf.Max(i - 1, 0)];
                dir.y = 0f;
                directions[i] = dir.normalized;
            }
        }

        // hint: tramo de la consulta anterior de quien pregunta (se actualiza).
        public void GetFlow(Vector3 position, ref int hint, out Vector3 direction, out float acceleration)
        {
            int index = FindNearest(position, ref hint);
            direction = directions[index];
            acceleration = accelerations[index];
        }

        // Posición normalizada 0-1 a lo largo del cauce (bots, cámara, progreso).
        public float GetProgress(Vector3 position, ref int hint)
        {
            return FindNearest(position, ref hint) / (positions.Length - 1f);
        }

        // Punto del cauce 'distance' metros por delante de la posición (bots).
        public Vector3 GetPointAhead(Vector3 position, ref int hint, float distance)
        {
            return positions[AheadIndex(position, ref hint, distance)];
        }

        // Dirección del cauce 'distance' metros por delante de la posición (bots: ¿es recta larga?).
        public Vector3 GetDirectionAhead(Vector3 position, ref int hint, float distance)
        {
            return directions[AheadIndex(position, ref hint, distance)];
        }

        private int AheadIndex(Vector3 position, ref int hint, float distance)
        {
            int index = FindNearest(position, ref hint);
            float spacing = Length / (positions.Length - 1f);
            return Mathf.Min(positions.Length - 1, index + Mathf.RoundToInt(distance / Mathf.Max(spacing, 0.01f)));
        }

        private int FindNearest(Vector3 position, ref int hint)
        {
            int last = positions.Length - 1;
            hint = Mathf.Clamp(hint, 0, last);
            int lo = Mathf.Max(0, hint - LOCAL_SEARCH);
            int hi = Mathf.Min(last, hint + LOCAL_SEARCH);
            int best = Search(position, lo, hi);

            // En el borde de la ventana local (por ejemplo tras reaparecer): buscar en todo el cauce.
            if ((best == lo && lo > 0) || (best == hi && hi < last))
                best = Search(position, 0, last);

            hint = best;
            return best;
        }

        private int Search(Vector3 position, int from, int to)
        {
            int best = from;
            float bestDistance = float.MaxValue;
            for (int i = from; i <= to; i++)
            {
                float distance = (positions[i] - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }
    }
}
