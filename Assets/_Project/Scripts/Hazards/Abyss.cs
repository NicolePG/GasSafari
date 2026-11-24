using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H22 Abismo (docs/05): hueco sin agua (WaterGap del nivel); caer = reaparición en el checkpoint. Este trigger va debajo.
    // Ayuda: si el equipo cae 'fallsForHelp' veces (2) en el mismo abismo, aparece el puente de boyas que lo cruza.
    [RequireComponent(typeof(BoxCollider))]
    public class Abyss : MonoBehaviour
    {
        // La balsa tiene varios colliders y una misma caída los cruza todos: cuenta una sola caída por este lapso.
        private const float FALL_DEBOUNCE = 2f;

        [Tooltip("Puente de boyas (desactivado) que aparece tras caer fallsForHelp veces")]
        [SerializeField] private GameObject helpBridge;
        [SerializeField] private int fallsForHelp = 2;

        private float lastFall = -100f;

        public int Falls { get; private set; }
        public bool HelpActive => helpBridge != null && helpBridge.activeSelf;

        // El trigger va horizontal y bien por debajo del puente de boyas: colocado con la pendiente de una rampa (W5-2, 21.8°) su
        // esquina trasera subía hasta la línea del agua en el borde del hueco, y la balsa reaparecía antes de caer y también al
        // cruzar el puente (W5-2 no se terminaba). Se conserva solo el giro en Y.
        private const float BRIDGE_CLEARANCE = 4f;

        private void Awake()
        {
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (helpBridge == null)
                return;
            var box = GetComponent<BoxCollider>();
            float top = transform.TransformPoint(box.center).y + box.size.y * 0.5f * transform.lossyScale.y;
            float maxTop = helpBridge.transform.position.y - BRIDGE_CLEARANCE;
            if (top > maxTop)
                transform.position += Vector3.down * (top - maxTop);
        }

        public void Configure(GameObject bridge, int falls = 2)
        {
            helpBridge = bridge;
            fallsForHelp = falls;
        }

        // Registra una caída; devuelve si es nueva (no un cruce repetido del mismo trigger).
        public bool RegisterFall()
        {
            if (Time.unscaledTime - lastFall < FALL_DEBOUNCE)
                return false;
            lastFall = Time.unscaledTime;
            Falls++;
            if (Falls >= fallsForHelp && helpBridge != null)
                helpBridge.SetActive(true);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var raft = other.GetComponentInParent<RaftController>();
            if (raft == null)
                return;
            RegisterFall();
            var checkpoints = FindFirstObjectByType<CheckpointSystem>();
            if (checkpoints != null)
                checkpoints.RequestRespawn(raft);
        }
    }
}
