using System.Collections;
using GasSafari.Modes;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-04 Balsa-Gol (docs/06 §1): con 3 bots en la sesión se completa a 2 vs 2, cada equipo en su balsa; los bots
    // meten goles y el minijuego termina (5 goles o el tiempo, aquí 90 s) con puestos y puntos de la Fiesta.
    public class BalsaGolTests
    {
        private PartyTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PartyTestRig();
            yield return rig.SetUp("goal", 3, data => data.durationSeconds = 90f);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => rig.TearDown();

        [UnityTest]
        public IEnumerator Bots_PlayTwoVsTwo_ScoreGoals_AndTheGameRanksEveryone()
        {
            var game = (BalsaGolGame)rig.Game;
            Assert.AreEqual(4, game.Players.Count, "3 jugadores se completan a 2 vs 2 con un bot");
            Assert.AreEqual(2, game.Rafts.Count, "una balsa por equipo");
            Assert.AreEqual(2, game.TeamRaft(0).GetComponent<Raft.RaftSeats>().Crew.Count);
            Assert.AreEqual(2, game.TeamRaft(1).GetComponent<Raft.RaftSeats>().Crew.Count);
            Assert.AreNotEqual(game.TeamOf(0), game.TeamOf(1), "J1 y J2 en equipos distintos");

            yield return rig.PlayToResults(30f, 120f);

            int goals = game.GoalsOf(0) + game.GoalsOf(1);
            UnityEngine.Debug.Log("PARTYTEST BalsaGol goles=" + game.GoalsOf(0) + "-" + game.GoalsOf(1) + " saques neutrales=" + game.NeutralDrops);
            Assert.Greater(goals, 0, "los bots meten al menos un gol en 90 s");
            Assert.IsTrue(game.GoalsOf(0) >= 5 || game.GoalsOf(1) >= 5 || game.Elapsed >= 90f, "termina por 5 goles o por tiempo");
            rig.AssertScored();
            for (int i = 0; i < game.Players.Count; i++)
                Assert.AreEqual(game.GoalsOf(game.TeamOf(i)), game.Scores[i], "cada jugador tiene los goles de su equipo");
            if (game.GoalsOf(0) != game.GoalsOf(1))
            {
                int winner = game.GoalsOf(0) > game.GoalsOf(1) ? 0 : 1;
                for (int i = 0; i < game.Players.Count; i++)
                    Assert.AreEqual(game.TeamOf(i) == winner ? 1 : 3, game.Ranks[i], "ganadores 1.º, perdedores 3.º");
            }
        }
    }
}
