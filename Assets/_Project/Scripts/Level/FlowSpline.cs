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
        private const float FAR_MARGIN = 6f;   // m fuera del borde del cauce desde los que la ventana local ya no basta

        [Tooltip("Ancho del cauce en metros")]
        [SerializeField] private float width;
        [Tooltip("Aceleración de la corriente (m/s²) según la posición normalizada: 0 = salida, 1 = meta")]
        [SerializeField] private AnimationCurve intensity = new AnimationCurve();

        private Vector3[] positions;
        private Vector3[] directions;
        private float[] accelerations;

        public float Width => width;
        public float Length { get; private set; }
        public int SampleCount => positions != null ? positions.Length : 0;
        // Altura del punto más bajo del cauce (red de seguridad de CheckpointSystem).
        public float MinHeight { get; private set; }

        private bool runtimePath;

        private void Awake()
        {
            if (!runtimePath)
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

            MinHeight = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                positions[i] = container.EvaluatePosition(t);
                accelerations[i] = intensity.Evaluate(t);
                MinHeight = Mathf.Min(MinHeight, positions[i].y);
            }

            // Tangente por diferencias entre muestras vecinas: no se anula en nodos con tangente cero.
            for (int i = 0; i < count; i++)
            {
                var dir = positions[Mathf.Min(i + 1, count - 1)] - positions[Mathf.Max(i - 1, 0)];
                dir.y = 0f;
                directions[i] = dir.normalized;
            }
        }

        // Cauce armado en tiempo de ejecución (Infinito, docs/06 §1): una polilínea con la aceleración de cada punto. Se
        // remuestrea cada SAMPLE_SPACING m; la SplineContainer de la escena deja de usarse.
        public void SetPath(System.Collections.Generic.IReadOnlyList<Vector3> points, System.Collections.Generic.IReadOnlyList<float> pointAccel, float channelWidth)
        {
            runtimePath = true;
            width = channelWidth;
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
                total += Vector3.Distance(points[i - 1], points[i]);
            Length = total;
            int count = Mathf.Max(2, Mathf.CeilToInt(total / SAMPLE_SPACING) + 1);
            if (positions == null || positions.Length != count)
            {
                positions = new Vector3[count];
                directions = new Vector3[count];
                accelerations = new float[count];
            }
            float step = total / (count - 1f);
            int segment = 1;
            float segmentStart = 0f;
            float segmentLength = points.Count > 1 ? Vector3.Distance(points[0], points[1]) : 0f;
            MinHeight = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                float at = i * step;
                while (segment < points.Count - 1 && at > segmentStart + segmentLength)
                {
                    segmentStart += segmentLength;
                    segment++;
                    segmentLength = Vector3.Distance(points[segment - 1], points[segment]);
                }
                float t = segmentLength > 1e-4f ? Mathf.Clamp01((at - segmentStart) / segmentLength) : 0f;
                int a = Mathf.Max(0, segment - 1);
                int b = Mathf.Min(points.Count - 1, segment);
                positions[i] = Vector3.Lerp(points[a], points[b], t);
                accelerations[i] = Mathf.Lerp(pointAccel[a], pointAccel[b], t);
                MinHeight = Mathf.Min(MinHeight, positions[i].y);
            }
            for (int i = 0; i < count; i++)
            {
                var dir = positions[Mathf.Min(i + 1, count - 1)] - positions[Mathf.Max(i - 1, 0)];
                dir.y = 0f;
                directions[i] = dir.normalized;
            }
        }

        // Metros recorridos a lo largo del cauce hasta el punto más cercano a la posición (Infinito: distancia y dificultad).
        public float GetDistance(Vector3 position, ref int hint)
        {
            return FindNearest(position, ref hint) * Length / Mathf.Max(1f, positions.Length - 1f);
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
            int best = Search(position, lo, hi, out float bestDistance);

            // En el borde de la ventana local (por ejemplo tras reaparecer) o lejos del cauce (un ramal como el atajo de W5-3, que
            // corta 300 m de curvas: la ventana se quedaba en la primera curva y los bots volvían hacia ella al salir): todo el cauce.
            float far = width * 0.5f + FAR_MARGIN;
            if ((best == lo && lo > 0) || (best == hi && hi < last) || bestDistance > far * far)
                best = Search(position, 0, last, out _);

            hint = best;
            return best;
        }

        private int Search(Vector3 position, int from, int to, out float bestDistance)
        {
            int best = from;
            bestDistance = float.MaxValue;
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
