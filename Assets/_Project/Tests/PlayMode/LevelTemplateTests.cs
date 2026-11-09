using System.Collections;
using GasSafari.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // TEST_LevelTemplate (docs/10 §3): arranca solo, la balsa flota en la salida y el nivel llega a Running.
    public class LevelTemplateTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";

        [UnityTest]
        public IEnumerator Template_LoadsAndRuns_RaftFloatsAtStart()
        {
#if UNITY_EDITOR
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TEMPLATE, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return op;
            yield return new WaitForSeconds(3f);

            var manager = LevelManager.Current;
            Assert.IsNotNull(manager, "LevelManager en la plantilla");
            Assert.AreEqual(LevelState.Running, manager.State);
            Assert.IsTrue(manager.Timer.IsRunning);
            Assert.IsNotNull(manager.Raft);
            Assert.Greater(manager.Raft.transform.position.y, -1f, "la balsa flota");
            Assert.IsNotNull(Object.FindFirstObjectByType<StartGate>());
            Assert.IsNotNull(Object.FindFirstObjectByType<FinishGate>());
            Assert.IsNotNull(Object.FindFirstObjectByType<FlowSpline>());
            Assert.IsNotNull(Object.FindFirstObjectByType<CheckpointSystem>());
            Assert.IsNotNull(Camera.main);

            var scene = SceneManager.GetSceneByName("TEST_LevelTemplate");
            yield return SceneManager.UnloadSceneAsync(scene);
            Object.Destroy(manager.Raft.gameObject);
#else
            yield break;
#endif
        }
    }
}
