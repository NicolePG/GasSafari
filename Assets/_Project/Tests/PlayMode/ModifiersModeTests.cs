using System.Collections;
using System.Linq;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.Raft;
using GasSafari.Save;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-03 Modificadores en TEST_LevelTemplate: los 8 efectos se aplican al crear la balsa y se revierten al salir (la
    // gravedad, que es global, siempre); sin estrellas ni récord; Ayuda Real sí (Contrarreloj no); logros de nivel no.
    public class ModifiersModeTests
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";
        private const string LEVEL_ID = "MODS-TEST";

        private GameObject sources;
        private string savePath;
        private LevelData data;
        private Vector3 gravity;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gravity = Physics.gravity;
            GameEvents.ClearAll();
            // ClearAll suelta las suscripciones de los servicios: se vuelven a enganchar.
            Services.Achievements.enabled = false;
            Services.Achievements.enabled = true;
            Services.Session.ClearPlayers();
            savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gasSafariModsPlay_" + System.Guid.NewGuid().ToString("N"), "save.json");
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
                    Character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Characters/" + (i == 0 ? "Melman" : "Julien") + ".asset")
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
            LevelFlow.InGameFlow = false;
            LevelFlow.AssistArmed = false;
            Services.Session.RunMode = ProgressionRules.MODE_STORY;
            Services.Session.Modifiers.Clear();
            Services.Session.ClearPlayers();
            Object.Destroy(sources);
            var replay = Object.FindFirstObjectByType<ReplayPlayer>();
            if (replay != null)
                replay.Stop();
            // Primero la escena (con la cámara que sigue a la balsa) y después la balsa y los animales.
            var raft = LevelManager.Current != null ? LevelManager.Current.Raft : null;
            var avatars = Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("TEST_LevelTemplate"));
            foreach (var avatar in avatars)
            {
                if (avatar != null)
                    Object.Destroy(avatar.gameObject);
            }
            if (raft != null)
                Object.Destroy(raft.gameObject);
            Assert.AreEqual(gravity, Physics.gravity, "al salir del nivel la gravedad vuelve a la normal");
            Physics.gravity = gravity;
            string dir = System.IO.Path.GetDirectoryName(savePath);
            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);
            Object.Destroy(data);
        }

        private void SetData()
        {
            typeof(LevelManager).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(LevelManager.Current, data);
        }

        private ModifiersMode Attach(params string[] modifiers)
        {
            SetData();
            Services.Session.Modifiers.Clear();
            Services.Session.Modifiers.AddRange(modifiers);
            Services.Session.RunMode = ProgressionRules.MODE_MODIFIERS;
            return (ModifiersMode)ModeDirector.Attach(LevelManager.Current, ProgressionRules.MODE_MODIFIERS);
        }

        [UnityTest]
        public IEnumerator AllEight_ApplyOnTheRaft_AndRevertWhenTheModeEnds()
        {
            var all = ModesConfig.Instance.modifiers.Select(m => m.id).ToArray();
            var mode = Attach(all.Concat(new[] { "no_existe" }).ToArray());
            Assert.AreEqual(8, mode.Active.Count, "un id desconocido se ignora");
            var raft = LevelManager.Current.Raft;
            yield return null;

            Assert.AreEqual(0.5f, raft.Buoyancy.ModifierDragMultiplier, "Balsa jabonosa: waterDrag x0.5");
            Assert.AreEqual(gravity * 0.4f, Physics.gravity, "Gravedad lunar x0.4");
            Assert.AreEqual(-1f, raft.GetComponent<RaftEffects>().FartSign, "Pedos al revés");
            Assert.AreEqual(1.5f, raft.GetComponent<RaftFlowReceiver>().CurrentMultiplier, "Turbo corriente x1.5");
            Assert.IsTrue(raft.GetComponent<RaftChambers>().SingleChamber);
            Assert.IsNotNull(mode.Dizzy, "Cámara mareada");
            foreach (var avatar in LevelManager.Current.Avatars)
            {
                Assert.IsTrue(avatar.Gas.Infinite);
                float gas = avatar.Gas.Gas;
                Assert.IsTrue(avatar.Gas.TrySpend(gas + 50f), "Gas infinito: siempre alcanza");
                avatar.Gas.Drain(10f);
                Assert.AreEqual(gas, avatar.Gas.Gas, "y no se gasta");
                var head = avatar.GetComponent<BigHeadScaler>();
                Assert.IsNotNull(head, "Cabezones");
                Assert.IsNotNull(head.Head, avatar.Character.name + ": se encuentra el hueso de la cabeza");
            }
            var firstHead = LevelManager.Current.Avatars[0].GetComponent<BigHeadScaler>().Head;
            yield return null;
            Vector3 big = firstHead.localScale;

            // Cámara única: la primera cámara reventada hunde la balsa.
            var chambers = raft.GetComponent<RaftChambers>();
            Assert.IsTrue(chambers.PopChamber(0));
            Assert.IsTrue(chambers.IsSinking);

            Object.Destroy(mode);
            yield return null;
            yield return null;
            Assert.AreEqual(gravity, Physics.gravity);
            Assert.AreEqual(1f, raft.Buoyancy.ModifierDragMultiplier);
            Assert.AreEqual(1f, raft.GetComponent<RaftEffects>().FartSign);
            Assert.AreEqual(1f, raft.GetComponent<RaftFlowReceiver>().CurrentMultiplier);
            Assert.IsFalse(chambers.SingleChamber);
            Assert.IsFalse(LevelManager.Current.Avatars[0].Gas.Infinite);
            Assert.AreEqual(big.x / 2f, firstHead.localScale.x, 1e-4f, "la cabeza vuelve a su tamaño");
        }

        [UnityTest]
        public IEnumerator ReversedFarts_PushTheRaftBackwards()
        {
            Attach("reverse");
            var raft = LevelManager.Current.Raft;
            var avatar = LevelManager.Current.Avatars[0];
            var delivery = new FartDelivery(avatar.GetComponent<PlayerSeat>(), raft.Balance);
            yield return new WaitForFixedUpdate();
            raft.Body.velocity = Vector3.zero;
            var forward = raft.transform.forward;
            delivery.Push(forward, 4f, ForceMode.VelocityChange);
            yield return new WaitForFixedUpdate();
            Assert.Less(Vector3.Dot(raft.Body.velocity, forward), -1f, "el pedo hacia adelante empuja hacia atrás");
        }

        [UnityTest]
        public IEnumerator Finish_GivesNoStarsOrRecord_AndNoLevelAchievements()
        {
            Services.Progression.Data.GetOrAddLevel(LEVEL_ID).stars = 1;
            Services.Progression.Data.GetOrAddLevel(LEVEL_ID).best = 300f;
            Attach("soapy");
            yield return new WaitForSeconds(3.5f);
            var level = LevelManager.Current;
            Assert.AreEqual(LevelState.Running, level.State);
            level.Raft.Body.position = Object.FindFirstObjectByType<FinishGate>().transform.position;
            yield return new WaitForSeconds(2.6f);
            Assert.AreEqual(LevelState.Results, level.State);

            var results = Object.FindFirstObjectByType<ResultsController>().Results;
            Assert.IsTrue(results.ModifiersActive);
            Assert.AreEqual(3, results.Stars, "se muestran las estrellas que habría ganado");
            Assert.AreEqual(ResultsBuilder.BeansWithoutProgress(results.FullSyncGroups), results.Beans);
            Assert.AreEqual(LocalizedText.Table.Get("mod.warning"), Object.FindFirstObjectByType<ResultsScreen>().ModeNoteText);
            var record = Services.Progression.Data.FindLevel(LEVEL_ID);
            Assert.AreEqual(1, record.stars, "docs/06: los tiempos con modificadores no cuentan para estrellas");
            Assert.AreEqual(300f, record.best);
            Assert.IsNull(GhostStore.Load(LEVEL_ID), "ni para el fantasma");
            Assert.IsNull(Services.Progression.Data.FindTrial(LEVEL_ID), "ni para la tabla del Contrarreloj");
            Assert.IsFalse(Services.Achievements.IsUnlocked("not_a_scratch"), "Sin un Rasguño no cuenta con modificadores");
        }

        [UnityTest]
        public IEnumerator RoyalAssist_IsOfferedInModifiers_ButNotInTimeTrial()
        {
            SetData();
            Services.Session.Modifiers.Add("single");
            Services.Session.RunMode = ProgressionRules.MODE_MODIFIERS;
            LevelFlow.InGameFlow = true;
            LevelFlow.AssistArmed = true;
            LevelFlow.Hook();
            Assert.IsInstanceOf<ModifiersMode>(LevelManager.Current.GetComponent<GameModeBase>());
            var checkpoints = Object.FindFirstObjectByType<CheckpointSystem>();
            Assert.IsNotNull(checkpoints.GetComponent<RoyalAssist>(), "con Cámara única la ayuda llega antes, y se ofrece igual");

            Object.Destroy(checkpoints.GetComponent<RoyalAssist>());
            Object.Destroy(LevelManager.Current.GetComponent<GameModeBase>());
            yield return null;
            Services.Session.RunMode = ProgressionRules.MODE_TIME_TRIAL;
            LevelFlow.AssistArmed = true;
            LevelFlow.Hook();
            Assert.IsInstanceOf<TimeTrialMode>(LevelManager.Current.GetComponent<GameModeBase>());
            Assert.IsNull(checkpoints.GetComponent<RoyalAssist>(), "un tramo saltado no puede ser récord del Contrarreloj");
        }
    }
}
