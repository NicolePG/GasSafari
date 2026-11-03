using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Salto de Julien (docs/03, botón A/Cruz): sale hacia afuera de la balsa hasta julienJumpDistance
    // para atrapar algo. Al caer cerca del borde vuelve solo.
    [RequireComponent(typeof(AvatarFall), typeof(AvatarStateMachine))]
    public class JulienLeap : MonoBehaviour
    {
        // Ángulo de alcance máximo; la distancia sale de BalanceConfig.
        private const float LAUNCH_ANGLE = 45f;

        [SerializeField] private BalanceConfig balance;

        private readonly AbilityCooldown cooldown = new AbilityCooldown();
        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private AvatarFall fall;
        private AvatarStateMachine states;
        private byte lastJumpPresses;

        public AbilityCooldown Cooldown => cooldown;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            seat = GetComponent<PlayerSeat>();
            fall = GetComponent<AvatarFall>();
            states = GetComponent<AvatarStateMachine>();
        }

        private void Update()
        {
            cooldown.Tick(Time.deltaTime);
            var cmd = avatar.Command;
            bool pressed = PlayerCommand.NewPresses(cmd.JumpPresses, lastJumpPresses) > 0;
            lastJumpPresses = cmd.JumpPresses;

            if (pressed && cooldown.IsReady && states.State == AvatarState.Seated)
                Jump();
        }

        private void Jump()
        {
            var outward = transform.position - seat.Raft.transform.position;
            outward.y = 0f;
            outward.Normalize();

            float height = 0f;
            if (RaftBuoyancy.TryGetWaterHeight(transform.position, out float water))
                height = Mathf.Max(0f, transform.position.y - water);

            fall.Leap(LaunchVelocity(outward, balance.julienJumpDistance, height), balance.julienJumpReturnDistance);
            cooldown.Start(balance.julienJumpCooldown);
        }

        // Velocidad para caer a 'distance' en horizontal partiendo 'height' sobre el agua.
        public static Vector3 LaunchVelocity(Vector3 outward, float distance, float height)
        {
            float rad = LAUNCH_ANGLE * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float g = Physics.gravity.magnitude;
            float speed = Mathf.Sqrt(g * distance * distance / (2f * cos * cos * (height + distance * Mathf.Tan(rad))));
            return outward * (speed * cos) + Vector3.up * (speed * Mathf.Sin(rad));
        }
    }
}
