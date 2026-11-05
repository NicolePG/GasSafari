using UnityEngine;

namespace GasSafari.Core
{
    // Raíz persistente de los servicios (docs/10 §4). Solo puede existir una.
    [DefaultExecutionOrder(-1000)]
    public class GameServices : MonoBehaviour
    {
        private void Awake()
        {
            if (Services.IsReady && Services.Root != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            // El guardado vive en el prefab de servicios; si falta, se agrega.
            if (GetComponentInChildren<Progression.ProgressionService>() == null)
                gameObject.AddComponent<Progression.ProgressionService>();
            Services.Bind(this);
        }

        private void OnDestroy()
        {
            Services.Unbind(this);
        }
    }
}
