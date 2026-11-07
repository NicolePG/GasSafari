using GasSafari.Core;
using GasSafari.Input;
using GasSafari.Raft;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Characters
{
    // Nado y subida a la balsa (docs/02 §9). La dirección del stick es relativa a la cámara.
    [RequireComponent(typeof(AvatarFall), typeof(CapsuleCollider))]
    public class AvatarSwim : MonoBehaviour
    {
        // Contacto con normal mayormente hacia arriba = apoyado sobre la cubierta.
        private const float DECK_NORMAL_Y = 0.7f;

        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;
        private AvatarStateMachine states;
        private AvatarFall fall;
        private Rigidbody rb;
        private CapsuleCollider body;
        private Transform view;
        private byte lastJumpPresses;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            states = GetComponent<AvatarStateMachine>();
            fall = GetComponent<AvatarFall>();
            rb = GetComponent<Rigidbody>();
            body = GetComponent<CapsuleCollider>();
            var cam = Camera.main;
            view = cam != null ? cam.transform : null;
        }

        private void Update()
        {
            var cmd = avatar.Command;
            bool jumped = PlayerCommand.NewPresses(cmd.JumpPresses, lastJumpPresses) > 0;
            lastJumpPresses = cmd.JumpPresses;

            if (jumped && states.State == AvatarState.Swimming
                && fall.HomeRaft.DistanceToEdge(transform.position) <= balance.climbReach)
            {
                Climb();
            }
        }

        private void FixedUpdate()
        {
            var state = states.State;
            if (state != AvatarState.Falling && state != AvatarState.Swimming)
                return;
            if (!WaterSurface.TryGetHeight(rb.position, out float waterHeight))
                return;

            if (state == AvatarState.Falling)
            {
                if (rb.position.y > waterHeight)
                    return;
                if (fall.IsLeaping)
                {
                    fall.EndLeap();
                    if (!fall.IsOverboard)
                        return;
                }
                rb.useGravity = false;
                states.Enter(AvatarState.Swimming);
            }

            var move = ToWorld(Vector2.ClampMagnitude(avatar.Command.Swim, 1f)) * balance.swimSpeed;
            float rise = (waterHeight - rb.position.y) / Time.fixedDeltaTime;
            rb.velocity = new Vector3(move.x, rise, move.z);
        }

        private void OnCollisionEnter(Collision collision) => OnRaftContact(collision);
        private void OnCollisionStay(Collision collision) => OnRaftContact(collision);

        private void OnRaftContact(Collision collision)
        {
            if (collision.gameObject.layer != GameLayers.Raft)
                return;

            // Salió despedido pero cayó sobre la cubierta (dentro de la huella, no en el borde): vuelve a su sitio.
            if (states.State == AvatarState.Falling && collision.GetContact(0).normal.y > DECK_NORMAL_Y
                && fall.HomeRaft.IsOverDeck(transform.position, body.radius))
            {
                fall.Board(fall.HomeRaft.AngleTowards(transform.position));
                return;
            }
            if (states.State == AvatarState.Swimming)
                CheckRunOver(collision);
        }

        // Si la balsa pasa por encima del caído (lo alcanza más rápido de lo que él nada), se sube solo.
        private void CheckRunOver(Collision collision)
        {

            var raftBody = collision.rigidbody;
            var toSwimmer = rb.position - raftBody.position;
            toSwimmer.y = 0f;
            if (Vector3.Dot(raftBody.velocity, toSwimmer.normalized) <= balance.swimSpeed)
                return;

            Climb();
            GameEvents.RaisePlayerRescued(avatar, null);
        }

        private void Climb()
        {
            states.Enter(AvatarState.Climbing);
            fall.Board(fall.HomeRaft.AngleTowards(transform.position));
        }

        private Vector3 ToWorld(Vector2 input)
        {
            if (view == null)
                return new Vector3(input.x, 0f, input.y);

            var forward = view.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);
            return right * input.x + forward * input.y;
        }
    }
}
