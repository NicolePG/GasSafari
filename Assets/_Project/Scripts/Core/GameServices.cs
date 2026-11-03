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
            Services.Bind(this);
        }

        private void OnDestroy()
        {
            Services.Unbind(this);
        }
    }
}
