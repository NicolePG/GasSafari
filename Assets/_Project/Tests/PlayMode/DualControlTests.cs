using System.Collections;
using GasSafari.Core;
using GasSafari.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M4-03: modo solo con 2 animales, un mando con dos comandos (docs/02 §13).
    public class DualControlTests : InputTestFixture
    {
        private InputHub hub;
        private Gamepad pad;
        private PlayerSlot first;
        private PlayerSlot second;

        public override void Setup()
        {
            base.Setup();
            hub = Services.InputHub;
            Services.Session.ClearPlayers();
            pad = InputSystem.AddDevice<Gamepad>();
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

        private IEnumerator JoinWithDualControl()
        {
            hub.EnableJoining();
            Press(pad.buttonSouth);
            yield return null;
            Release(pad.buttonSouth);
            yield return null;
            hub.DisableJoining();
            first = Services.Session.Players[0];
            second = hub.EnableDualControl(first);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnableDualControl_AddsASecondAnimalWithItsOwnSlot()
        {
            yield return JoinWithDualControl();

            Assert.IsNotNull(second);
            Assert.AreEqual(2, Services.Session.Players.Count);
            Assert.AreEqual(0, first.Index);
            Assert.AreEqual(1, second.Index);
            Assert.AreNotEqual(first.Color, second.Color, "cada animal con su color");
            Assert.AreSame(second, first.Partner);
            Assert.IsNull(second.Input, "el segundo animal no tiene PlayerInput propio");
            Assert.AreNotSame(first.CommandSource, second.CommandSource);
            Assert.IsNotInstanceOf<HumanCommandSource>(first.CommandSource);
        }

        // Un solo evento con todo el estado: varios Press/Set seguidos en el mismo frame se pisarían entre sí.
        private IEnumerator Send(GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gamepad_SplitsBetweenTheTwoAnimals()
        {
            yield return JoinWithDualControl();

            yield return Send(new GamepadState
            {
                leftStick = new Vector2(1f, 0f),
                rightStick = new Vector2(-0.5f, 0f),
                rightTrigger = 1f
            }.WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.West));

            var a = first.CommandSource.Current;
            var b = second.CommandSource.Current;
            Assert.AreEqual(1f, a.SeatMove, 0.05f);
            Assert.AreEqual(-0.5f, b.SeatMove, 0.05f);
            Assert.IsTrue(a.FartHeld);
            Assert.IsFalse(a.HoldHeld);
            Assert.IsFalse(b.FartHeld);
            Assert.IsTrue(b.HoldHeld, "LB aguanta al segundo");
            Assert.AreEqual(1, a.FartPresses);
            Assert.AreEqual(0, b.FartPresses);
            Assert.AreEqual(1, a.AbilityPresses, "X es del primero");
            Assert.AreEqual(0, b.AbilityPresses);

            yield return Send(new GamepadState().WithButton(GamepadButton.RightShoulder).WithButton(GamepadButton.North));

            a = first.CommandSource.Current;
            b = second.CommandSource.Current;
            Assert.IsFalse(a.FartHeld);
            Assert.IsTrue(b.FartHeld, "RB es el pedo del segundo");
            Assert.AreEqual(1, a.FartPresses);
            Assert.AreEqual(1, b.FartPresses);
            Assert.AreEqual(1, a.AbilityPresses);
            Assert.AreEqual(1, b.AbilityPresses, "Y es del segundo");
        }

        [UnityTest]
        public IEnumerator SharedButtons_GrabJumpAndSuperFartReachBothAnimals()
        {
            yield return JoinWithDualControl();

            yield return Send(new GamepadState()
                .WithButton(GamepadButton.East).WithButton(GamepadButton.South)
                .WithButton(GamepadButton.LeftStick).WithButton(GamepadButton.RightStick));

            foreach (var slot in new[] { first, second })
            {
                var cmd = slot.CommandSource.Current;
                Assert.IsTrue(cmd.GrabHeld);
                Assert.IsTrue(cmd.SuperHeld);
                Assert.AreEqual(1, cmd.JumpPresses);
            }
        }

        [UnityTest]
        public IEnumerator DisableDualControl_ReturnsToASingleAnimal()
        {
            yield return JoinWithDualControl();

            hub.DisableDualControl(first);
            yield return null;

            Assert.AreEqual(1, Services.Session.Players.Count);
            Assert.IsNull(first.Partner);
            Assert.IsInstanceOf<HumanCommandSource>(first.CommandSource);
        }

        [UnityTest]
        public IEnumerator LeavingRemovesTheSecondAnimalToo()
        {
            yield return JoinWithDualControl();

            Object.Destroy(first.Input.gameObject);
            yield return null;

            Assert.AreEqual(0, Services.Session.Players.Count);
        }

        [UnityTest]
        public IEnumerator Keyboard_CannotUseDualControl()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            hub.EnableJoining();
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return null;
            hub.DisableJoining();

            var keyboardPlayer = Services.Session.Players[0];
            Assert.IsNull(hub.EnableDualControl(keyboardPlayer), "el modo con 2 animales pide un mando");
            Assert.AreEqual(1, Services.Session.Players.Count);
        }
    }
}
