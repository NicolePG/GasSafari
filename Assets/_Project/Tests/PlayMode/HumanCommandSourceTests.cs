using System.Collections;
using GasSafari.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class HumanCommandSourceTests : InputTestFixture
    {
        private const string ASSET_PATH = "Assets/_Project/Settings/GasSafariControls.inputactions";

        private Keyboard keyboard;
        private HumanCommandSource source;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();

            var go = new GameObject("Human");
            go.SetActive(false);
            var playerInput = go.AddComponent<PlayerInput>();
#if UNITY_EDITOR
            playerInput.actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(ASSET_PATH);
#endif
            playerInput.defaultActionMap = "Player";
            playerInput.defaultControlScheme = "Keyboard";
            source = go.AddComponent<HumanCommandSource>();
            go.SetActive(true);
        }

        public override void TearDown()
        {
            Object.Destroy(source.gameObject);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Keyboard_ProducesExpectedCommand()
        {
            // Un solo evento con todas las teclas: en Play Mode el fixture procesa los eventos en el
            // siguiente frame, y varios Press seguidos se pisarían entre sí.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(
                Key.D, Key.E, Key.Space, Key.LeftCtrl, Key.R, Key.Digit1));
            yield return null;

            var cmd = source.Current;
            Assert.AreEqual(1f, cmd.SeatMove, 0.001f);
            Assert.AreEqual(1f, cmd.Aim, 0.001f);
            Assert.IsTrue(cmd.FartHeld);
            Assert.IsTrue(cmd.GrabHeld);
            Assert.IsTrue(cmd.SuperHeld);
            Assert.AreEqual(1, cmd.FartPresses);
            Assert.AreEqual(PingType.Now, cmd.Ping);
            Assert.IsFalse(cmd.HoldHeld);
        }

        [UnityTest]
        public IEnumerator Presses_AreCountedPerPress()
        {
            for (int i = 0; i < 3; i++)
            {
                PressAndRelease(keyboard.spaceKey);
                PressAndRelease(keyboard.fKey);
                PressAndRelease(keyboard.cKey);
            }
            yield return null;

            var cmd = source.Current;
            Assert.AreEqual(3, cmd.FartPresses);
            Assert.AreEqual(3, cmd.AbilityPresses);
            Assert.AreEqual(3, cmd.JumpPresses);
            Assert.IsFalse(cmd.FartHeld);
        }
    }
}
