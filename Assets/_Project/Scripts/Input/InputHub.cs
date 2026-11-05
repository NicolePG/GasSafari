using System;
using GasSafari.Core;
using GasSafari.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Input
{
    // Unión y salida de jugadores locales (docs/02 §13). Cada jugador es un PlayerController persistente
    // con su PlayerInput; el avatar del nivel recibe su HumanCommandSource.
    [RequireComponent(typeof(PlayerInputManager))]
    public class InputHub : MonoBehaviour
    {
        private const string JOIN_GAMEPAD = "<Gamepad>/buttonSouth";
        private const string JOIN_KEYBOARD = "<Keyboard>/space";

        [SerializeField] private GameObject playerControllerPrefab;
        [SerializeField] private PlayerPalette palette;

        private PlayerInputManager manager;
        private InputAction joinAction;

        public bool IsJoining => manager.joiningEnabled;

        public event Action<PlayerSlot> PlayerJoined;
        public event Action<PlayerSlot> PlayerLeft;

        private void Awake()
        {
            manager = GetComponent<PlayerInputManager>();
            joinAction = new InputAction("Join", InputActionType.Button);
            joinAction.AddBinding(JOIN_GAMEPAD);
            joinAction.AddBinding(JOIN_KEYBOARD);

            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
            manager.joinAction = new InputActionProperty(joinAction);
            manager.playerPrefab = playerControllerPrefab;
            manager.DisableJoining();
        }

        public void Configure(GameObject controllerPrefab, PlayerPalette playerPalette)
        {
            playerControllerPrefab = controllerPrefab;
            palette = playerPalette;
            if (manager != null)
                manager.playerPrefab = controllerPrefab;
        }

        private void OnEnable()
        {
            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;
        }

        private void OnDisable()
        {
            manager.onPlayerJoined -= OnPlayerJoined;
            manager.onPlayerLeft -= OnPlayerLeft;
        }

        private void OnDestroy()
        {
            joinAction.Dispose();
        }

        // Pantalla de selección: "Pulsa A para unirte".
        public void EnableJoining()
        {
            if (Services.Session.Players.Count < GameSession.MAX_PLAYERS)
                manager.EnableJoining();
        }

        public void DisableJoining()
        {
            manager.DisableJoining();
        }

        // Modo solo con 2 animales (docs/02 §13): el mando de 'owner' controla además un segundo animal, con su propio
        // slot y color. Devuelve ese slot, o null si no cabe otro jugador o el dueño no usa un mando.
        public PlayerSlot EnableDualControl(PlayerSlot owner)
        {
            if (owner.Input == null || owner.Partner != null || Services.Session.Players.Count >= GameSession.MAX_PLAYERS)
                return null;
            if (!HasGamepad(owner.Input))
                return null;

            var dual = owner.Input.GetComponent<DualHumanCommandSource>();
            if (dual == null)
                dual = owner.Input.gameObject.AddComponent<DualHumanCommandSource>();
            owner.CommandSource = dual.First;

            int index = FreeIndex();
            var second = new PlayerSlot(index, palette.Get(index), null, dual.Second);
            owner.Partner = second;
            Services.Session.AddPlayer(second);
            if (Services.Session.Players.Count >= GameSession.MAX_PLAYERS)
                manager.DisableJoining();
            return second;
        }

        // Vuelve al control normal de un solo animal.
        public void DisableDualControl(PlayerSlot owner)
        {
            if (owner.Partner == null)
                return;
            Services.Session.RemovePlayer(owner.Partner);
            owner.Partner = null;
            owner.CommandSource = owner.Input.GetComponent<HumanCommandSource>();
            Destroy(owner.Input.GetComponent<DualHumanCommandSource>());
        }

        private static bool HasGamepad(PlayerInput input)
        {
            foreach (var device in input.devices)
            {
                if (device is Gamepad)
                    return true;
            }
            return false;
        }

        // El índice libre más bajo (el color del jugador sale de él).
        private static int FreeIndex()
        {
            var players = Services.Session.Players;
            for (int index = 0; index < GameSession.MAX_PLAYERS; index++)
            {
                bool used = false;
                for (int i = 0; i < players.Count; i++)
                    used |= players[i].Index == index;
                if (!used)
                    return index;
            }
            return players.Count;
        }

        // Salir con B/Círculo desde "no listo".
        public void RemovePlayer(PlayerSlot slot)
        {
            if (slot.Input != null)
                Destroy(slot.Input.gameObject);
        }

        private void OnPlayerJoined(PlayerInput input)
        {
            input.transform.SetParent(transform, false);
            var slot = new PlayerSlot(input.playerIndex, palette.Get(input.playerIndex), input,
                input.GetComponent<IPlayerCommandSource>());
            Services.Session.AddPlayer(slot);
            if (Services.Session.Players.Count >= GameSession.MAX_PLAYERS)
                manager.DisableJoining();
            PlayerJoined?.Invoke(slot);
        }

        private void OnPlayerLeft(PlayerInput input)
        {
            var players = Services.Session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Input != input)
                    continue;
                var slot = players[i];
                Services.Session.RemovePlayer(slot);
                PlayerLeft?.Invoke(slot);
                // Su segundo animal (modo solo con 2) se va con él.
                if (slot.Partner != null)
                {
                    var partner = slot.Partner;
                    slot.Partner = null;
                    Services.Session.RemovePlayer(partner);
                    PlayerLeft?.Invoke(partner);
                }
                return;
            }
        }
    }
}
