using System.Collections;
using GasSafari.Modes;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-06 Pesca de Mangos (docs/06 §1): cada bot en su mini balsa individual (radio 1 m, 1 asiento) recoge mangos que
    // reaparecen; al acabar el tiempo (aquí 40 s) gana el que tiene más y se reparten los puntos de la Fiesta.
    public class PescaMangosTests
    {
        private PartyTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PartyTestRig();
            yield return rig.SetUp("fishing", 3, data => data.durationSeconds = 40f);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => rig.TearDown();

        [UnityTest]
        public IEnumerator Bots_CollectMangos_OnMiniRafts_AndTheGameRanksEveryone()
        {
            var game = (PescaMangosGame)rig.Game;
            Assert.AreEqual(3, game.Players.Count, "3 jugadores no necesitan relleno");
            Assert.AreEqual(3, game.Rafts.Count, "una mini balsa por jugador");
            foreach (var raft in game.Rafts)
            {
                Assert.AreEqual(1f, raft.Radius, 1e-4f, "mini balsa de 1 m");
                Assert.AreEqual(1, raft.GetComponent<RaftSeats>().Crew.Count, "un solo asiento ocupado");
                var model = raft.transform.Find(MiniRaft.MODEL_NAME);
                Assert.IsNotNull(model, "modelo MiniRaft_J<n>");
                var bounds = model.GetComponentInChildren<UnityEngine.Renderer>().bounds;
                foreach (var r in model.GetComponentsInChildren<UnityEngine.Renderer>())
                    bounds.Encapsulate(r.bounds);
                float extent = UnityEngine.Mathf.Max(bounds.extents.x, bounds.extents.z);
                Assert.That(extent, Is.InRange(1f, 1.6f), "el modelo cubre el radio de 1 m donde se sienta el animal");
            }
            Assert.AreEqual(rig.Data.mangosAtOnce, game.Mangos.Count);

            yield return rig.PlayToResults(15f, 60f);

            Assert.GreaterOrEqual(game.Collected, 3, "los bots recogen mangos");
            int sum = 0;
            foreach (int s in game.Scores)
                sum += s;
            Assert.AreEqual(game.Collected, sum, "cada mango cuenta para quien lo recoge");
            int active = 0;
            foreach (var mango in game.Mangos)
                active += mango.IsCollected ? 0 : 1;
            Assert.Greater(active, 0, "los mangos recogidos reaparecen");
            rig.AssertScored();
        }
    }
}
