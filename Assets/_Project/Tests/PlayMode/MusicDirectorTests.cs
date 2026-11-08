using System.Collections;
using GasSafari.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-40: música por mundo con capas (docs/08 §5): base + intensidad sincronizadas, intensidad con la velocidad (8 m/s)
    // o en jefes, transición de 1 s, y tabla de pulsos de la pista rítmica de W3.
    public class MusicDirectorTests
    {
        private GameObject go;
        private MusicDirector music;
        private MusicLibrary library;
        private float speed;

        [SetUp]
        public void SetUp()
        {
            library = ScriptableObject.CreateInstance<MusicLibrary>();
            var tracks = new[]
            {
                Track("mus_test", true, null),
                Track("mus_test_boss", true, null),
                Track("mus_flat", false, null),
                Track("mus_beats", true, new TextAsset("[0.0, 0.5, 1.0, 1.5, 1.9, 2.3, 2.7, 3.1, 3.5]")),
            };
            library.Set(tracks);
            go = new GameObject("Music");
            music = go.AddComponent<MusicDirector>();
            music.Configure(library);
            music.SpeedSource = () => speed;
            speed = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(go);
            Object.Destroy(library);
        }

        private static MusicLibrary.Track Track(string id, bool withIntensity, TextAsset beats)
        {
            return new MusicLibrary.Track
            {
                id = id,
                baseClip = AudioClip.Create(id + "_base", 44100 * 4, 1, 44100, false),
                intensityClip = withIntensity ? AudioClip.Create(id + "_intensity", 44100 * 4, 1, 44100, false) : null,
                beats = beats,
            };
        }

        [UnityTest]
        public IEnumerator IntensityLayer_RisesAbove8_HoldsBetween7And8_FallsBelow7_InOneSecond()
        {
            music.PlayLevel("mus_test", false);
            speed = 3f;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual("mus_test", music.CurrentId);
            Assert.IsTrue(music.HasIntensityLayer);
            Assert.AreEqual(0f, music.IntensityLevel, 0.001f, "lento: solo la base");

            speed = 9f;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Greater(music.IntensityLevel, 0.2f, "sube en una transición, no de golpe");
            Assert.Less(music.IntensityLevel, 0.9f);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(1f, music.IntensityLevel, 0.001f, "más de 8 m/s: capa de intensidad completa");

            speed = 7.5f;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1f, music.IntensityLevel, 0.001f, "entre 7 y 8 m/s se mantiene (sin parpadeo)");

            speed = 5f;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(0f, music.IntensityLevel, 0.001f, "baja al bajar de 7 m/s");
        }

        [UnityTest]
        public IEnumerator BossLevel_KeepsTheIntensityLayerUp()
        {
            music.PlayLevel("mus_test_boss", true);
            speed = 0f;
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsTrue(music.IsBossTrack);
            Assert.AreEqual(1f, music.IntensityLevel, 0.001f, "en jefes la intensidad está arriba aunque la balsa vaya lenta");
        }

        [UnityTest]
        public IEnumerator TrackWithoutIntensityLayer_OnlyPlaysTheBase()
        {
            music.PlayLevel("mus_flat", false);
            speed = 14f;
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsFalse(music.HasIntensityLayer);
            Assert.AreEqual(0f, music.IntensityLevel, 0.001f);
        }

        [UnityTest]
        public IEnumerator BeatTable_GivesBeatPositionAndSeeksToTheFasterSection()
        {
            music.PlayLevel("mus_beats", false);
            yield return new WaitForSecondsRealtime(0.4f);
            Assume.That(music.IsPlaying, "sin dispositivo de audio no hay reproducción que medir");
            Assert.IsTrue(music.HasBeats);
            Assert.IsTrue(music.TryGetBeatPosition(out double beats));
            Assert.GreaterOrEqual(beats, 0.0);
            Assert.Less(beats, 1.5, "al principio de la pista");
            Assert.AreEqual(120f, music.CurrentBpm(), 1f, "intervalo de 0.5 s = 120 BPM");

            Assert.IsTrue(music.SeekToTempoChange(), "salta al tramo de pulsos más rápidos");
            yield return null;
            Assert.IsTrue(music.TryGetBeatPosition(out beats));
            Assert.GreaterOrEqual(beats, 3.0, "ya en el tramo rápido");
            Assert.AreEqual(150f, music.CurrentBpm(), 1f, "intervalo de 0.4 s = 150 BPM");
            Assert.IsFalse(music.SeekToTempoChange(), "no vuelve a saltar si ya está en el tramo rápido");
        }

        [UnityTest]
        public IEnumerator Stop_FadesTheMusicOut()
        {
            music.PlayLevel("mus_test", false);
            yield return new WaitForSecondsRealtime(0.3f);
            music.Stop();
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsNull(music.CurrentId);
            Assert.IsFalse(music.IsPlaying);
        }
    }
}
