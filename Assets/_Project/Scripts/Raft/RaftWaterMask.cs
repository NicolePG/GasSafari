using UnityEngine;

namespace GasSafari.Raft
{
    // Máscara de agua (docs/09 §2): cilindro bajo sobre el piso de la balsa que solo escribe profundidad,
    // para que el agua (y sus olas) no se dibuje dentro del bote. Va en la balsa, en cada skin de la tienda y en la mini balsa.
    // La malla se arma al empezar a partir del piso, así sirve para cualquier modelo sin ajustes a mano.
    public class RaftWaterMask : MonoBehaviour
    {
        public const string MASK_NAME = "WaterMask";
        private const string SHADER_NAME = "GasSafari/SG_WaterMask";

        [Tooltip("Piso de la balsa. Vacío: busca un renderer cuyo nombre contenga 'Floor' o, si no, usa el conjunto de la balsa")]
        [SerializeField] private Renderer floor;
        [Tooltip("Material de SG_WaterMask. Vacío: se crea en ejecución")]
        [SerializeField] private Material maskMaterial;
        [Tooltip("Altura de la máscara sobre el piso (m): cubre las olas visuales (0.12 m) con margen")]
        [SerializeField] private float height = 0.35f;
        [Tooltip("Fracción del radio del piso que cubre la máscara; menos de 1 para no asomar entre los tubos")]
        [SerializeField] private float radiusScale = 0.95f;
        [SerializeField] private int segments = 24;

        private static Material sharedMaterial;

        public GameObject Mask { get; private set; }

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            if (Mask != null)
                return;

            ResolveFloor(out var parent, out var bounds);
            if (parent == null)
                return;

            var material = maskMaterial != null ? maskMaterial : GetSharedMaterial();
            if (material == null)
                return;

            Mask = new GameObject(MASK_NAME);
            Mask.transform.SetParent(parent, false);
            Mask.layer = gameObject.layer;
            Mask.AddComponent<MeshFilter>().sharedMesh = BuildMesh(bounds, parent.lossyScale);
            var renderer = Mask.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        // Agrega la máscara a una balsa (skins de la tienda y mini balsa): devuelve el componente.
        public static RaftWaterMask Attach(GameObject root)
        {
            var mask = root.GetComponent<RaftWaterMask>();
            return mask != null ? mask : root.AddComponent<RaftWaterMask>();
        }

        // 'parent': transform al que se cuelga la máscara; 'bounds': caja del piso en el espacio local de 'parent'.
        private void ResolveFloor(out Transform parent, out Bounds bounds)
        {
            if (floor == null)
            {
                foreach (var candidate in GetComponentsInChildren<Renderer>(true))
                {
                    if (candidate.name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        floor = candidate;
                        break;
                    }
                }
            }

            if (floor != null && floor.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
            {
                parent = floor.transform;
                bounds = filter.sharedMesh.bounds;
                return;
            }

            // Sin piso reconocible: la caja de todos los renderers, con el piso en el tercio inferior.
            var all = GetComponentsInChildren<Renderer>(true);
            if (all.Length == 0)
            {
                parent = null;
                bounds = default;
                return;
            }
            var world = all[0].bounds;
            for (int i = 1; i < all.Length; i++)
                world.Encapsulate(all[i].bounds);
            parent = transform;
            var center = transform.InverseTransformPoint(world.center);
            var size = transform.InverseTransformVector(world.size);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            // El piso queda a un tercio de la altura de la balsa; la máscara se apoya ahí.
            bounds = new Bounds(new Vector3(center.x, center.y - size.y * 0.17f, center.z), new Vector3(size.x * 0.6f, size.y * 0.33f, size.z * 0.6f));
        }

        // Cilindro cerrado: base en la cara superior del piso, tapa 'height' metros más arriba.
        private Mesh BuildMesh(Bounds bounds, Vector3 scale)
        {
            float sx = Mathf.Max(Mathf.Abs(scale.x), 1e-4f);
            float sy = Mathf.Max(Mathf.Abs(scale.y), 1e-4f);
            float sz = Mathf.Max(Mathf.Abs(scale.z), 1e-4f);
            // El radio sale del lado menor del piso en metros; se reparte por eje para no deformarlo.
            float radiusWorld = Mathf.Min(bounds.extents.x * sx, bounds.extents.z * sz) * radiusScale;
            float rx = radiusWorld / sx;
            float rz = radiusWorld / sz;
            float y0 = bounds.max.y;
            float y1 = y0 + height / sy;
            var center = bounds.center;

            int n = Mathf.Max(8, segments);
            var vertices = new Vector3[n * 2 + 2];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                float x = center.x + Mathf.Cos(a) * rx;
                float z = center.z + Mathf.Sin(a) * rz;
                vertices[i] = new Vector3(x, y0, z);
                vertices[n + i] = new Vector3(x, y1, z);
            }
            vertices[n * 2] = new Vector3(center.x, y0, center.z);
            vertices[n * 2 + 1] = new Vector3(center.x, y1, center.z);

            var triangles = new int[n * 12];
            int t = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                // Pared (cara hacia afuera), tapa (hacia arriba) y base (hacia abajo).
                triangles[t++] = i; triangles[t++] = n + i; triangles[t++] = n + j;
                triangles[t++] = i; triangles[t++] = n + j; triangles[t++] = j;
                triangles[t++] = n * 2 + 1; triangles[t++] = n + j; triangles[t++] = n + i;
                triangles[t++] = n * 2; triangles[t++] = i; triangles[t++] = j;
            }

            var mesh = new Mesh { name = "WaterMask" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material GetSharedMaterial()
        {
            if (sharedMaterial != null)
                return sharedMaterial;
            var shader = Shader.Find(SHADER_NAME);
            if (shader == null)
                return null;
            sharedMaterial = new Material(shader) { name = "M_WaterMask (runtime)", hideFlags = HideFlags.HideAndDontSave };
            return sharedMaterial;
        }
    }
}
