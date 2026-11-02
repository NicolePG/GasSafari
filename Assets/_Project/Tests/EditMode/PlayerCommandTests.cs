using GasSafari.Input;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GasSafari.Tests
{
    public class PlayerCommandTests
    {
        [Test]
        public void NetworkSerialize_RoundTripsAllFields()
        {
            var original = new PlayerCommand
            {
                SeatMove = -0.5f,
                Aim = 0.75f,
                FartHeld = true,
                HoldHeld = false,
                AbilityHeld = true,
                GrabHeld = true,
                SuperHeld = false,
                FartPresses = 250,
                AbilityPresses = 3,
                JumpPresses = 7,
                Ping = PingType.TurnLeft,
                Swim = new Vector2(0.2f, -1f)
            };

            PlayerCommand copy;
            using (var writer = new FastBufferWriter(64, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(original);
                using (var reader = new FastBufferReader(writer, Allocator.Temp))
                {
                    reader.ReadNetworkSerializable(out copy);
                }
            }

            Assert.AreEqual(original, copy);
        }

        [TestCase(5, 3, 2)]
        [TestCase(1, 255, 2)]
        [TestCase(7, 7, 0)]
        public void NewPresses_HandlesWrapAround(int current, int previous, int expected)
        {
            Assert.AreEqual(expected, PlayerCommand.NewPresses((byte)current, (byte)previous));
        }
    }
}
