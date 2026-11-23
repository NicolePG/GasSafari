using System.Collections;
using GasSafari.Modes;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-05 Rey de la Balsa (docs/06 §1): 4 bots en una balsa que gira; los pedos tiran al agua a quien está a menos de
    // 2.5 m, el caído sale de la ronda y gana la ronda el último a bordo; termina con 2 rondas ganadas o el tope de rondas.
    public class ReyBalsaTests
    {
        private PartyTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PartyTestRig();
            yield return rig.SetUp("king", 4, data =>
            {
                data.roundSeconds = 30f;
                data.maxRounds = 3;
            });
        }

        [UnityTearDown]
        public IEnumerator TearDown() => rig.TearDown();

        [UnityTest]
        public IEnumerator Bots_PushEachOtherOff_WinRounds_AndTheGameRanksEveryone()
        {
            var game = (ReyBalsaGame)rig.Game;
            Assert.AreEqual(4, game.Players.Count);
            Assert.AreEqual(1, game.Rafts.Count, "todos en una sola balsa");
            Assert.AreEqual(4, game.Raft.GetComponent<RaftSeats>().Seats.Count);
            while (game.State != PartyState.Playing)
                yield return null;
            var seats = game.Raft.GetComponent<RaftSeats>().Seats;
            for (int i = 0; i < seats.Count; i++)
            {
                for (int j = i + 1; j < seats.Count; j++)
                    Assert.Greater(Vector3.Distance(seats[i].transform.position, seats[j].transform.position), rig.Data.pushRadius,
                        "al empezar la ronda nadie está a tiro de otro");
            }
            Assert.IsNotNull(game.Raft.GetComponent<RaftSpinner>(), "la balsa gira");

            yield return new WaitForSeconds(1f);
            Assert.Greater(Mathf.Abs(game.Raft.Body.angularVelocity.y), 0.1f, "la balsa está girando");

            yield return rig.PlayToResults(6f, 120f);

            int rounds = 0;
            foreach (int won in game.Scores)
                rounds += won;
            Assert.Greater(rounds, 0, "los bots tiran a los demás y alguien gana al menos una ronda");
            Debug.Log("PARTYTEST ReyBalsa rondas=" + string.Join(",", game.RoundLengths));
            Assert.GreaterOrEqual(game.RoundLengths[0], 2f, "con los asientos repartidos nadie cae en el primer instante");
            Assert.GreaterOrEqual(game.RoundsPlayed, 1);
            Assert.LessOrEqual(game.RoundsPlayed, 3, "tope provisional de rondas");
            rig.AssertScored();
        }
    }
}
