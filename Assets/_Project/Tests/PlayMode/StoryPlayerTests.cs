using System.Collections;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M5-39: StoryPlayer reproduce la viñeta con su Timeline, muestra una imagen tras otra con su pie y se puede saltar.
    public class StoryPlayerTests
    {
        private StoryVignetteData Data(string moment)
        {
            StoryVignetteData data = null;
#if UNITY_EDITOR
            data = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryLibrary>("Assets/_Project/Data/Story/StoryLibrary.asset").Get(moment);
#endif
            return data;
        }

        [UnityTest]
        public IEnumerator Vignette_ShowsOneImageAtATimeWithItsCaption_AndFinishesAtTheEnd()
        {
            var data = Data("W1_End");
            Assert.IsNotNull(data);
            int finished = 0;
            var player = StoryPlayer.Play(data, () => finished++);
            Assert.IsNotNull(player);
            yield return null;
            yield return null;
            Assert.IsTrue(player.IsPlaying);
            Assert.AreEqual(0, player.PanelIndex, "empieza con la primera imagen");
            var caption = player.GetComponentsInChildren<TMPro.TMP_Text>(true)[0];
            Assert.AreEqual(data.CaptionFor(0), caption.text, "con el pie de texto de la tabla");

            // Salta en el tiempo de la línea de tiempo a la segunda imagen y a la tercera.
            player.Director.time = data.secondsPerPanel + 0.5f;
            player.Director.Evaluate();
            yield return null;
            Assert.AreEqual(1, player.PanelIndex);
            player.Director.time = data.secondsPerPanel * 2f + 0.5f;
            player.Director.Evaluate();
            yield return null;
            Assert.AreEqual(2, player.PanelIndex);

            player.Director.time = data.TotalSeconds + 0.1f;
            player.Director.Evaluate();
            player.Director.Stop();
            yield return null;
            yield return null;
            Assert.AreEqual(1, finished, "al terminar avisa una sola vez");
            Assert.IsTrue(player == null, "el lienzo se destruye");
        }

        [UnityTest]
        public IEnumerator Skip_FinishesRightAway_AndIsReportedAsSkipped()
        {
            var data = Data("Intro");
            int finished = 0;
            var player = StoryPlayer.Play(data, () => finished++);
            yield return null;
            Assert.IsTrue(player.IsPlaying);
            player.Skip();
            yield return null;
            Assert.AreEqual(1, finished);
            Assert.IsTrue(player == null);
        }

        [UnityTest]
        public IEnumerator Play_WithoutAVignette_ReportsFinishedAtOnce()
        {
            int finished = 0;
            var player = StoryPlayer.Play(null, () => finished++);
            yield return null;
            Assert.IsNull(player);
            Assert.AreEqual(1, finished);
        }
    }
}
