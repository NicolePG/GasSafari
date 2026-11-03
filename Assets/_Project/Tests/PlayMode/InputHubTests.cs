using System.Collections;
using GasSafari.Core;
using GasSafari.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class InputHubTests : InputTestFixture
    {
        private InputHub hub;
        private Gamepad pad;
        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            hub = Services.InputHub;
            Services.Session.ClearPlayers();
            pad = InputSystem.AddDevice<Gamepad>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            hub.DisableJoining();
            var players = Services.Session.Players;
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].Input != null)
                    Object.Destroy(players[i].Input.gameObject);
            }
            Services.Session.ClearPlayers();
            base.TearDown();
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out var color);
            return color;
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f);
            Assert.AreEqual(expected.g, actual.g, 0.01f);
            Assert.AreEqual(expected.b, actual.b, 0.01f);
        }

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        [UnityTest]
        public IEnumerator JoiningIsOffByDefault()
        {
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual(0, Services.Session.Players.Count);
        }

        [UnityTest]
        public IEnumerator GamepadAThenKeyboardSpace_JoinWithPlayerColors()
        {
            hub.EnableJoining();
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual(1, Services.Session.Players.Count);
            var p1 = Services.Session.Players[0];
            Assert.AreEqual(0, p1.Index);
            AssertColor(Hex("#F28C28"), p1.Color);
            CollectionAssert.Contains(p1.Input.devices, pad);
            Assert.IsNotNull(p1.CommandSource);

            yield return Tap(keyboard.spaceKey);
            Assert.AreEqual(2, Services.Session.Players.Count);
            var p2 = Services.Session.Players[1];
            Assert.AreEqual(1, p2.Index);
            AssertColor(Hex("#3A86FF"), p2.Color);
            CollectionAssert.Contains(p2.Input.devices, keyboard);
        }

        [UnityTest]
        public IEnumerator OtherButtons_DoNotJoin()
        {
            hub.EnableJoining();
            yield return Tap(pad.buttonNorth);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(0, Services.Session.Players.Count);
        }

        [UnityTest]
        public IEnumerator RemovePlayer_FreesTheSlot()
        {
            hub.EnableJoining();
            yield return Tap(pad.buttonSouth);
            hub.RemovePlayer(Services.Session.Players[0]);
            yield return null;
            Assert.AreEqual(0, Services.Session.Players.Count);
        }
    }
}
