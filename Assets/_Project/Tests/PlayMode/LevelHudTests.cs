using System.Collections;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Level;
using GasSafari.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // HUD de docs/07 §3 dentro de _LevelTemplate con 2 jugadores.
    public class LevelHudTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Levels/_LevelTemplate.unity";
        private const string DATA = "Assets/_Project/Data/Characters/";

        private GameObject sources;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameEvents.ClearAll();
            Services.Session.ClearPlayers();
            sources = new GameObject("Sources");
#if UNITY_EDITOR
            string[] characters = { "Alex", "Marty" };
            for (int i = 0; i < characters.Length; i++)
            {
                var source = sources.AddComponent<ScriptedCommandSource>();
                var slot = new PlayerSlot(i, Color.HSVToRGB(i * 0.5f, 0.8f, 1f), null, source)
                {
                    Character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(DATA + characters[i] + ".asset")
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
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
            if (LevelManager.Current != null)
                Object.Destroy(LevelManager.Current.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("_LevelTemplate"));
        }

        [UnityTest]
        public IEnumerator Hud_BuildsPanelsAndOverlaysPerPlayer()
        {
            yield return null;
            Assert.AreEqual(2, Object.FindObjectsByType<HudPlayerPanel>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2, Object.FindObjectsByType<AvatarOverlay>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator Countdown_ShowsNumbersThenGo_ThenTimerRuns()
        {
            var hud = Object.FindFirstObjectByType<LevelHud>();
            var seen = new System.Collections.Generic.List<string>();
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                if (hud.EventText.IsShowing && (seen.Count == 0 || seen[seen.Count - 1] != hud.EventText.Current))
                    seen.Add(hud.EventText.Current);
                yield return null;
            }
            CollectionAssert.Contains(seen, "¡YA!");
            Assert.AreEqual(LevelState.Running, LevelManager.Current.State);

            var timerText = hud.GetComponentInChildren<HudTimer>().GetComponentInChildren<TMP_Text>();
            Assert.AreEqual("0:01", timerText.text.Trim(), "tras ~1 s de carrera");
        }

        [UnityTest]
        public IEnumerator Panels_ReflectGasAndColors()
        {
            yield return new WaitForSeconds(0.3f);
            var avatar = LevelManager.Current.Avatars[0];
            avatar.Gas.SetFraction(0.25f);
            yield return null;

            HudPlayerPanel panel = null;
            foreach (var candidate in Object.FindObjectsByType<HudPlayerPanel>(FindObjectsSortMode.None))
            {
                if (candidate.Avatar == avatar)
                    panel = candidate;
            }
            Assert.IsNotNull(panel);
            var fills = panel.GetComponentsInChildren<UnityEngine.UI.Image>();
            bool foundGas = false;
            foreach (var image in fills)
            {
                if (image.name == "GasFill" && Mathf.Abs(image.fillAmount - 0.25f) < 0.05f)
                    foundGas = true;
            }
            Assert.IsTrue(foundGas, "la barra de gas sigue al tanque");
            Assert.AreEqual(avatar.Character.uiColor, panel.transform.Find("Portrait").GetComponent<UnityEngine.UI.Image>().color);
        }

        [UnityTest]
        public IEnumerator Events_ShowSyncAndCheckpointTexts()
        {
            var hud = Object.FindFirstObjectByType<LevelHud>();
            yield return new WaitForSeconds(2.6f);

            GameEvents.RaiseSync(3, 1.6f);
            Assert.AreEqual("¡SYNC x3!", hud.EventText.Current);
            yield return new WaitForSeconds(1.1f);
            GameEvents.RaiseCheckpoint(1);
            Assert.AreEqual("CHECKPOINT", hud.EventText.Current);
        }

        [UnityTest]
        public IEnumerator Meter_ShowsSuperFartReadyWhenFull()
        {
            yield return new WaitForSeconds(0.3f);
            var meter = LevelManager.Current.Raft.GetComponent<Team.TeamMeter>();
            var hud = Object.FindFirstObjectByType<HudTimer>();
            meter.Set(meter.Max);
            yield return null;

            var ready = hud.transform.Find("SuperReady");
            Assert.IsTrue(ready.gameObject.activeSelf);
            Assert.AreEqual("¡SÚPER PEDO LISTO!", ready.GetComponent<TMP_Text>().text);
            meter.Consume();
            yield return null;
            Assert.IsFalse(ready.gameObject.activeSelf);
        }
    }
}
