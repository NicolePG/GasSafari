using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Costo de los fondos por capas (Tools/Export/Backdrops): en 2 niveles por mundo, desde la cámara del juego en 3 puntos del
    // recorrido, mide ms por cuadro, triángulos y batches con y sin la raíz "Backdrops". No va en la suite: [Explicit].
    // Escribe Screenshots/backdrop_perf.csv (nivel,punto,variante,ms,fps,tris,batches,setpass). Son números del Editor (incluye
    // su propio costo de interfaz): sirven para comparar con/sin, no como FPS de la build.
    public class BackdropPerfTests
    {
        private const string FOLDER = "Assets/_Project/Scenes/Juego/Niveles/";
        private const int FRAMES = 90;
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };
        private static readonly string[] Levels =
        {
            "N01_W1-1_FuentesZoo", "N03_W1-3_RioCiudad",
            "N07_W2-2_Tormenta", "N09_W2-4_GranRemolino",
            "N11_W3-1_PlayaLlegada", "N13_W3-3_TerritorioFosas",
            "N17_W4-2_CuevaCristal", "N20_W4-5_VolcanJefe",
            "N21_W5-1_RapidosCanon", "N25_W5-5_MegatoboganJefe",
            "N27_W6-2_NocheSinLuz", "N30_W6-5_CoronaRey",
        };
        private static readonly float[] Fractions = { 0.15f, 0.5f, 0.85f };

        [UnityTest, Explicit("Medición de rendimiento de los fondos"), Timeout(3600000)]
        public IEnumerator MeasureBackdrops()
        {
#if UNITY_EDITOR
            string csv = "Screenshots/backdrop_perf.csv";
            Directory.CreateDirectory("Screenshots");
            File.WriteAllText(csv, "nivel,punto,variante,ms,fps,tris,batches,setpass\n");
            int vsync = QualitySettings.vSyncCount;
            int target = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            foreach (var name in Levels)
            {
                Services.Session.ClearPlayers();
                for (int i = 0; i < Crew.Length; i++)
                {
                    var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Crew[i] + ".asset");
                    Services.Session.AddPlayer(new PlayerSlot(i, Color.white, null, null) { IsBot = true, Character = data });
                }
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(FOLDER + name + ".unity",
                    new LoadSceneParameters(LoadSceneMode.Additive));
                yield return null;
                var scene = SceneManager.GetSceneByName(name);
                SceneManager.SetActiveScene(scene);
                var manager = LevelManager.Current;
                foreach (var avatar in manager.Avatars)
                {
                    var brain = avatar.GetComponent<BotBrain>();
                    if (brain != null) brain.enabled = false;
                }
                manager.SkipIntro();
                for (float guard = 0f; manager.State != LevelState.Running && guard < 20f; guard += Time.unscaledDeltaTime)
                    yield return null;

                var backdrops = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(t => t.name == "Backdrops");
                var spline = Object.FindFirstObjectByType<SplineContainer>();
                var body = manager.Raft.Body;
                float yOffset = body.position.y - spline.EvaluatePosition(0f).y;
                for (int k = 0; k < Fractions.Length; k++)
                {
                    float t = Fractions[k];
                    Vector3 p = spline.EvaluatePosition(t);
                    Vector3 ahead = spline.EvaluatePosition(Mathf.Min(1f, t + 0.01f)) - spline.EvaluatePosition(Mathf.Max(0f, t - 0.01f));
                    ahead.y = 0f;
                    manager.Raft.transform.SetPositionAndRotation(new Vector3(p.x, p.y + yOffset, p.z), Quaternion.LookRotation(ahead.normalized));
                    Physics.SyncTransforms();
                    foreach (bool on in new[] { true, false })
                    {
                        if (backdrops != null)
                            backdrops.gameObject.SetActive(on);
                        // Que la cámara llegue a la balsa y se asienten las cargas de shaders.
                        for (float w = 0f; w < 2f; w += Time.unscaledDeltaTime)
                        {
                            body.velocity = Vector3.zero;
                            body.angularVelocity = Vector3.zero;
                            yield return null;
                        }
                        double ms = 0;
                        long tris = 0, batches = 0, setPass = 0;
                        for (int f = 0; f < FRAMES; f++)
                        {
                            body.velocity = Vector3.zero;
                            yield return null;
                            ms += Time.unscaledDeltaTime * 1000.0;
                            tris += UnityEditor.UnityStats.triangles;
                            batches += UnityEditor.UnityStats.batches;
                            setPass += UnityEditor.UnityStats.setPassCalls;
                        }
                        ms /= FRAMES;
                        string line = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F2},{4:F0},{5},{6},{7}\n", name, k,
                            on ? "con" : "sin", ms, 1000.0 / ms, tris / FRAMES, batches / FRAMES, setPass / FRAMES);
                        File.AppendAllText(csv, line);
                        Debug.Log("BDPERF " + line.Trim());
                    }
                    if (backdrops != null)
                        backdrops.gameObject.SetActive(true);
                }

                foreach (var avatar in manager.Avatars)
                    Object.Destroy(avatar.gameObject);
                Object.Destroy(manager.Raft.gameObject);
                yield return SceneManager.UnloadSceneAsync(scene);
                Services.Session.ClearPlayers();
            }
            QualitySettings.vSyncCount = vsync;
            Application.targetFrameRate = target;
            Debug.Log("BDPERF terminado");
#else
            yield break;
#endif
        }
    }
}
