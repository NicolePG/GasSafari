using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Input
{
    [RequireComponent(typeof(PlayerInput))]
    public class HumanCommandSource : MonoBehaviour, IPlayerCommandSource
    {
        private const float PING_THRESHOLD = 0.5f;

        private InputAction move, aim, fart, hold, ability, grab, superLeft, superRight, ping, jump;
        private byte fartPresses, abilityPresses, jumpPresses;

        public PlayerCommand Current
        {
            get
            {
                var moveValue = move.ReadValue<Vector2>();
                return new PlayerCommand
                {
                    SeatMove = moveValue.x,
                    Aim = aim.ReadValue<Vector2>().x,
                    FartHeld = fart.IsPressed(),
                    HoldHeld = hold.IsPressed(),
                    AbilityHeld = ability.IsPressed(),
                    GrabHeld = grab.IsPressed(),
                    SuperHeld = superLeft.IsPressed() && superRight.IsPressed(),
                    FartPresses = fartPresses,
                    AbilityPresses = abilityPresses,
                    JumpPresses = jumpPresses,
                    Ping = ToPing(ping.ReadValue<Vector2>()),
                    Swim = moveValue
                };
            }
        }

        private void Awake()
        {
            var map = GetComponent<PlayerInput>().actions.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            aim = map.FindAction("Aim", true);
            fart = map.FindAction("Fart", true);
            hold = map.FindAction("Hold", true);
            ability = map.FindAction("Ability", true);
            grab = map.FindAction("Grab", true);
            superLeft = map.FindAction("SuperLeft", true);
            superRight = map.FindAction("SuperRight", true);
            ping = map.FindAction("Ping", true);
            jump = map.FindAction("Jump", true);
        }

        private void OnEnable()
        {
            fart.performed += CountFart;
            ability.performed += CountAbility;
            jump.performed += CountJump;
        }

        private void OnDisable()
        {
            fart.performed -= CountFart;
            ability.performed -= CountAbility;
            jump.performed -= CountJump;
        }

        // No usar nombres On<Acción>: PlayerInput en modo SendMessages los llamaría con otra firma.
        private void CountFart(InputAction.CallbackContext context) => fartPresses++;
        private void CountAbility(InputAction.CallbackContext context) => abilityPresses++;
        private void CountJump(InputAction.CallbackContext context) => jumpPresses++;

        private static PingType ToPing(Vector2 value)
        {
            if (value.y > PING_THRESHOLD) return PingType.Now;
            if (value.y < -PING_THRESHOLD) return PingType.Wait;
            if (value.x < -PING_THRESHOLD) return PingType.TurnLeft;
            if (value.x > PING_THRESHOLD) return PingType.TurnRight;
            return PingType.None;
        }
    }
}
