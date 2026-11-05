using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class LevelManagerTests
    {
        private const string RAFT_PREFAB = "Assets/_Project/Prefabs/Raft/Raft.prefab";
        private const string AVATAR_PREFAB = "Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab";
        private const string BALANCE_PATH = "Assets/_Project/Data/Balance/BalanceConfig.asset";

        private readonly List<GameObject> created = new List<GameObject>();
        private LevelManager manager;
        private FinishGate finish;
        private List<ScriptedCommandSource> sources;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            var water = Track(GameObject.CreatePrimitive(PrimitiveType.Plane));
            water.layer = GameLayers.Water;
            water.transform.localScale = new Vector3(20f, 1f, 20f);

            var start = Track(new GameObject("Start")).AddComponent<StartGate>();
            start.transform.position = new Vector3(0f, 0.2f, -10f);

            var finishObject = Track(new GameObject("Finish"));
            finishObject.transform.position = new Vector3(0f, 0f, 10f);
            var box = finishObject.AddComponent<BoxCollider>();
            box.size = new Vector3(20f, 10f, 2f);
            box.isTrigger = true;
            finish = finishObject.AddComponent<FinishGate>();

            var spawner = Track(new GameObject("Spawner")).AddComponent<RaftSpawner>();
            spawner.Configure(Load<GameObject>(RAFT_PREFAB), Load<GameObject>(AVATAR_PREFAB), start);

            var managerObject = Track(new GameObject("LevelManager"));
            manager = managerObject.AddComponent<LevelManager>();
            manager.Configure(null, Load<BalanceConfig>(BALANCE_PATH), spawner, finish, null);

            sources = new List<ScriptedCommandSource>();
            var slots = new List<PlayerSlot>();
            for (int i = 0; i < 2; i++)
            {
                var source = Track(new GameObject("Source" + i)).AddComponent<ScriptedCommandSource>();
                sources.Add(source);
                slots.Add(new PlayerSlot(i, Color.white, null, source));
            }
            manager.Begin(slots);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                Object.Destroy(go);
            if (manager != null && manager.Raft != null)
                Object.Destroy(manager.Raft.gameObject);
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
                Object.Destroy(avatar.gameObject);
        }

        [UnityTest]
        public IEnumerator Begin_SpawnsRaftAndCrew_InIntroWithInputLocked()
        {
            yield return null;
            Assert.IsNotNull(manager.Raft);
            Assert.AreEqual(2, manager.Avatars.Count);
            Assert.AreEqual(-10f, manager.Raft.transform.position.z, 1f);
            Assert.IsFalse(manager.Timer.IsRunning);
            foreach (var avatar in manager.Avatars)
                Assert.IsTrue(avatar.InputLocked);
        }

        [UnityTest]
        public IEnumerator Countdown_TicksDownToGo_ThenRunsAndUnlocksInput()
        {
            var ticks = new List<int>();
            manager.CountdownTick += ticks.Add;
            var states = new List<LevelState>();
            manager.StateChanged += states.Add;

            sources[0].Command.FartPresses++;
            yield return null;
            Assert.AreEqual(LevelState.Countdown, manager.State);
            Assert.IsFalse(manager.Timer.IsRunning);
            Assert.AreEqual(default(byte), manager.Avatars[0].Command.FartPresses, "sin comandos antes de la salida");

            float step = manager.Raft.Balance.pingCountdownStep;
            yield return new WaitForSeconds(step * 4f + 0.3f);

            // El 3 puede salir antes de que la prueba se suscriba.
            ticks.RemoveAll(t => t == 3);
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, ticks);
            Assert.AreEqual(LevelState.Running, manager.State);
            Assert.IsTrue(manager.Timer.IsRunning);
            Assert.IsFalse(manager.Avatars[0].InputLocked);
            Assert.AreEqual(LevelState.Running, states[states.Count - 1]);
        }

        [UnityTest]
        public IEnumerator Finish_StopsTimer_ThenShowsResults()
        {
            float finishedTime = -1f;
            manager.Finished += time => finishedTime = time;
            float step = manager.Raft.Balance.pingCountdownStep;
            yield return new WaitForSeconds(step * 4f + 0.3f);
            Assert.AreEqual(LevelState.Running, manager.State);

            manager.Raft.Body.position = finish.transform.position;
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(LevelState.Finished, manager.State);
            Assert.IsFalse(manager.Timer.IsRunning);
            Assert.AreEqual(manager.Timer.Total, finishedTime, 1e-4f);
            Assert.IsTrue(manager.Avatars[0].InputLocked);

            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(LevelState.Results, manager.State);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesCountdown()
        {
            yield return null;
            manager.SetPaused(true);
            Assert.IsTrue(manager.IsPaused);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(LevelState.Countdown, manager.State);
            Assert.AreEqual(3, manager.CountdownValue);

            manager.SetPaused(false);
            yield return new WaitForSeconds(manager.Raft.Balance.pingCountdownStep * 4f + 0.3f);
            Assert.AreEqual(LevelState.Running, manager.State);
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }

        private static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }
    }
}
