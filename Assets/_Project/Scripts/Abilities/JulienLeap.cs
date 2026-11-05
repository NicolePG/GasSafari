using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using GasSafari.PowerUps;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Salto de Julien (docs/03, botón A/Cruz): sale hasta julienJumpDistance en la dirección del stick
    // izquierdo relativa a la cámara (sin stick, hacia afuera de su asiento). Si hay un power-up a menos de
    // julienJumpSnapRadius del aterrizaje, el salto se corrige hacia él. Al caer cerca del borde vuelve solo.
    [RequireComponent(typeof(AvatarFall), typeof(AvatarStateMachine))]
    public class JulienLeap : MonoBehaviour
    {
        // Ángulo de alcance máximo; la distancia sale de BalanceConfig.
        private const float LAUNCH_ANGLE = 45f;
        private const float STICK_DEADZONE = 0.3f;

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

            var direction = StickDirection(avatar.Command.Swim, Camera.main, outward);
            float distance = balance.julienJumpDistance;
            ApplyPowerUpSnap(transform.position, ref direction, ref distance);

            float height = 0f;
            if (RaftBuoyancy.TryGetWaterHeight(transform.position, out float water))
                height = Mathf.Max(0f, transform.position.y - water);

            fall.Leap(LaunchVelocity(direction, distance, height), balance.julienJumpReturnDistance);
            cooldown.Start(balance.julienJumpCooldown);
        }

        // Stick izquierdo relativo a la cámara; sin stick (o sin cámara, ejes del mundo), 'fallback'.
        public static Vector3 StickDirection(Vector2 stick, Camera cam, Vector3 fallback)
        {
            if (stick.magnitude < STICK_DEADZONE)
                return fallback;

            var forward = cam != null ? cam.transform.forward : Vector3.forward;
            var right = cam != null ? cam.transform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            var dir = forward.normalized * stick.y + right.normalized * stick.x;
            return dir.sqrMagnitude > 0f ? dir.normalized : fallback;
        }

        // Si cae a menos de julienJumpSnapRadius de un power-up, apunta hacia él (sin pasar de julienJumpDistance).
        private void ApplyPowerUpSnap(Vector3 origin, ref Vector3 direction, ref float distance)
        {
            var landing = origin + direction * distance;
            if (!PowerUpTargets.TryFindNear(landing, balance.julienJumpSnapRadius, out var target))
                return;

            var toTarget = target.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 1e-4f)
                return;
            direction = toTarget.normalized;
            distance = Mathf.Min(toTarget.magnitude, balance.julienJumpDistance);
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
