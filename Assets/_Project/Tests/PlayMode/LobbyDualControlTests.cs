using System.Collections;
using System.Linq;
using GasSafari.Core;
using GasSafari.Progression;
using GasSafari.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // M4-03: el interruptor del control doble en la selección de personajes, manejado con eventos de un mando
    // (stick = navegación, botón Sur = Aceptar), como lo haría un jugador.
    public class LobbyDualControlTests : MenuTestBase
    {
        private Gamepad pad;

        [TearDown]
        public void RemovePad()
        {
            var players = Services.Session.Players;
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].Input != null)
                    Object.Destroy(players[i].Input.gameObject);
            }
            Services.Session.ClearPlayers();
            if (pad != null)
                InputSystem.RemoveDevice(pad);
            pad = null;
        }

        private IEnumerator Hold(GamepadState state, int frames = 3)
        {
            InputSystem.QueueStateEvent(pad, state);
            for (int i = 0; i < frames; i++)
                yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            for (int i = 0; i < 2; i++)
                yield return null;
        }

        private IEnumerator Stick(Vector2 direction) => Hold(new GamepadState { leftStick = direction });
        private IEnumerator Accept() => Hold(new GamepadState { buttons = 1u << (int)GamepadButton.South });

        private IEnumerator JoinWithPad(LobbyScreen lobby)
        {
            pad = InputSystem.AddDevice<Gamepad>();
            yield return Accept(); // el mismo botón que une al jugador
            Assert.AreEqual(1, Services.Session.Players.Count, "el mando se unió");
            yield return new WaitForSecondsRealtime(0.5f); // la columna ignora la entrada un instante al unirse
            Assert.IsTrue(lobby.Columns[0].IsOccupied);
        }

        [UnityTest]
        public IEnumerator Lobby_DualRow_TogglesTheSecondAnimal_WithNavigationEvents()
        {
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            yield return JoinWithPad(lobby);

            var column = lobby.Columns[0];
            Assert.IsTrue(column.DualButton.gameObject.activeSelf, "con mando aparece el interruptor");
            for (int i = 0; i < 3; i++)
                yield return Stick(Vector2.down); // personaje → sombrero → gas → doble
            Assert.AreEqual(3, column.Row, "la fila del control doble va después del color de gas");

            yield return Accept();
            Assert.IsNotNull(column.Slot.Partner, "Aceptar activa el control doble");
            Assert.IsTrue(lobby.Columns[1].IsOccupied, "el segundo animal ocupa la siguiente columna");
            Assert.IsTrue(lobby.IsPartner(lobby.Columns[1]));
            Assert.AreEqual(column.Slot.Partner, lobby.Columns[1].Slot);
            Assert.AreNotEqual(column.Character.id, lobby.Columns[1].Character.id);
            Assert.IsFalse(lobby.Columns[1].ReadyButton.gameObject.activeSelf, "el segundo animal no tiene botón Listo");
            Assert.IsFalse(lobby.Columns[1].DualButton.gameObject.activeSelf);

            // Izquierda/derecha en esa fila cambian el personaje del segundo animal.
            string before = lobby.Columns[1].Character.id;
            yield return Stick(Vector2.right);
            Assert.AreNotEqual(before, lobby.Columns[1].Character.id);
            Assert.AreEqual(lobby.Columns[1].Character, column.Slot.Partner.Character);
            Assert.AreNotEqual(column.Character.id, lobby.Columns[1].Character.id, "no se repite animal");

            // Listo del dueño basta para empezar: el segundo animal no bloquea la cuenta.
            lobby.ToggleReady(lobby.Columns[1]);
            Assert.IsFalse(lobby.Columns[1].Ready, "el segundo animal no se pone Listo por su cuenta");
            lobby.ToggleReady(column);
            Assert.IsTrue(lobby.IsStarting);
            lobby.ToggleReady(column);
            Assert.IsFalse(lobby.IsStarting);

            // Aceptar otra vez lo desactiva.
            yield return Accept();
            Assert.IsNull(column.Slot.Partner);
            Assert.IsFalse(lobby.Columns[1].IsOccupied);
            Assert.AreEqual(1, Services.Session.Players.Count);
        }

        [UnityTest]
        public IEnumerator Lobby_DualRow_IsSkippedWithoutGamepad_AndHiddenForBots()
        {
            Services.Session.AddPlayer(new PlayerSlot(0, GameCatalog.Instance.playerPalette.Get(0), null, null)
            {
                Character = GameCatalog.Instance.FindCharacter("Alex")
            });
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            lobby.AddBot();
            Assert.IsFalse(lobby.Columns[0].DualButton.gameObject.activeSelf, "sin mando no hay control doble");
            Assert.IsFalse(lobby.Columns[1].DualButton.gameObject.activeSelf, "un bot tampoco");
            Assert.IsFalse(lobby.ToggleDual(lobby.Columns[0]));
            Assert.IsNull(Services.Session.Players[0].Partner);
        }

        [UnityTest]
        public IEnumerator Lobby_Back_TurnsTheDualControlOff()
        {
            App.Push<LobbyScreen>();
            var lobby = App.Get<LobbyScreen>();
            yield return null;
            yield return JoinWithPad(lobby);
            Assert.IsTrue(lobby.ToggleDual(lobby.Columns[0]));
            Assert.AreEqual(2, Services.Session.Players.Count);
            lobby.LeaveLobby();
            yield return null;
            Assert.IsTrue(Services.Session.Players.All(p => p.Partner == null), "al volver atrás se quita el segundo animal");
            Assert.AreEqual(1, Services.Session.Players.Count);
        }
    }
}
