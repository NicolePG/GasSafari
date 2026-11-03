using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Characters
{
    // Caída al agua, subida y reaparición automática (docs/02 §9).
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat), typeof(AvatarStateMachine))]
    [RequireComponent(typeof(Rigidbody), typeof(GasTank))]
    public class AvatarFall : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private AvatarStateMachine states;
        private Rigidbody rb;
        private GasTank gas;
        private float lastAngle;

        public RaftSeats HomeRaft { get; private set; }
        public bool IsOverboard { get; private set; }
        public float TimeOverboard { get; private set; }

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            seat = GetComponent<PlayerSeat>();
            states = GetComponent<AvatarStateMachine>();
            rb = GetComponent<Rigidbody>();
            gas = GetComponent<GasTank>();
            rb.mass = avatar.Character.weight;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            SetOnRaftPhysics();
        }

        // Golpe recibido sentado: sale despedido hacia afuera de la balsa a 'speed' m/s.
        public bool Knockoff(float impulse, float speed)
        {
            if (!seat.IsSeated || states.IsGrabbing || impulse <= balance.knockoffImpulse)
                return false;

            var outward = transform.position - seat.Raft.transform.position;
            outward.y = 0f;
            Fall(outward.normalized * speed);
            return true;
        }

        // Cae sin importar el umbral ni el agarre (vuelco de su lado).
        public void Fall(Vector3 kick)
        {
            if (!seat.IsSeated)
                return;

            HomeRaft = seat.Raft;
            lastAngle = seat.Angle;
            var carried = HomeRaft.Controller.Body.GetPointVelocity(transform.position);
            seat.Leave();
            SetInWaterPhysics();
            rb.velocity = carried + kick;

            IsOverboard = true;
            TimeOverboard = 0f;
            states.Enter(AvatarState.Falling);
            GameEvents.RaisePlayerFell(avatar);
        }

        // Vuelve a la balsa en el ángulo libre más cercano (trepar, rescate o reaparición).
        public void Board(float angle)
        {
            if (!IsOverboard)
                return;

            IsOverboard = false;
            SetOnRaftPhysics();
            seat.Sit(HomeRaft, HomeRaft.FindFreeAngle(angle));
            states.Enter(AvatarState.Seated);
        }

        private void Update()
        {
            if (!IsOverboard)
                return;

            TimeOverboard += Time.deltaTime;
            if (TimeOverboard >= Services.Session.Difficulty.autoRespawnTime)
                AutoRespawn();
        }

        private void AutoRespawn()
        {
            states.Enter(AvatarState.Respawning);
            gas.SetFraction(balance.respawnGasFraction);
            Board(lastAngle);
        }

        private void SetInWaterPhysics()
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            gameObject.layer = GameLayers.CharacterInWater;
        }

        private void SetOnRaftPhysics()
        {
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            gameObject.layer = GameLayers.Character;
        }
    }
}
