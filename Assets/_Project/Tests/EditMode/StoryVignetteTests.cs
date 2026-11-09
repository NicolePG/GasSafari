using System.Linq;
using GasSafari.Editor;
using GasSafari.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Timeline;

namespace GasSafari.Tests
{
    // M5-39: las siete viñetas de historia (docs/01) con sus imágenes, línea de tiempo y pies de texto.
    public class StoryVignetteTests
    {
        private static readonly (string id, int panels)[] Expected =
        {
            ("Intro", 5), ("W1_End", 3), ("W2_End", 3), ("W3_End", 4), ("W4_End", 3), ("W5_End", 5), ("W6_Secret", 3),
        };

        private static StoryLibrary Library() => AssetDatabase.LoadAssetAtPath<StoryLibrary>(StoryBuilder.LIBRARY_PATH);

        [Test]
        public void Library_HasTheSevenMomentsWithTheirImages()
        {
            var library = Library();
            Assert.IsNotNull(library, "falta la StoryLibrary (menú Crear viñetas de historia)");
            foreach (var (id, panels) in Expected)
            {
                var data = library.Get(id);
                Assert.IsNotNull(data, id);
                Assert.AreEqual(panels, data.panels.Length, $"{id}: las imágenes que dibujó Blender");
                Assert.IsTrue(data.panels.All(p => p.image != null), $"{id}: todas las imágenes están importadas como sprite");
                Assert.IsNotNull(data.prefab, id);
            }
            Assert.AreEqual(26, Expected.Sum(e => e.panels), "26 viñetas en total");
        }

        [Test]
        public void EveryPanel_HasItsCaptionFromTheLocalizationTable()
        {
            var library = Library();
            foreach (var (id, panels) in Expected)
            {
                var data = library.Get(id);
                for (int i = 0; i < panels; i++)
                    Assert.IsNotEmpty(data.CaptionFor(i), $"{id} imagen {i + 1}: sin pie de texto (clave {data.panels[i].captionKey})");
            }
        }

        [Test]
        public void ACaptionWithoutText_IsLeftEmptyInsteadOfShowingItsKey()
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<StoryVignetteData>();
            data.strings = UnityEngine.ScriptableObject.CreateInstance<UIStrings>();
            data.strings.entries = new[] { new UIStrings.Entry { key = "story.x.1", text = "" } };
            data.panels = new[]
            {
                new StoryVignetteData.Panel { captionKey = "story.x.1" },
                new StoryVignetteData.Panel { captionKey = "story.x.2" },
                new StoryVignetteData.Panel { captionKey = "" },
            };
            Assert.AreEqual(string.Empty, data.CaptionFor(0), "clave con texto vacío");
            Assert.AreEqual(string.Empty, data.CaptionFor(1), "clave que no existe: no se muestra la clave");
            Assert.AreEqual(string.Empty, data.CaptionFor(2));
            Assert.AreEqual(string.Empty, data.CaptionFor(9));
            UnityEngine.Object.DestroyImmediate(data.strings);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void Timeline_HasOneActivationTrackPerImage_FiveSecondsEach()
        {
            foreach (var (id, panels) in Expected)
            {
                var data = Library().Get(id);
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>($"Assets/_Project/Data/Story/Story_{id}.playable");
                Assert.IsNotNull(timeline, id);
                var tracks = timeline.GetOutputTracks().OfType<ActivationTrack>().ToArray();
                Assert.AreEqual(panels, tracks.Length, id);
                Assert.AreEqual(panels * data.secondsPerPanel, timeline.duration, 0.01, $"{id}: dura {panels} x {data.secondsPerPanel} s");
                for (int i = 0; i < tracks.Length; i++)
                    Assert.AreEqual(i * data.secondsPerPanel, tracks[i].GetClips().First().start, 0.01, $"{id}: las imágenes van una tras otra");
            }
        }

        [Test]
        public void StoryMoments_ChooseTheVignetteAfterEachBossAndForTheSecretWorld()
        {
            Assert.AreEqual("W1_End", StoryMoments.AfterLevel("W1-5"));
            Assert.AreEqual("W5_End", StoryMoments.AfterLevel("W5-5"));
            Assert.IsNull(StoryMoments.AfterLevel("W3-2"));
            Assert.IsNull(StoryMoments.AfterLevel("W6-5"), "W6-5 cierra con los créditos");
            Assert.AreEqual("W6_Secret", StoryMoments.OnWorldUnlocked(6));
            Assert.IsNull(StoryMoments.OnWorldUnlocked(3));
            foreach (var (id, _) in Expected)
                Assert.IsNotNull(Library().Get(id), id);
        }

        [Test]
        public void ParseCsv_HandlesQuotesCommasAndEmbeddedNewlines()
        {
            var rows = StoryBuilder.ParseCsv("a,b,c\n\"x, y\",\"he said \"\"hi\"\"\",z\r\n\"two\nlines\",,end\n");
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("x, y", rows[1][0]);
            Assert.AreEqual("he said \"hi\"", rows[1][1]);
            Assert.AreEqual("two\nlines", rows[2][0]);
            Assert.AreEqual(string.Empty, rows[2][1]);
            Assert.AreEqual("end", rows[2][2]);
        }
    }
}
