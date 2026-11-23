using System.Collections;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Fiesta desde los menús (docs/06 §1, M6-02 + M7-04..06): la sala lleva a la pantalla de minijuegos con los puntos en
    // cero; los 3 minijuegos hechos cargan su escena y los otros 2 dicen "Próximamente"; "Terminar la Fiesta" muestra el
    // ranking final y vuelve al menú principal.
    public class PartyMenuTests : MenuTestBase
    {
        private readonly List<string> loaded = new List<string>();
        private int menuLoads;

        [SetUp]
        public void SwapLoaders()
        {
            loaded.Clear();
            menuLoads = 0;
            PartyLauncher.Loader = scene => loaded.Add(scene);
            PartyLauncher.MenuLoader = () => menuLoads++;
        }

        [TearDown]
        public void RestoreLoaders()
        {
            PartyLauncher.Loader = null;
            PartyLauncher.MenuLoader = null;
            typeof(PartyLauncher).GetMethod("ResetStatics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Services.Session.ModeId = ProgressionRules.MODE_STORY;
            Services.Session.RunMode = ProgressionRules.MODE_STORY;
            PartyCup.Reset();
        }

        [UnityTest]
        public IEnumerator Lobby_InParty_GoesToTheMinigames_WithTheCupReset()
        {
            PartyCup.Award(new[] { new PlayerSlot(0, Color.red, null, null) }, new[] { 1 });
            Services.Session.ModeId = ProgressionRules.MODE_PARTY;
            Services.Session.AddPlayer(new PlayerSlot(0, Catalog.playerPalette.Get(0), null, null) { Character = Catalog.FindCharacter("Alex") });
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            lobby.ToggleReady(lobby.Columns[0]);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsInstanceOf<PartySelectScreen>(App.Top, "Fiesta no pasa por el mapa");
            Assert.AreEqual(0, PartyCup.GamesPlayed, "una Fiesta nueva empieza en cero");
            Assert.IsFalse(App.Get<PartySelectScreen>().FinishButton.interactable, "sin minijuegos no hay ranking final");
        }

        [UnityTest]
        public IEnumerator PartyCard_IsActive_AndTheFiveMinigamesLoadTheirScenes()
        {
            var party = System.Array.Find(Catalog.modes, m => m.id == ProgressionRules.MODE_PARTY);
            Assert.IsTrue(party.implemented, "la tarjeta Fiesta ya no dice Próximamente");

            App.Push<PartySelectScreen>();
            var screen = App.Get<PartySelectScreen>();
            yield return null;
            Assert.AreEqual(5, screen.Cards.Count, "los 5 minijuegos de docs/06");
            var expected = new Dictionary<string, string> { { "goal", "BalsaGol" }, { "king", "ReyBalsa" }, { "fishing", "PescaMangos" }, { "bubble", "CarreraBurbujas" }, { "coconut", "CocoCaliente" } };
            foreach (var card in screen.Cards)
            {
                loaded.Clear();
                bool started = screen.Choose(card.game);
                if (expected.TryGetValue(card.game.id, out var scene))
                {
                    Assert.IsTrue(started, card.game.id);
                    CollectionAssert.AreEqual(new[] { scene }, loaded);
                    Assert.AreEqual(ProgressionRules.MODE_PARTY, Services.Session.RunMode);
                    Assert.AreEqual(card.game, PartyCup.Current);
                    Assert.IsTrue(UnityEditorSceneInBuild(scene), scene + " está en Build Settings");
                }
                else
                {
                    Assert.IsFalse(started, card.game.id + " llega con M7-08");
                    Assert.IsEmpty(loaded);
                }
            }
        }

        [UnityTest]
        public IEnumerator FinishParty_ShowsTheFinalRanking_AndResetsTheCup()
        {
            var alex = new PlayerSlot(0, Color.red, null, null) { Character = Catalog.FindCharacter("Alex") };
            var gloria = new PlayerSlot(1, Color.blue, null, null) { Character = Catalog.FindCharacter("Gloria") };
            PartyCup.Award(new[] { alex, gloria }, new[] { 2, 1 });
            PartyCup.Award(new[] { alex, gloria }, new[] { 1, 1 });
            var standings = PartyCup.Standings();
            Assert.AreEqual(gloria, standings[0].slot, "Gloria 6 puntos, Alex 5");
            Assert.AreEqual(6, standings[0].points);
            Assert.AreEqual(5, standings[1].points);

            App.Push<PartySelectScreen>();
            var screen = App.Get<PartySelectScreen>();
            yield return null;
            Assert.IsTrue(screen.FinishButton.interactable);
            screen.FinishButton.onClick.Invoke();
            Assert.IsTrue(screen.FinalPanel.activeSelf, "ranking final");
            var close = screen.FinalPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            close.onClick.Invoke();
            yield return null;
            Assert.IsInstanceOf<MainMenuScreen>(App.Top);
            Assert.AreEqual(0, PartyCup.GamesPlayed, "la Fiesta terminó");
        }

        [UnityTest]
        public IEnumerator FullPartyCard_StartsTheTourFromZero_AndAfterTheLastGameOpensTheFinalRankingWithBeans()
        {
            var alex = new PlayerSlot(0, Color.red, null, null) { Character = Catalog.FindCharacter("Alex") };
            var gloria = new PlayerSlot(1, Color.blue, null, null) { IsBot = true, Character = Catalog.FindCharacter("Gloria") };
            PartyCup.Award(new[] { alex, gloria }, new[] { 2, 1 });
            App.Push<PartySelectScreen>();
            var screen = App.Get<PartySelectScreen>();
            yield return null;
            Assert.IsTrue(screen.TourButton.gameObject.activeInHierarchy, "tarjeta Fiesta completa");
            screen.TourButton.onClick.Invoke();
            CollectionAssert.AreEqual(new[] { "BalsaGol" }, loaded, "empieza por el primer minijuego");
            Assert.IsTrue(PartyCup.Tour);
            Assert.AreEqual(0, PartyCup.GamesPlayed, "la Fiesta completa empieza con los puntos en cero");
            Assert.AreEqual(5, PartyCup.TourLength);

            // Fin de los 5: Alex (humano) 2.º y Gloria (bot) 1.ª en cada uno.
            for (int i = 0; i < 5; i++)
                PartyCup.Award(new[] { alex, gloria }, new[] { 2, 1 });
            PartyCup.ShowFinalOnReturn = true;
            int before = Data.frijolitos;
            screen.OnShow();
            yield return null;
            Assert.IsTrue(screen.FinalPanel.activeSelf, "al volver del último minijuego se ve el ranking final");
            Assert.AreEqual(15, PartyCup.BeansAwarded, "el mejor humano quedó 2.º en 5 minijuegos: 15 frijolitos");
            Assert.AreEqual(before + 15, Data.frijolitos);
            StringAssert.Contains("15", screen.FinalBeansText);
            screen.ShowFinal();
            Assert.AreEqual(before + 15, Data.frijolitos, "los frijolitos se dan una sola vez");
            Assert.AreEqual("mus_results", Services.Music.CurrentId);
        }

        [Test]
        public void PartyBeans_GoFrom5To25_ByBestHumanPlace_AndGamesPlayed()
        {
            Assert.AreEqual(25, PartyCup.BeansFor(1, 5));
            Assert.AreEqual(5, PartyCup.BeansFor(4, 5));
            Assert.AreEqual(5, PartyCup.BeansFor(1, 1), "un solo minijuego: 25 x 1/5");
            Assert.AreEqual(15, PartyCup.BeansFor(1, 3));
            Assert.AreEqual(0, PartyCup.BeansFor(1, 0), "sin minijuegos no hay frijolitos");
        }

        private static bool UnityEditorSceneInBuild(string scene)
        {
#if UNITY_EDITOR
            foreach (var entry in UnityEditor.EditorBuildSettings.scenes)
            {
                if (entry.enabled && entry.path.EndsWith("/" + scene + ".unity"))
                    return true;
            }
#endif
            return false;
        }
    }
}
