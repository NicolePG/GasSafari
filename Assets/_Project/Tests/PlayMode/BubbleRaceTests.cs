using System.Collections;
using GasSafari.Modes;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-07 Carrera de Burbujas (docs/06 §1): 4 bots en mini balsas individuales con gaseosa infinita (flotan a 1 m) dan
    // las vueltas a la pista con obstáculos siguiendo la línea de carrera; el que llega primero gana y se reparten los
    // puntos de la Fiesta. Recogen frijoles de la pista para no quedarse sin gas.
    public class BubbleRaceTests
    {
        private PartyTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PartyTestRig();
            yield return rig.SetUp("bubble", 4, null);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => rig.TearDown();

        [Test]
        public void Track_ProjectsAndRebuildsPoints()
        {
            var track = new StadiumTrack(Vector3.zero, 10f, 9f, 3.5f);
            Assert.AreEqual(4f * 10f + 2f * Mathf.PI * 9f, track.Perimeter, 1e-3f);
            foreach (float s in new[] { 3f, 25f, 40f, 55f, 70f, 90f })
            {
                foreach (float lat in new[] { -2f, 0f, 2.5f })
                {
                    track.Project(track.Point(s, lat), out float ps, out float plat);
                    Assert.AreEqual(s, ps, 1e-3f, "s en " + s + "/" + lat);
                    Assert.AreEqual(lat, plat, 1e-3f, "lat en " + s + "/" + lat);
                }
            }
            Assert.AreEqual(-2f, track.Delta(1f, track.Perimeter - 1f), 1e-3f, "cruzar el 0 hacia atrás");
        }

        [UnityTest]
        public IEnumerator Bots_RaceTheLaps_OnHoveringMiniRafts_AndFinishInOrder()
        {
            var game = (BubbleRaceGame)rig.Game;
            Assert.AreEqual(4, game.Players.Count);
            Assert.AreEqual(4, game.Rafts.Count, "una mini balsa por jugador");
            foreach (var raft in game.Rafts)
            {
                Assert.AreEqual(1f, raft.Radius, 1e-4f, "mini balsa de 1 m");
                Assert.AreEqual(1, raft.GetComponent<RaftSeats>().Crew.Count);
            }

            float maxHover = 0f;
            yield return rig.PlayToResults(12f, 120f, () =>
            {
                string line = "BUBBLE t=" + game.Elapsed.ToString("0");
                for (int i = 0; i < game.Rafts.Count; i++)
                {
                    maxHover = Mathf.Max(maxHover, game.Rafts[i].Buoyancy.HoverOffset);
                    line += " J" + (game.Players[i].Index + 1) + "=" + game.Distance(i).ToString("0") + "m";
                }
                Debug.Log(line + " rescates=" + game.Rescues + " frijoles=" + game.BeansTaken);
            });

            Assert.AreEqual(rig.Data.bubbleHover, maxHover, 0.05f, "la gaseosa infinita las hace flotar 1 m");
            Assert.GreaterOrEqual(game.Finishers, 1, "al menos un bot termina las vueltas");
            int winner = System.Array.IndexOf(game.Ranks, 1);
            Assert.IsTrue(game.HasFinished(winner), "gana el primero en llegar");
            Assert.GreaterOrEqual(game.Distance(winner), game.RaceLength, "el ganador recorrió las 3 vueltas");
            Assert.Greater(game.BeansTaken, 0, "los bots recogen frijoles de la pista");
            for (int i = 0; i < game.Players.Count; i++)
            {
                if (i == winner)
                    continue;
                if (game.HasFinished(i))
                    Assert.Greater(game.FinishTime(i), game.FinishTime(winner));
            }
            Assert.LessOrEqual(game.Rescues, 6, "los bots casi no se traban en la pista");
            rig.AssertScored();
        }
    }
}
