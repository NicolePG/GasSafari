using UnityEngine;

namespace GasSafari.Utils
{
    // Partes móviles de los props (noria, faro, hélices, aspas; skill level-builder): gira sobre un eje fijo del mundo y
    // arrastra en círculo a sus 'orbiters' (cabinas) sin girarlos, para que sigan colgando derechos.
    public class Spinner : MonoBehaviour
    {
        [Tooltip("Eje de giro en el espacio local del padre")] [SerializeField] private Vector3 parentAxis = Vector3.forward;
        [Tooltip("Grados por segundo")] [SerializeField] private float speed = 12f;
        [SerializeField] private Transform[] orbiters = System.Array.Empty<Transform>();

        private Vector3 axis;

        public void Configure(Vector3 axisInParent, float degreesPerSecond, Transform[] cabins)
        {
            parentAxis = axisInParent;
            speed = degreesPerSecond;
            orbiters = cabins ?? System.Array.Empty<Transform>();
        }

        private void Awake()
        {
            axis = transform.parent != null ? transform.parent.TransformDirection(parentAxis) : parentAxis;
            axis.Normalize();
        }

        private void Update()
        {
            float angle = speed * Time.deltaTime;
            var pivot = transform.position;
            transform.Rotate(axis, angle, Space.World);
            if (orbiters.Length == 0)
                return;
            var step = Quaternion.AngleAxis(angle, axis);
            for (int i = 0; i < orbiters.Length; i++)
                orbiters[i].position = pivot + step * (orbiters[i].position - pivot);
        }
    }
}
