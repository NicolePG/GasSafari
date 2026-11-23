using System.Collections;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Modes;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Fiesta completa con 4 bots (docs/06 §1, M7-04..08): los 5 minijuegos en orden, cada uno a sus resultados con el
    // botón "Siguiente", los puntos se acumulan, suena mus_party, después del último vuelve al menú con el ranking final,
    // cuenta para "Fiestero" (los 5 minijuegos) y da frijolitos. Minijuegos más cortos que los de verdad.
    public class PartyFullTests
    {
        private PartyTestRig rig;
        private readonly List<string> loaded = new List<string>();
        private int menuLoads;

        [SetUp]
        public void SetUp()
        {
            rig = new PartyTestRig();
            rig.Prepare(4);
            loaded.Clear();
            menuLoads = 0;
            PartyLauncher.Loader = scene => loaded.Add(scene);
            PartyLauncher.MenuLoader = () => menuLoads++;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            typeof(PartyLauncher).GetMethod("ResetStatics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Services.Session.RunMode = Progression.ProgressionRules.MODE_STORY;
            yield return rig.TearDown();
        }

        private static void Shorten(PartyGameData data)
        {
            switch (data.id)
            {
                case "goal": data.durationSeconds = 45f; break;
                case "king": data.roundSeconds = 20f; data.maxRounds = 3; break;
                case "fishing": data.durationSeconds = 25f; break;
                case "bubble": data.laps = 1; data.finishGraceSeconds = 15f; break;
                case "coconut": data.fuseMinSeconds = 4f; data.fuseMaxSeconds = 6f; break;
            }
        }

        [UnityTest]
        public IEnumerator FullParty_WithBots_PlaysTheFiveInOrder_AddsUpPoints_AndEndsInTheFinalRanking()
        {
            var order = PartyCup.Order();
            CollectionAssert.AreEqual(new[] { "goal", "king", "fishing", "bubble", "coconut" }, order.ConvertAll(g => g.id), "orden de docs/06");
            Assert.IsTrue(PartyLauncher.StartTour());
            int beansBefore = Services.Progression.Beans;
            int pointsSoFar = 0;

            for (int k = 0; k < order.Count; k++)
            {
                Assert.AreEqual(k + 1, loaded.Count, "se cargó un minijuego por vez");
                Assert.AreEqual(order[k].sceneName, loaded[k], "minijuego " + (k + 1) + " en orden");
                Assert.AreEqual(order[k], PartyCup.Current);
                Assert.AreEqual(k, PartyCup.TourIndex);

                yield return rig.Load(order[k].id, Shorten);
                if (Services.Music != null && Services.Music.IsPlaying)
                    Assert.AreEqual("mus_party", Services.Music.CurrentId, "suena mus_party");
                yield return rig.PlayToResults(-1f, 120f);

                Assert.AreEqual(k + 1, PartyCup.GamesPlayed);
                foreach (int rank in rig.Game.Ranks)
                    pointsSoFar += PartyCup.PointsForRank(rank);
                int total = 0;
                foreach (var entry in PartyCup.Standings())
                    total += entry.points;
                Assert.AreEqual(pointsSoFar, total, "los puntos se acumulan de minijuego en minijuego");

                var hud = rig.Game.Hud;
                Assert.IsTrue(hud.ResultsPanel.activeSelf);
                Assert.IsFalse(hud.RetryButton.gameObject.activeSelf, "en la Fiesta completa no hay Repetir");
                string label = hud.OtherButton.GetComponentInChildren<LocalizedText>().Label.text;
                Debug.Log("PARTYFULL " + order[k].id + " boton=" + label + " puntos=" + total);
                hud.OtherButton.onClick.Invoke();
                yield return rig.Unload();
            }

            Assert.AreEqual(5, loaded.Count, "no carga un sexto minijuego");
            Assert.AreEqual(1, menuLoads, "después del último vuelve a la pantalla de minijuegos");
            Assert.IsTrue(PartyCup.ShowFinalOnReturn, "que abre el ranking final");
            Assert.AreEqual(5, Services.Progression.Data.partyPlayed.Count);
            Assert.IsTrue(Services.Achievements.IsUnlocked("party_animal"), "logro de jugar los 5 minijuegos");

            var standings = PartyCup.Standings();
            Assert.AreEqual(4, standings.Count, "los 4 jugaron todo");
            Assert.AreEqual(1, standings[0].rank);
            int beans = PartyCup.AwardBeans();
            Assert.That(beans, Is.InRange(5, 25), "Fiesta: 5 a 25 frijolitos");
            Assert.AreEqual(PartyCup.BeansFor(standings[0].rank, 5), beans, "sin humanos cuenta el mejor puesto");
            Assert.AreEqual(beansBefore + beans, Services.Progression.Beans);
        }
    }
}
