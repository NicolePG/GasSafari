using UnityEngine;

namespace GasSafari.Modes
{
    // Raíz del prefab de un tramo de Infinito (docs/06 §1). El cauce local empieza en el origen mirando a +Z y termina en
    // el último punto de 'path'; el tramo siguiente se pone con su origen ahí y girado según la salida. Paredes y
    // obstáculos cuelgan de esta raíz. No se reciclan con pool: varios peligros guardan su posición del mundo al despertar
    // (cocodrilos, cangrejos, bloqueadores), así que cada tramo se crea inactivo en su lugar y se activa al acercarse.
    public class EndlessChunk : MonoBehaviour
    {
        [Tooltip("Cauce en coordenadas locales, un punto cada 2 m")] [SerializeField] private Vector3[] path = new Vector3[0];
        [Tooltip("Aceleración de la corriente (m/s²) en cada punto del cauce")] [SerializeField] private float[] accel = new float[0];

        public Vector3[] Path => path;
        public float[] Accel => accel;
        public EndlessChunkData Data { get; set; }
        public int Index { get; set; }

        public Vector3 ExitLocal => path.Length > 0 ? path[path.Length - 1] : Vector3.zero;

        public float ExitYawLocal
        {
            get
            {
                if (path.Length < 2)
                    return 0f;
                var dir = path[path.Length - 1] - path[path.Length - 2];
                return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            }
        }

        public void Configure(Vector3[] localPath, float[] localAccel)
        {
            path = localPath;
            accel = localAccel;
        }
    }
}
