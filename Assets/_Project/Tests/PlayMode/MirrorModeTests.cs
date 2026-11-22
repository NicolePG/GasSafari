using System.Collections;
using System.Linq;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-02 Espejo en TEST_LevelTemplate: imagen invertida bajo el HUD, textos del mundo al derecho, pings con el lado de la
    // pantalla, récords propios con frijolitos x1.5, y todo vuelve a la normalidad al salir.
    public class MirrorModeTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";
        private const string LEVEL_ID = "MIRROR-TEST";

        private GameObject sources;
        private string savePath;
        private LevelData data;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariMirror_" + System.Guid.NewGuid().ToString("N"), "save.json");
            Services.Progression.Initialize(new SaveSystem(savePath));
            data = ScriptableObject.CreateInstance<LevelData>();
            data.id = LEVEL_ID;
            data.goldTime = 100f;
            data.silverTime = 140f;
            sources = new GameObject("Sources");
#if UNITY_EDITOR
            for (int i = 0; i < 2; i++)
            {
                var slot = new PlayerSlot(i, i == 0 ? Color.red : Color.blue, null, sources.AddComponent<ScriptedCommandSource>())
                {
                    Character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/Marty.asset")
                };
                Services.Session.AddPlayer(slot);
            }
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TEMPLATE, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield break;
#endif
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Services.Session.ClearPlayers();
            Object.Destroy(sources);
            var replay = Object.FindFirstObjectByType<ReplayPlayer>();
            if (replay != null)
                replay.Stop();
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            if (LevelManager.Current != null)
                Object.Destroy(LevelManager.Current.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("TEST_LevelTemplate"));
            Assert.IsFalse(MirrorView.Active, "al salir del nivel el Espejo se apaga");
            string dir = System.IO.Path.GetDirectoryName(savePath);
            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);
            Object.Destroy(data);
        }

        private MirrorMode Attach()
        {
            var level = LevelManager.Current;
            typeof(LevelManager).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(level, data);
            return (MirrorMode)ModeDirector.Attach(level, ProgressionRules.MODE_MIRROR);
        }

        [UnityTest]
        public IEnumerator View_IsFlippedUnderTheHud_AndWorldTextsReadRight()
        {
            var overlays = Object.FindObjectsByType<AvatarOverlay>(FindObjectsSortMode.None);
            Assert.Greater(overlays.Length, 0);
            float before = overlays[0].GetComponentInParent<Canvas>().rootCanvas.transform.localScale.x;
            var mode = Attach();
            yield return null;
            Assert.IsTrue(MirrorView.Active);
            var cam = Camera.main;
            Assert.IsNotNull(cam.targetTexture, "la cámara del nivel dibuja en la textura del Espejo");
            Assert.AreSame(mode.Screen.Texture, cam.targetTexture);
            Assert.AreEqual(Screen.width, cam.targetTexture.width);
            Assert.AreEqual(new Rect(1f, 0f, -1f, 1f), mode.Screen.Image.uvRect, "se muestra invertida en X");
            var screenCanvas = mode.Screen.Image.canvas;
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, screenCanvas.renderMode);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c != screenCanvas))
                Assert.Greater(canvas.sortingOrder, screenCanvas.sortingOrder, canvas.name + " (HUD) queda encima, sin invertir");

            Assert.Greater(mode.FlippedTexts, 0);
            float after = overlays[0].GetComponentInParent<Canvas>().rootCanvas.transform.localScale.x;
            Assert.AreEqual(-before, after, 1e-4f, "los anillos y pings sobre los animales se voltean para leerse al derecho");

            // Un bot que pide "gira a la izquierda" (del mundo) se lee "a la derecha" en la pantalla invertida.
            var avatar = LevelManager.Current.Avatars[0];
            GameEvents.RaisePing(avatar, PingType.TurnLeft);
            var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToArray();
            CollectionAssert.Contains(texts, LocalizedText.Table.Get("ping.right"));

            Object.Destroy(mode);
            yield return null;
            yield return null;
            Assert.IsFalse(MirrorView.Active);
            Assert.IsNull(cam.targetTexture, "sin el modo la cámara vuelve a la pantalla");
        }

        [UnityTest]
        public IEnumerator Finish_SavesMirrorRecords_WithBeansTimesOnePointFive()
        {
            Services.Progression.Data.GetOrAddLevel(LEVEL_ID).stars = 3;
            Services.Progression.Data.GetOrAddLevel(LEVEL_ID).best = 50f;
            Attach();
            yield return new WaitForSeconds(3.5f);
            var level = LevelManager.Current;
            Assert.AreEqual(LevelState.Running, level.State);
            level.Raft.Body.position = Object.FindFirstObjectByType<FinishGate>().transform.position;
            yield return new WaitForSeconds(2.6f);
            Assert.AreEqual(LevelState.Results, level.State);

            var results = Object.FindFirstObjectByType<ResultsController>().Results;
            Assert.AreEqual(ProgressionRules.MODE_MIRROR, results.RunMode);
            Assert.IsTrue(results.FirstTime, "en Espejo el nivel nunca se terminó: récords propios");
            int expected = Mathf.RoundToInt(ResultsBuilder.BeansFor(results.Stars, -1, results.FullSyncGroups) * 1.5f);
            Assert.AreEqual(expected, results.Beans, "docs/06: frijolitos x1.5");
            Assert.AreEqual(LocalizedText.Table.Get("mirror.bonus"), Object.FindFirstObjectByType<ResultsScreen>().ModeNoteText);

            var save = Services.Progression.Data;
            Assert.AreEqual(results.Stars, save.FindMirror(LEVEL_ID).stars);
            Assert.AreEqual(results.Time, save.FindMirror(LEVEL_ID).best, 1e-3f);
            Assert.AreEqual(3, save.FindLevel(LEVEL_ID).stars, "las estrellas de Historia no cambian");
            Assert.AreEqual(50f, save.FindLevel(LEVEL_ID).best);
            Assert.AreEqual(3, ProgressionRules.TotalStars(save), "las del Espejo no suman a las 93");
            Assert.AreEqual(expected, save.frijolitos);
            Assert.IsNull(GhostStore.Load(LEVEL_ID), "el Espejo no graba fantasma");

            Services.Achievements.HandleLevelFinished(data, 0, "AmistadesRotas", ProgressionRules.MODE_MIRROR);
            Assert.IsFalse(save.brokenLevels.Contains(LEVEL_ID), "Amistades Rotas pide la historia, no el Espejo");
        }
    }
}
