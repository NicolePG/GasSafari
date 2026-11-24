using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // M7-11: reglas de la serie de Carrera por Equipos (docs/06 §1) sin cargar escenas: mejor de 3, reparto de equipos con
    // 2 animales por balsa, bots que completan y pistas compatibles.
    public class TeamRaceSeriesTests
    {
        private GameCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = GameCatalog.Instance;
            TeamRaceSeries.Reset();
        }

        [TearDown]
        public void TearDown() => TeamRaceSeries.Reset();

        private LevelData Track(string id) => catalog.FindLevel(id);

        private PlayerSlot Human(int index, string character) =>
            new PlayerSlot(index, catalog.playerPalette.Get(index), null, null) { Character = catalog.FindCharacter(character) };

        [Test]
        public void BestOfThree_EndsAtTwoWins_AndAdvancesTrackByTrack()
        {
            TeamRaceSeries.Begin(new[] { Track("W1-3"), Track("W2-2"), Track("W4-1") });
            Assert.AreEqual("W1-3", TeamRaceSeries.CurrentTrack.id);
            Assert.IsNull(TeamRaceSeries.Advance(), "sin ganador no se pasa a la pista siguiente");

            TeamRaceSeries.RecordRace(TeamRaceMode.ORANGE, 100f);
            TeamRaceSeries.RecordRace(TeamRaceMode.BLUE, 90f);
            Assert.AreEqual(1, TeamRaceSeries.Wins(TeamRaceMode.ORANGE), "una carrera tiene un solo ganador");
            Assert.AreEqual(0, TeamRaceSeries.Wins(TeamRaceMode.BLUE));
            Assert.IsFalse(TeamRaceSeries.IsOver);

            Assert.AreEqual("W2-2", TeamRaceSeries.Advance().id);
            TeamRaceSeries.RecordRace(TeamRaceMode.BLUE, 80f);
            Assert.AreEqual(-1, TeamRaceSeries.SeriesWinner, "1-1: falta la tercera");
            Assert.AreEqual("W4-1", TeamRaceSeries.Advance().id);
            TeamRaceSeries.RecordRace(TeamRaceMode.BLUE, 70f);
            Assert.IsTrue(TeamRaceSeries.IsOver);
            Assert.AreEqual(TeamRaceMode.BLUE, TeamRaceSeries.SeriesWinner);
            CollectionAssert.AreEqual(new[] { 1, 0, 0 }, TeamRaceSeries.Winners);
            Assert.IsNull(TeamRaceSeries.Advance(), "terminada la serie no hay más pistas");
        }

        [Test]
        public void TwoNil_EndsTheSeriesWithoutTheThirdTrack()
        {
            TeamRaceSeries.Begin(new[] { Track("W1-3"), Track("W2-2"), Track("W4-1") });
            TeamRaceSeries.RecordRace(TeamRaceMode.BLUE, 100f);
            TeamRaceSeries.Advance();
            TeamRaceSeries.RecordRace(TeamRaceMode.BLUE, 100f);
            Assert.IsTrue(TeamRaceSeries.IsOver, "2-0 gana la serie");
            Assert.IsNull(TeamRaceSeries.Advance());

            TeamRaceSeries.ForgetCurrent();
            Assert.AreEqual(1, TeamRaceSeries.Wins(TeamRaceMode.BLUE), "repetir la carrera borra solo su resultado");
            Assert.IsFalse(TeamRaceSeries.IsOver);
        }

        [Test]
        public void Crews_TwoAnimalsPerRaft_BotsFillTheGaps()
        {
            var session = new List<PlayerSlot> { Human(0, "Alex"), Human(1, "Marty"), Human(2, "Melman") };
            TeamRaceSeries.AutoAssign(session);
            Assert.AreEqual(0, TeamRaceSeries.TeamOf(0));
            Assert.AreEqual(1, TeamRaceSeries.TeamOf(1));
            Assert.AreEqual(0, TeamRaceSeries.TeamOf(2));

            var blue = new List<PlayerSlot>();
            var orange = new List<PlayerSlot>();
            TeamRaceSeries.BuildCrews(session, blue, orange);
            Assert.AreEqual(2, blue.Count);
            Assert.AreEqual(2, orange.Count);
            CollectionAssert.AreEqual(new[] { session[0], session[2] }, blue);
            Assert.AreSame(session[1], orange[0]);
            Assert.IsTrue(orange[1].IsBot, "un bot completa la balsa Naranja");
            Assert.AreEqual(3, orange[1].Index, "con el número de jugador libre");
            Assert.IsNotNull(orange[1].Character);
            CollectionAssert.AllItemsAreUnique(new[] { blue[0].Character, blue[1].Character, orange[0].Character, orange[1].Character });

            Assert.IsFalse(TeamRaceSeries.SetTeam(session[1], 0, session), "Azul ya tiene 2 animales");
            Assert.IsTrue(TeamRaceSeries.SetTeam(session[2], 1, session));
            TeamRaceSeries.BuildCrews(session, blue, orange);
            CollectionAssert.AreEqual(new[] { session[1], session[2] }, orange);
            Assert.IsTrue(blue[1].IsBot);
        }

        [Test]
        public void Tracks_AreTheSixCompatibleOnes_ThreePerSeries()
        {
            var all = TeamRaceSeries.AvailableTracks(catalog, null);
            CollectionAssert.AreEquivalent(new[] { "W1-3", "W2-2", "W3-2", "W4-1", "W5-1", "W5-5" }, all.ConvertAll(l => l.id));
            var picked = TeamRaceSeries.PickTracks(all, new System.Random(7));
            Assert.AreEqual(TeamRaceSeries.RACES, picked.Count);
            CollectionAssert.AllItemsAreUnique(picked, "sin repetir si hay 3 o más");

            // Con solo el Mundo 1 abierto queda W1-3: las 3 carreras son en esa pista.
            var save = new Save.SaveData();
            var open = TeamRaceSeries.AvailableTracks(catalog, save);
            CollectionAssert.AreEqual(new[] { "W1-3" }, open.ConvertAll(l => l.id));
            var repeated = TeamRaceSeries.PickTracks(open, new System.Random(7));
            Assert.AreEqual(3, repeated.Count);
            Assert.IsTrue(repeated.TrueForAll(l => l.id == "W1-3"));
        }
    }
}
