using System.Collections;
using GasSafari.Modes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-08 Coco Caliente (docs/06 §1): 4 bots en la balsa compartida se pasan el coco-bomba con pedos (va al más cercano
    // de quien lo tira); cada explosión saca a quien lo tiene y hay rondas hasta que queda uno. Mecha corta para la prueba.
    public class CocoCalienteTests
    {
        private PartyTestRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PartyTestRig();
            yield return rig.SetUp("coconut", 4, data =>
            {
                data.fuseMinSeconds = 6f;
                data.fuseMaxSeconds = 9f;
            });
        }

        [UnityTearDown]
        public IEnumerator TearDown() => rig.TearDown();

        [UnityTest]
        public IEnumerator Bots_PassTheCoconut_AndEachBlastKnocksOneOut_UntilOneIsLeft()
        {
            var game = (CocoCalienteGame)rig.Game;
            Assert.AreEqual(4, game.Players.Count);
            Assert.AreEqual(1, game.Rafts.Count, "una balsa compartida");
            Assert.IsNotNull(game.Coconut, "la escena trae el coco-bomba");

            int maxPassesInRound = 0, passesAtRoundStart = 0, lastRound = 0;
            yield return rig.PlayToResults(4f, 90f, () =>
            {
                if (game.Round != lastRound)
                {
                    lastRound = game.Round;
                    passesAtRoundStart = game.Passes;
                }
                maxPassesInRound = Mathf.Max(maxPassesInRound, game.Passes - passesAtRoundStart);
                Debug.Log("COCO t=" + game.Elapsed.ToString("0") + " ronda=" + game.Round + " tiene=" + game.Holder + " mecha=" + game.FuseLeft.ToString("0.0") + " pases=" + game.Passes);
            });

            Assert.AreEqual(3, game.Eliminated.Count, "con 4 jugadores, 3 explosiones");
            Assert.AreEqual(3, game.Round, "una ronda por eliminado");
            Assert.GreaterOrEqual(game.Passes, 6, "los bots se pasan el coco con pedos");
            int survivor = -1;
            for (int i = 0; i < game.Players.Count; i++)
            {
                if (!game.IsOut(i))
                    survivor = i;
            }
            Assert.GreaterOrEqual(survivor, 0, "queda uno");
            Assert.AreEqual(1, game.Ranks[survivor], "el último en pie gana");
            Assert.AreEqual(4, game.Ranks[game.Eliminated[0]], "el primero en volar queda último");
            Assert.AreEqual(3, game.Ranks[game.Eliminated[1]]);
            Assert.AreEqual(2, game.Ranks[game.Eliminated[2]]);
            rig.AssertScored();
        }
    }
}
