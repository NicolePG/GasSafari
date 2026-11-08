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
            if (other.GetComponentInParent<RaftController>() == null)
                return;
            RegisterFall();
            var checkpoints = FindFirstObjectByType<CheckpointSystem>();
            if (checkpoints != null)
                checkpoints.RequestRespawn();
        }
    }
}
