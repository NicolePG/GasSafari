using System.Collections;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M4-04 (parcial): una tripulación de bots termina la plantilla de nivel. QA-10 (1 jugador + 3 bots en W1-1,
    // menos de Plata x1.3) espera a W1-1 (M5-03).
    public class BotLevelTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Levels/_LevelTemplate.unity";
        private const float TIME_LIMIT = 150f;

        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };

        [UnityTest]
        public IEnumerator FourBots_FinishTheTemplateLevel()
        {
#if UNITY_EDITOR
            Services.Session.ClearPlayers();
            for (int i = 0; i < Crew.Length; i++)
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Crew[i] + ".asset");
                Services.Session.AddPlayer(new PlayerSlot(i, Color.white, null, null) { IsBot = true, Character = data });
            }

            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TEMPLATE, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return op;
            yield return null;

            var manager = LevelManager.Current;
            float finishedAt = -1f;
            manager.Finished += time => finishedAt = time;
            Assert.AreEqual(Crew.Length, manager.Avatars.Count);
            foreach (var avatar in manager.Avatars)
                Assert.IsNotNull(avatar.GetComponent<BotBrain>(), "RaftSpawner adjunta el BotBrain a los slots de bot");

            float elapsed = 0f;
            while (finishedAt < 0f && elapsed < TIME_LIMIT)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            Debug.Log($"BOTLEVEL terminó={finishedAt >= 0f} tiempo={finishedAt:F1}s espera={elapsed:F0}s");
            Assert.GreaterOrEqual(finishedAt, 0f, $"los bots llegan a la meta en menos de {TIME_LIMIT} s");

            foreach (var avatar in manager.Avatars)
                Object.Destroy(avatar.gameObject);
            Object.Destroy(manager.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("_LevelTemplate"));
            Services.Session.ClearPlayers();
#else
            yield break;
#endif
        }
    }
}
