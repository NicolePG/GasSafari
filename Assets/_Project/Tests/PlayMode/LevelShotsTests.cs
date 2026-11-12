using System.Collections;
using System.Collections.Generic;
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
    // Capturas de cada nivel desde la cámara del juego (revisión visual, no se ejecuta con la suite: [Explicit]).
    // Lee Screenshots/shots_request.txt (fracciones y filtro de niveles, ver abajo) y guarda Screenshots/Niveles/<escena>_<k>.png.
    // Línea 1: filtro de escenas separado por comas (vacío = todas). Línea 2: fracciones del recorrido (0.02,0.4,0.7,0.97).
    public class LevelShotsTests
    {
        private const string FOLDER = "Assets/_Project/Scenes/Juego/Niveles";
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };

        [UnityTest, Explicit("Capturas de revisión visual"), Timeout(3600000)]
        public IEnumerator CaptureLevels()
        {
#if UNITY_EDITOR
            string request = "Screenshots/shots_request.txt";
            string[] lines = File.Exists(request) ? File.ReadAllLines(request) : new string[0];
            string[] filter = lines.Length > 0 ? lines[0].Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries) : new string[0];
            float[] fractions = lines.Length > 1
                ? lines[1].Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray()
                : new[] { 0.02f, 0.4f, 0.7f, 0.97f };
            bool moving = lines.Length > 3 && lines[3].Trim() == "move";
            bool pano = lines.Length > 3 && lines[3].Trim() == "pano";
            string outFolder = lines.Length > 2 && lines[2].Trim().Length > 0 ? lines[2].Trim() : "Screenshots/Niveles";
            Directory.CreateDirectory(outFolder);

            var scenes = UnityEditor.AssetDatabase.FindAssets("t:Scene", new[] { FOLDER })
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
            foreach (var path in scenes)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (filter.Length > 0 && !filter.Any(f => name.Contains(f.Trim())))
                    continue;

                Services.Session.ClearPlayers();
                for (int i = 0; i < Crew.Length; i++)
                {
                    var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Crew[i] + ".asset");
                    Services.Session.AddPlayer(new PlayerSlot(i, Color.white, null, null) { IsBot = true, Character = data });
                }
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                yield return null;

                // Como en el juego (carga simple): el cielo, la niebla y la luz ambiente son los de la escena del nivel.
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(name));
                var manager = LevelManager.Current;
                foreach (var avatar in manager.Avatars)
                {
                    var brain = avatar.GetComponent<BotBrain>();
                    if (brain != null) brain.enabled = false;
                }
                manager.SkipIntro();
                float guard = 0f;
                while (manager.State != LevelState.Running && guard < 20f)
                {
                    guard += Time.unscaledDeltaTime;
                    yield return null;
                }
                var spline = Object.FindFirstObjectByType<SplineContainer>();
                var body = manager.Raft.Body;
                float yOffset = body.position.y - spline.EvaluatePosition(0f).y;
                for (int k = 0; k < fractions.Length; k++)
                {
                    float t = Mathf.Clamp01(fractions[k]);
                    Vector3 p = spline.EvaluatePosition(t);
                    Vector3 ahead = spline.EvaluatePosition(Mathf.Min(1f, t + 0.01f)) - spline.EvaluatePosition(Mathf.Max(0f, t - 0.01f));
                    ahead.y = 0f;
                    manager.Raft.transform.SetPositionAndRotation(new Vector3(p.x, p.y + yOffset, p.z), Quaternion.LookRotation(ahead.normalized));
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    Physics.SyncTransforms();
                    for (float waited = 0f; waited < 2.5f; waited += Time.unscaledDeltaTime)
                    {
                        body.velocity = moving ? manager.Raft.transform.forward * 6f : Vector3.zero;
                        yield return null;
                    }
                    yield return new WaitForEndOfFrame();
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(outFolder, $"{name}_{k}.png"), texture.EncodeToPNG());
                    Object.Destroy(texture);
                    if (pano)
                        PanoramaShots(Path.Combine(outFolder, $"{name}_{k}"), manager.Raft.transform.position);
                }

                foreach (var avatar in manager.Avatars)
                    Object.Destroy(avatar.gameObject);
                Object.Destroy(manager.Raft.gameObject);
                yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(name));
                Services.Session.ClearPlayers();
            }
            Debug.Log("SHOTS terminado");
#else
            yield break;
#endif
        }

#if UNITY_EDITOR
        // Cuatro vistas al nivel del horizonte (cada 90°) desde la balsa, con la cámara del juego como base de lente y niebla.
        private static void PanoramaShots(string prefix, Vector3 position)
        {
            var main = Camera.main;
            var go = new GameObject("PanoCamera");
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(main);
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.3f;
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.requiresDepthTexture = true;
            data.requiresColorTexture = true;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            for (int yaw = 0; yaw < 360; yaw += 90)
            {
                go.transform.SetPositionAndRotation(position + Vector3.up * 3f, Quaternion.Euler(10f, yaw, 0f));
                camera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"{prefix}_pano{yaw}.png", tex.EncodeToPNG());
                Object.Destroy(tex);
            }
            camera.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(go);
        }
#endif
    }
}
