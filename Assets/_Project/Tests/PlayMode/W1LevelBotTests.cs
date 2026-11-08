using System.Collections;
using GasSafari.Bots;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-03 a M5-07 y M4-04: una tripulación de 4 bots termina cada nivel del Mundo 1. El tiempo se compara con Plata x1.3
    // (QA-10 pide 1 humano + 3 bots; aquí son 4 bots porque no hay un humano que simular).
    public class W1LevelBotTests
    {
        private static readonly string[] Crew = { "Alex", "Marty", "Melman", "Gloria" };

        [UnityTest, Timeout(600000)]
        public IEnumerator W1_1_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return RunLevel("L_W1_1_FuentesZoo", "W1-1", 255f * 1.3f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W1_2_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return RunLevel("L_W1_2_Alcantarillas", "W1-2", 275f * 1.3f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W1_3_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return RunLevel("L_W1_3_RioCiudad", "W1-3", 295f * 1.3f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W1_4_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return RunLevel("L_W1_4_MercadoFlotante", "W1-4", 315f * 1.3f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator W1_5_FourBots_FinishUnderSilverTimes1Point3()
        {
            yield return RunLevel("L_W1_5_PuertoJefe", "W1-5", 340f * 1.3f);
        }

        private IEnumerator RunLevel(string scene, string id, float limit)
        {
#if UNITY_EDITOR
            Services.Session.ClearPlayers();
            for (int i = 0; i < Crew.Length; i++)
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + Crew[i] + ".asset");
                Services.Session.AddPlayer(new PlayerSlot(i, Color.white, null, null) { IsBot = true, Character = data });
            }

            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                $"Assets/_Project/Scenes/Levels/{scene}.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            yield return op;
            yield return null;

            var manager = LevelManager.Current;
            var telemetry = manager.Raft.gameObject.AddComponent<RaftTelemetry>();
            float finishedAt = -1f;
            manager.Finished += time => finishedAt = time;
            int falls = 0;
            System.Action<PlayerAvatar> onFall = a => falls++;
            GameEvents.OnPlayerFell += onFall;
            int respawns = 0;
            System.Action onRespawn = () => respawns++;
            GameEvents.OnRespawn += onRespawn;

            System.Action sunk = () => Debug.Log($"W1BOT {id} HUNDIDA en ({manager.Raft.transform.position.x:F0},{manager.Raft.transform.position.z:F0})");
            System.Action capsized = () => Debug.Log($"W1BOT {id} VUELCO en ({manager.Raft.transform.position.x:F0},{manager.Raft.transform.position.z:F0})");
            System.Action<int> popped = c => Debug.Log($"W1BOT {id} CAMARA {c} reventada en ({manager.Raft.transform.position.x:F0},{manager.Raft.transform.position.z:F0})");
            GameEvents.OnRaftSunk += sunk;
            GameEvents.OnRaftCapsized += capsized;
            GameEvents.OnChamberPopped += popped;

            // Tiempo de juego x3: la física por paso no cambia y el límite se mide en tiempo de juego.
            Time.timeScale = 3f;
            float elapsed = 0f;
            while (finishedAt < 0f && elapsed < limit)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                if (((int)elapsed) % 20 == 0)
                {
                    var p = manager.Raft.transform.position;
                    Debug.Log($"W1BOT {id} t={elapsed:F0}s raft=({p.x:F0},{p.z:F0}) v={manager.Raft.Body.velocity.magnitude:F1} caídas={falls} reapariciones={respawns}");
                }
            }
            Time.timeScale = 1f;
            GameEvents.OnPlayerFell -= onFall;
            GameEvents.OnRespawn -= onRespawn;
            GameEvents.OnRaftSunk -= sunk;
            GameEvents.OnRaftCapsized -= capsized;
            GameEvents.OnChamberPopped -= popped;

            string csv = System.IO.Path.Combine(RaftHighSpeedTests.TelemetryFolder, $"bots_{id}.csv");
            telemetry.WriteCsv(csv);
            Debug.Log($"W1BOT {id} telemetría vMax={telemetry.MaxSpeed:F1} inclMax={telemetry.MaxTilt:F0} csv={csv}");
            foreach (var avatar in manager.Avatars)
            {
                var brain = avatar.GetComponent<BotBrain>();
                if (brain != null)
                    Debug.Log($"W1BOT {id} bot {avatar.Character.name} {brain.FireStats}");
            }
            Debug.Log($"W1BOT {id} terminó={finishedAt >= 0f} tiempo={finishedAt:F1}s espera={elapsed:F0}s caídas={falls} reapariciones={respawns}");
            Assert.GreaterOrEqual(finishedAt, 0f, $"{id}: los bots llegan a la meta en menos de {limit:F0} s");
            var star = Object.FindFirstObjectByType<StarMoment>();
            if (star != null)
                Assert.IsTrue(star.Played, $"{id}: el momento estrella se disparó");
            var finale = Object.FindFirstObjectByType<CraneFinale>();
            if (finale != null)
                Assert.IsTrue(finale.Played, $"{id}: el gancho cae tras la balsa en la rampa");

            foreach (var avatar in manager.Avatars)
                Object.Destroy(avatar.gameObject);
            Object.Destroy(manager.Raft.gameObject);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(scene));
            Services.Session.ClearPlayers();
#else
            yield break;
#endif
        }
    }
}
