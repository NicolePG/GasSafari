using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Raft
{
    // Posición de un animal en el borde de la balsa. Sentado, es hijo de la balsa y no tiene física propia.
    [RequireComponent(typeof(PlayerAvatar))]
    public class PlayerSeat : MonoBehaviour
    {
        private const float INPUT_DEADZONE = 0.01f;

        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;

        public RaftSeats Raft { get; private set; }
        public float Angle { get; private set; }
        public bool IsSeated => Raft != null;
        // La máquina de estados lo apaga mientras carga o se agarra.
        public bool CanMove { get; set; } = true;
        // Giro de puntería en grados respecto de mirar al centro (lo fija FartThruster).
        public float AimOffset { get; set; }
        public Vector3 WorldPoint => transform.position;

        // Hacia el centro de la balsa en el plano XZ: el sentido en que empuja un pedo sin puntería.
        public Vector3 InwardDirection
        {
            get
            {
                var dir = Raft.transform.position - transform.position;
                dir.y = 0f;
                return dir.normalized;
            }
        }

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
        }

        public void Sit(RaftSeats raft, float angle)
        {
            Raft = raft;
            Angle = Mathf.Repeat(angle, 360f);
            transform.SetParent(raft.transform, false);
            ApplyPose();
            raft.Register(this);
            raft.Controller.AddWeight(transform, avatar.Character.weight);
        }

        public void Leave()
        {
            if (!IsSeated)
                return;

            Raft.Unregister(this);
            Raft.Controller.RemoveWeight(transform);
            transform.SetParent(null, true);
            Raft = null;
        }

        private void Update()
        {
            if (!IsSeated)
                return;

            float input = avatar.Command.SeatMove;
            if (CanMove && Mathf.Abs(input) >= INPUT_DEADZONE)
            {
                // Con la cámara detrás de la balsa, derecha lleva al animal hacia la derecha de la pantalla
                // cuando está en la mitad trasera (ángulo decreciente).
                float delta = -input * balance.seatMoveSpeed * Time.deltaTime;
                Angle = Mathf.Repeat(Angle + Raft.ClampMove(this, delta), 360f);
            }
            ApplyPose();
        }

        private void ApplyPose()
        {
            transform.localPosition = Raft.LocalSeatPosition(Angle);
            transform.localRotation = Quaternion.Euler(0f, Angle + 180f + AimOffset, 0f);
        }
    }
}
