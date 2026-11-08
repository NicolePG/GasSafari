using System;
using GasSafari.Level;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    // docs/07 §6: cada botón del tutorial tiene su icono de Xbox, PlayStation y teclado (Kenney Input Prompts, CC0).
    public class InputIconsTests
    {
        [Test]
        public void EveryTutorialButton_HasAnIconForEveryDevice()
        {
            var set = Resources.Load<InputIconSet>(InputIconSet.RESOURCE_PATH);
            Assert.IsNotNull(set, "Resources/InputIconSet.asset: corre Gas Safari/Setup/Importar iconos de botones (Kenney)");

            foreach (TutorialAction action in Enum.GetValues(typeof(TutorialAction)))
            {
                foreach (InputDeviceKind kind in Enum.GetValues(typeof(InputDeviceKind)))
                {
                    var tokens = InputGlyphs.Icons(action, kind);
                    Assert.IsNotEmpty(tokens, $"{action}/{kind} sin iconos");
                    foreach (var token in tokens)
                    {
                        if (token == "+" || token == "/")
                            continue;
                        Assert.IsNotNull(set.Get(token), $"{action}/{kind}: falta el icono '{token}'");
                    }
                }
            }
        }

        [Test]
        public void IconsAreSplitPerDevice()
        {
            var xbox = InputGlyphs.Icons(TutorialAction.Fart, InputDeviceKind.Xbox);
            var ps = InputGlyphs.Icons(TutorialAction.Fart, InputDeviceKind.PlayStation);
            var keyboard = InputGlyphs.Icons(TutorialAction.Fart, InputDeviceKind.Keyboard);
            Assert.AreEqual("xbox_rt", xbox[0]);
            Assert.AreEqual("playstation_trigger_r2", ps[0]);
            Assert.AreEqual("keyboard_space", keyboard[0]);
        }

        [Test]
        public void KenneyPack_IsRegisteredInCredits()
        {
            string credits = System.IO.File.ReadAllText("Assets/_Project/CREDITS.md");
            StringAssert.Contains("Input Prompts", credits);
            StringAssert.Contains("https://kenney.nl/assets/input-prompts", credits);
        }
    }
}
