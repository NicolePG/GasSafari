using System.Collections;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M7-11 desde los menús: la tarjeta está activa, la sala lleva al reparto de equipos (cada jugador cambia de lado, 2 por
    // balsa) con 3 pistas compatibles, y "¡A correr!" carga la primera en Carrera por Equipos.
    public class TeamRaceMenuTests : MenuTestBase
    {
        private readonly List<LevelData> loaded = new List<LevelData>();

        [SetUp]
        public void SwapLoaders()
        {
            loaded.Clear();
            TeamRaceLauncher.Loader = level => loaded.Add(level);
            TeamRaceScreen.Rng = new System.Random(3);
            TeamRaceSeries.Reset();
        }

        [TearDown]
        public void RestoreLoaders()
        {
            typeof(TeamRaceLauncher).GetMethod("ResetStatics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Services.Session.ModeId = ProgressionRules.MODE_STORY;
            Services.Session.RunMode = ProgressionRules.MODE_STORY;
            TeamRaceSeries.Reset();
        }

        [Test]
        public void TeamRaceCard_IsActive()
        {
            var mode = System.Array.Find(Catalog.modes, m => m.id == ProgressionRules.MODE_TEAM_RACE);
            Assert.IsTrue(mode.implemented, "la tarjeta Carrera por Equipos ya no dice Próximamente");
        }

        [UnityTest]
        public IEnumerator Lobby_GoesToTeams_PlayersSwitchSides_AndTheSeriesStarts()
        {
            Services.Session.ModeId = ProgressionRules.MODE_TEAM_RACE;
            var j1 = new PlayerSlot(0, Catalog.playerPalette.Get(0), null, null) { Character = Catalog.FindCharacter("Alex") };
            var j2 = new PlayerSlot(1, Catalog.playerPalette.Get(1), null, null) { Character = Catalog.FindCharacter("Marty") };
            var j3 = new PlayerSlot(2, Catalog.playerPalette.Get(2), null, null) { Character = Catalog.FindCharacter("Gloria") };
            Services.Session.AddPlayer(j1);
            Services.Session.AddPlayer(j2);
            Services.Session.AddPlayer(j3);
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            foreach (var column in lobby.Columns)
            {
                if (column.IsOccupied)
                    lobby.ToggleReady(column);
            }
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsInstanceOf<TeamRaceScreen>(App.Top, "Carrera por Equipos pasa por el reparto de equipos, no por el mapa");

            var screen = App.Get<TeamRaceScreen>();
            CollectionAssert.AreEqual(new[] { j1, j3 }, screen.Blue, "reparto inicial: J1 y J3 Azul");
            Assert.AreSame(j2, screen.Orange[0]);
            Assert.IsTrue(screen.Orange[1].IsBot, "un bot completa la balsa de J2");

            Assert.IsFalse(screen.Move(j2, -1), "Azul ya tiene 2 animales");
            Assert.IsTrue(screen.Move(j3, +1), "J3 se pasa al Naranja");
            CollectionAssert.AreEqual(new[] { j2, j3 }, screen.Orange);
            Assert.AreSame(j1, screen.Blue[0]);
            Assert.IsTrue(screen.Blue[1].IsBot);

            // Sin progreso solo está abierto el Mundo 1: las 3 carreras son en W1-3.
            Assert.AreEqual(3, screen.Tracks.Count);
            foreach (var track in screen.Tracks)
                Assert.IsTrue(track.raceCompatible, track.id);

            screen.StartSeries();
            Assert.AreEqual(1, loaded.Count, "carga la primera pista");
            Assert.AreSame(screen.Tracks[0], loaded[0]);
            Assert.AreEqual(ProgressionRules.MODE_TEAM_RACE, Services.Session.RunMode);
            CollectionAssert.AreEqual(screen.Tracks, TeamRaceSeries.Tracks);
            Assert.AreEqual(1, TeamRaceSeries.TeamOf(j3.Index), "el reparto llega a la carrera");
        }

        [Test]
        public void Shuffle_UsesUnlockedCompatibleTracks()
        {
            // 15 niveles con 3 estrellas = 45: abre hasta el Mundo 5 (docs/06 §2).
            for (int w = 1; w <= 3; w++)
                for (int n = 1; n <= 5; n++)
                    Data.levels.Add(new Save.SaveData.LevelRecord { id = "W" + w + "-" + n, stars = 3 });
            App.Push<TeamRaceScreen>();
            var screen = App.Get<TeamRaceScreen>();
            var seen = new HashSet<string>();
            for (int i = 0; i < 12; i++)
            {
                screen.Shuffle();
                Assert.AreEqual(3, screen.Tracks.Count);
                CollectionAssert.AllItemsAreUnique(screen.Tracks);
                foreach (var track in screen.Tracks)
                    seen.Add(track.id);
            }
            Assert.IsTrue(seen.IsSubsetOf(new[] { "W1-3", "W2-2", "W3-2", "W4-1", "W5-1", "W5-5" }));
            Assert.Greater(seen.Count, 3, "el sorteo cambia las pistas");
        }
    }
}
