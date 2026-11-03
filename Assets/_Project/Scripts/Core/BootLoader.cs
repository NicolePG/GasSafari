using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Core
{
    // Escena 00_Boot: los servicios ya están creados; pasa a la primera escena del juego.
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string firstScene = "01_Title";

        private void Start()
        {
            if (Application.CanStreamedLevelBeLoaded(firstScene))
                SceneManager.LoadSceneAsync(firstScene);
        }
    }
}
