using System.Collections;
using GasSafari.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class TimeControllerTests
    {
        // Las pruebas de bots corren a x3: tras la cámara lenta y la pausa se vuelve a x3, no a x1.
        [UnityTest]
        public IEnumerator BaseScale_SurvivesSlowMotionAndPause()
        {
            Assume.That(Progression.GameSettings.SlowMotion, "la cámara lenta está apagada en las opciones");
            float fixedDelta = Time.fixedDeltaTime;
            var go = new GameObject("TimeController");
            var controller = go.AddComponent<TimeController>();
            try
            {
                controller.SetBaseScale(3f);
                Assert.AreEqual(3f, Time.timeScale, 1e-4f);
                Assert.AreEqual(fixedDelta, Time.fixedDeltaTime, 1e-6f, "el paso de física no cambia con la velocidad base");

                controller.SlowMotion(0.3f, 0.1f);
                Assert.AreEqual(0.9f, Time.timeScale, 1e-4f);
                for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
                    yield return null;
                Assert.AreEqual(3f, Time.timeScale, 1e-4f, "tras la cámara lenta vuelve a la velocidad base");

                controller.SetPaused(true);
                Assert.AreEqual(0f, Time.timeScale);
                controller.SetPaused(false);
                Assert.AreEqual(3f, Time.timeScale, 1e-4f, "tras la pausa vuelve a la velocidad base");
            }
            finally
            {
                Object.Destroy(go);
            }
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, 1e-4f, "al destruirse deja el tiempo normal");
            Time.fixedDeltaTime = fixedDelta;
        }
    }
}
