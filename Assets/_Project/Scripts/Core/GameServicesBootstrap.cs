using UnityEngine;

namespace GasSafari.Core
{
    // Permite dar Play desde cualquier escena en el editor. En la build los crea 00_Boot.
    public static class GameServicesBootstrap
    {
        private const string RESOURCE_PATH = "GameServices";

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureServices()
        {
            if (Services.IsReady)
                return;

            var prefab = Resources.Load<GameServices>(RESOURCE_PATH);
            var instance = Object.Instantiate(prefab);
            instance.name = prefab.name;
        }
#endif
    }
}
