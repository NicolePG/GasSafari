using GasSafari.Audio;
using NUnit.Framework;

namespace GasSafari.Tests
{
    // docs/08 §2: 3 s de enfriamiento entre voces de un personaje y nunca dos a la vez; la caída siempre suena.
    public class VoiceGateTests
    {
        [Test]
        public void FirstVoice_IsAllowed()
        {
            Assert.IsTrue(new VoiceGate().TryStart(0f, false, false));
        }

        [Test]
        public void SecondVoice_WaitsForTheCooldown()
        {
            var gate = new VoiceGate();
            Assert.IsTrue(gate.TryStart(10f, false, false));
            Assert.IsFalse(gate.TryStart(12.9f, false, false));
            Assert.IsTrue(gate.TryStart(13.0f, false, false));
        }

        [Test]
        public void NeverTwoAtOnce_EvenAfterTheCooldown()
        {
            var gate = new VoiceGate();
            Assert.IsTrue(gate.TryStart(0f, false, false));
            Assert.IsFalse(gate.TryStart(5f, true, false), "sigue sonando la voz anterior");
        }

        [Test]
        public void Fall_AlwaysSounds_AndRestartsTheCooldown()
        {
            var gate = new VoiceGate();
            Assert.IsTrue(gate.TryStart(0f, false, false));
            Assert.IsTrue(gate.TryStart(0.5f, true, true), "la caída corta la voz y suena");
            Assert.IsFalse(gate.TryStart(3.4f, false, false), "el enfriamiento cuenta desde la caída");
            Assert.IsTrue(gate.TryStart(3.5f, false, false));
        }
    }
}
