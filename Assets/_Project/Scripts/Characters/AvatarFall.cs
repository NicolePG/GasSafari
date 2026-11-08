using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Characters
{
    // Caída al agua, subida y reaparición automática (docs/02 §9).
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat), typeof(AvatarStateMachine))]
    [RequireComponent(typeof(Rigidbody), typeof(GasTank))]
    public class AvatarFall : MonoBehaviour
    {
        // Un golpe contra una pared solo lo deja caer junto a la balsa: salida suave, no un lanzamiento.
        private const float MAX_KNOCK_KICK = 3f;
        private const float KNOCK_LIFT = 1.5f;
        // Si sigue en el aire tanto tiempo, cayó fuera del agua (acera, pared, abismo): se le pone junto a la balsa.
        private const float MAX_AIR_TIME = 1.5f;
        private const float BESIDE_RAFT_GAP = 1.2f;
        // Rebote al reaparecer en la balsa.
        private const float RESPAWN_DROP_HEIGHT = 1.2f;
        private const float RESPAWN_DROP_TIME = 0.25f;
        private const float RESPAWN_BOUNCE_HEIGHT = 0.35f;
        private const float RESPAWN_BOUNCE_TIME = 0.3f;

        [SerializeField] private BalanceConfig balance;

        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private AvatarStateMachine states;
        private Rigidbody rb;
        private GasTank gas;
        private float lastAngle;
        private float airTime;
        private float bounceTime = -1f;

        public RaftSeats HomeRaft { get; private set; }
        public bool IsOverboard { get; private set; }
        public float TimeOverboard { get; private set; }
        // Salto de Julien en curso (todavía no tocó el agua).
        public bool IsLeaping { get; private set; }

        private float leapReturnDistance;

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

        // Golpe recibido sentado: solo cae quien no se agarra y solo si el impacto supera knockoffSpeed (m/s).
        // Sale hacia afuera de la balsa a 'speed' m/s, como mucho MAX_KNOCK_KICK.
        public bool Knockoff(float impactSpeed, float speed)
        {
            if (!seat.IsSeated || states.IsGrabbing || impactSpeed <= balance.knockoffSpeed)
                return false;

            var outward = transform.position - seat.Raft.transform.position;
            outward.y = 0f;
            Fall(outward.normalized * Mathf.Min(speed, MAX_KNOCK_KICK) + Vector3.up * KNOCK_LIFT);
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
            airTime = 0f;
            CancelBounce();
            states.Enter(AvatarState.Falling);
            GameEvents.RaisePlayerFell(avatar);
        }

        // Salto voluntario (Julien): sale de la balsa sin contar como caída y puede atrapar power-ups.
        public void Leap(Vector3 velocity, float returnDistance)
        {
            if (!seat.IsSeated)
                return;

            HomeRaft = seat.Raft;
            lastAngle = seat.Angle;
            var carried = HomeRaft.Controller.Body.GetPointVelocity(transform.position);
            seat.Leave();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            gameObject.layer = GameLayers.Character;
            rb.velocity = carried + velocity;

            IsOverboard = true;
            IsLeaping = true;
            leapReturnDistance = returnDistance;
            TimeOverboard = 0f;
            airTime = 0f;
            states.Enter(AvatarState.Falling);
        }

        // Toca el agua tras el salto: cerca del borde vuelve solo; si no, cae con las reglas normales.
        public void EndLeap()
        {
            IsLeaping = false;
            if (HomeRaft.DistanceToEdge(transform.position) <= leapReturnDistance)
            {
                Board(HomeRaft.AngleTowards(transform.position));
                return;
            }
            gameObject.layer = GameLayers.CharacterInWater;
            GameEvents.RaisePlayerFell(avatar);
        }

        // Vuelve a la balsa en el ángulo libre más cercano (trepar, rescate o reaparición).
        public void Board(float angle)
        {
            if (!IsOverboard)
                return;

            IsOverboard = false;
            IsLeaping = false;
            SetOnRaftPhysics();
            seat.Sit(HomeRaft, HomeRaft.FindFreeAngle(angle));
            states.Enter(AvatarState.Seated);
        }

        private void Update()
        {
            UpdateRespawnBounce();

            if (!IsOverboard)
                return;

            TimeOverboard += Time.deltaTime;
            if (states.State == AvatarState.Falling && !IsLeaping)
            {
                airTime += Time.deltaTime;
                if (airTime > MAX_AIR_TIME)
                    PlaceBesideRaft();
            }
            if (TimeOverboard >= Services.Session.Difficulty.autoRespawnTime)
                AutoRespawn();
        }

        // Reaparición del equipo en un checkpoint: vuelve a su asiento si estaba en el agua.
        public void ReturnToRaft()
        {
            if (!IsOverboard)
                return;
            states.Enter(AvatarState.Respawning);
            Board(lastAngle);
        }

        // Reaparición solo tras caer (6 s): instantánea, con un rebote sobre la balsa y sin fundido ni carga de escena.
        private void AutoRespawn()
        {
            gas.SetFraction(balance.respawnGasFraction);
            ReturnToRaft();
            if (seat.IsSeated)
                bounceTime = 0f;
        }

        private void CancelBounce()
        {
            bounceTime = -1f;
            seat.HopOffset = 0f;
        }

        // Altura extra del asiento durante el rebote: cae desde arriba y rebota una vez antes de quedarse quieto.
        private void UpdateRespawnBounce()
        {
            if (bounceTime < 0f)
                return;

            bounceTime += Time.deltaTime;
            float height = 0f;
            if (bounceTime < RESPAWN_DROP_TIME)
            {
                float k = 1f - bounceTime / RESPAWN_DROP_TIME;
                height = RESPAWN_DROP_HEIGHT * k * k;
            }
            else if (bounceTime < RESPAWN_DROP_TIME + RESPAWN_BOUNCE_TIME)
            {
                float u = (bounceTime - RESPAWN_DROP_TIME) / RESPAWN_BOUNCE_TIME;
                height = RESPAWN_BOUNCE_HEIGHT * 4f * u * (1f - u);
            }
            else
            {
                bounceTime = -1f;
            }
            seat.HopOffset = height;
        }

        // Cayó fuera del agua: lo deja nadando pegado a la balsa, del lado de su asiento, para que nunca desaparezca.
        private void PlaceBesideRaft()
        {
            var raftTransform = HomeRaft.transform;
            var side = raftTransform.TransformDirection(HomeRaft.LocalSeatPosition(lastAngle));
            side.y = 0f;
            if (side.sqrMagnitude < 1e-4f)
                side = raftTransform.right;
            var position = raftTransform.position + side.normalized * (balance.raftRadius + BESIDE_RAFT_GAP);
            float height = WaterSurface.TryGetHeight(position + Vector3.up * 2f, out float water) ? water : raftTransform.position.y;
            position.y = height - 0.05f;

            rb.position = position;
            transform.position = position;
            rb.velocity = Vector3.zero;
            airTime = 0f;
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
