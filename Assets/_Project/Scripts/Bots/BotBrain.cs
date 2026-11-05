using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Bots
{
    // Cerebro de un bot (docs/10 §15, skill bot-ai): percibe el cauce y los peligros cada 0.1 s y escribe un
    // PlayerCommand en su BotCommandSource. Nunca toca la física: se sienta, apunta y pedorrea como un humano.
    [DefaultExecutionOrder(-60)]
    [RequireComponent(typeof(PlayerAvatar), typeof(PlayerSeat), typeof(FartThruster))]
    [RequireComponent(typeof(GasTank), typeof(AvatarStateMachine), typeof(AvatarFall))]
    public partial class BotBrain : MonoBehaviour
    {
        // Cada bot de una balsa ocupa un lugar distinto entre los asientos traseros: se ordenan por esta lista.
        private static readonly List<BotBrain> Active = new List<BotBrain>();
        private const int MAX_HAZARD_HITS = 8;
        // Por debajo de este paso el asiento se considera en su sitio (grados).
        private const float SEAT_DEADZONE = 2f;
        private const float STRAIGHT_LOOK_NEAR = 6f;
        private const float STRAIGHT_LOOK_SECONDS = 4f;

        private readonly RaycastHit[] hazardHits = new RaycastHit[MAX_HAZARD_HITS];

        private BotConfig config;
        private BotCommandSource source;
        private PlayerAvatar avatar;
        private PlayerSeat seat;
        private GasTank gas;
        private AvatarStateMachine states;
        private AvatarFall fall;
        private FartThruster thruster;
        private FlowSpline flow;
        private int flowHint;
        private Vector3 fallbackHeading;
        private float decideTimer;
        private float puffTimer;
        private float lastFartTime;
        private bool blasting;
        private bool tiltLocked;

        public BotConfig Config => config;
        public BotCommandSource Source => source;
        public bool IsBlasting => blasting;
        public bool IsTiltLocked => tiltLocked;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            seat = GetComponent<PlayerSeat>();
            gas = GetComponent<GasTank>();
            states = GetComponent<AvatarStateMachine>();
            fall = GetComponent<AvatarFall>();
            thruster = GetComponent<FartThruster>();
            AwakeSkills();
        }

        // Lo llama quien crea al bot (RaftSpawner) justo después de instanciar el avatar.
        public void Attach(BotCommandSource commandSource, BotConfig botConfig)
        {
            source = commandSource;
            config = botConfig != null ? botConfig : ScriptableObject.CreateInstance<BotConfig>();
            avatar.SetCommandSource(source);
            decideTimer = Random.Range(0f, config.decideInterval); // los bots no deciden todos en el mismo frame
        }

        private void OnEnable()
        {
            Active.Add(this);
            thruster.Farted += OnOwnFart;
            lastFartTime = Time.time;
            OnEnableSkills();
        }

        private void OnDisable()
        {
            Active.Remove(this);
            thruster.Farted -= OnOwnFart;
            OnDisableSkills();
        }

        private void Start()
        {
            flow = FindFirstObjectByType<FlowSpline>();
            fallbackHeading = seat.Raft != null ? Flatten(seat.Raft.transform.forward) : Vector3.forward;
        }

        private void OnOwnFart(FartType type, float impulse, float multiplier)
        {
            lastFartTime = Time.time;
        }

        private void Update()
        {
            if (source == null)
                return;

            UpdateSkills(Time.deltaTime);
            decideTimer -= Time.deltaTime;
            if (decideTimer > 0f)
                return;
            decideTimer += config.decideInterval;
            Think();
        }

        private void Think()
        {
            ref var cmd = ref source.Command;
            switch (states.State)
            {
                case AvatarState.Seated:
                case AvatarState.Charging:
                    Navigate(ref cmd);
                    break;
                case AvatarState.Grabbing:
                    HoldOn(ref cmd);
                    break;
                case AvatarState.Falling:
                case AvatarState.Swimming:
                    SwimBack(ref cmd);
                    break;
                default:
                    Idle(ref cmd);
                    break;
            }
        }

        private static void Idle(ref Input.PlayerCommand cmd)
        {
            cmd.SeatMove = 0f;
            cmd.FartHeld = false;
            cmd.HoldHeld = false;
            cmd.Swim = Vector2.zero;
        }

        // ---------------------------------------------------------------- navegación sobre la balsa
        private void Navigate(ref Input.PlayerCommand cmd)
        {
            var raft = seat.Raft;
            if (raft == null)
            {
                Idle(ref cmd);
                return;
            }

            var balance = raft.Controller.Balance;
            var body = raft.Controller.Body;
            var position = body.worldCenterOfMass;
            var velocity = Flatten(body.velocity, false);
            float speed = velocity.magnitude;

            var heading = AvoidHazards(position, DesiredHeading(position, speed));
            // Empuje que sigue el rumbo y frena la deriva lateral.
            var lateral = velocity - heading * Vector3.Dot(velocity, heading);
            var thrust = (heading - lateral * config.lateralDamping).normalized;

            // Asiento: opuesto al empuje (el pedo empuja hacia el centro), repartido entre los bots.
            var local = raft.transform.InverseTransformDirection(thrust);
            float thrustAngle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float targetSeat = ReachableSeat(raft, thrustAngle + 180f + SeatOffset(raft, thrustAngle + 180f));
            float seatError = Mathf.DeltaAngle(seat.Angle, targetSeat);
            float step = balance.seatMoveSpeed * config.decideInterval;
            // SeatMove positivo reduce el ángulo (PlayerSeat.Update).
            cmd.SeatMove = Mathf.Abs(seatError) < SEAT_DEADZONE ? 0f : -Mathf.Clamp(seatError / step, -1f, 1f);

            // Sin puntería el empuje es radial: pasa por el centro y no hace girar la balsa. La puntería solo sirve
            // para frenar el giro que quede (Aim positivo gira a la derecha, sentido horario = velocidad angular +Y).
            cmd.Aim = Mathf.Clamp(-body.angularVelocity.y * config.yawDamping, -1f, 1f);

            bool aligned = Mathf.Abs(seatError) <= config.seatTolerance;
            Fire(ref cmd, aligned, position, velocity, heading);
            NavigateSkills(ref cmd, position, velocity, heading);
        }

        private Vector3 DesiredHeading(Vector3 position, float speed)
        {
            if (flow != null)
            {
                float look = Mathf.Clamp(speed * config.lookaheadSeconds, config.lookaheadMin, config.lookaheadMax);
                var toTarget = Flatten(flow.GetPointAhead(position, ref flowHint, look) - position, false);
                if (toTarget.sqrMagnitude > 0.25f)
                    return toTarget.normalized;
            }
            return fallbackHeading;
        }

        // Peligros a 8 m por delante: gira el rumbo hacia el lado contrario al del más cercano.
        private Vector3 AvoidHazards(Vector3 position, Vector3 heading)
        {
            int count = Physics.SphereCastNonAlloc(position, config.hazardRadius, heading, hazardHits, config.hazardRange,
                1 << GameLayers.Hazard, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hazardHits[i].distance > 0f && hazardHits[i].distance < nearestDistance)
                {
                    nearest = i;
                    nearestDistance = hazardHits[i].distance;
                }
            }
            if (nearest < 0)
                return heading;

            var toHit = Flatten(hazardHits[nearest].point - position, false);
            float side = Mathf.Sign(Vector3.Cross(heading, toHit).y); // + : el obstáculo queda a la derecha
            if (side == 0f)
                side = 1f;
            float closeness = 1f - nearestDistance / config.hazardRange;
            return Quaternion.AngleAxis(-side * config.avoidAngle * (0.5f + 0.5f * closeness), Vector3.up) * heading;
        }

        // El asiento no puede cruzar a otro (PlayerSeat/RaftSeats.ClampMove): el objetivo se recorta a lo alcanzable.
        private float ReachableSeat(RaftSeats raft, float desired)
        {
            float separation = raft.Controller.Balance.minSeatSeparation;
            float up = 180f, down = 180f;
            var seats = raft.Seats;
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] == seat)
                    continue;
                float gapUp = Mathf.Repeat(seats[i].Angle - seat.Angle, 360f);
                up = Mathf.Min(up, gapUp - separation);
                down = Mathf.Min(down, 360f - gapUp - separation);
            }
            float delta = Mathf.DeltaAngle(seat.Angle, desired);
            return seat.Angle + Mathf.Clamp(delta, -Mathf.Max(down, 0f), Mathf.Max(up, 0f));
        }

        // Los bots de una balsa se reparten simétricamente alrededor del asiento ideal, en el orden en que ya
        // están sentados (no pueden cruzarse).
        private float SeatOffset(RaftSeats raft, float rear)
        {
            float mine = Mathf.DeltaAngle(rear, seat.Angle);
            int rank = 0, total = 0;
            for (int i = 0; i < Active.Count; i++)
            {
                var other = Active[i];
                if (other.seat.Raft != raft || other.states.State > AvatarState.AbilityCast)
                    continue;
                float theirs = Mathf.DeltaAngle(rear, other.seat.Angle);
                if (theirs < mine || (theirs == mine && other.GetInstanceID() < GetInstanceID()))
                    rank++;
                total++;
            }
            return (rank - (total - 1) * 0.5f) * config.seatSpread;
        }

        // ---------------------------------------------------------------- pedos
        private void Fire(ref Input.PlayerCommand cmd, bool aligned, Vector3 position, Vector3 velocity, Vector3 heading)
        {
            float speed = velocity.magnitude;
            float frac = gas.Normalized;
            float deficit = config.cruiseSpeed - Vector3.Dot(velocity, heading);
            var current = speed > 1.5f ? velocity : Flatten(seat.Raft.transform.forward);
            bool needsCorrection = Mathf.Abs(Vector3.SignedAngle(current, heading, Vector3.up)) > config.bigHeadingError;
            bool straight = IsStraight(position, speed);
            // Una balsa muy inclinada está a punto de volcar: nadie dispara hasta que se estabilice.
            float tilt = Vector3.Angle(seat.Raft.transform.up, Vector3.up);
            if (tilt > config.maxTilt)
                tiltLocked = true;
            else if (tilt < config.maxTilt * 0.5f)
                tiltLocked = false;
            bool canFire = gas.Gas >= config.minGasToFire && !thruster.IsCoughing && !tiltLocked;

            puffTimer -= config.decideInterval;
            bool wantBlast = false;
            if (aligned && canFire)
            {
                if (blasting)
                    wantBlast = frac > config.blastStopGas && (needsCorrection || (straight && deficit > 0f));
                else
                    wantBlast = needsCorrection || (straight && deficit > 1f && frac >= config.blastStartGas);
            }
            // Pings y sincronía (BotBrain.Skills): "Espera" calla, la ráfaga de un humano se acompaña.
            bool hold = Time.time < holdFireUntil;
            if (mirrorBlast != null && !mirrorBlast.IsBlasting)
                mirrorBlast = null;
            if (hold)
                wantBlast = false;
            else if (mirrorBlast != null && gas.Gas > 0f && !thruster.IsCoughing && !tiltLocked)
                wantBlast = true;
            blasting = wantBlast;
            cmd.FartHeld = wantBlast;

            bool force = forceFire;
            forceFire = false;
            if (wantBlast)
                return;

            bool puff = aligned && canFire && deficit > 0f && puffTimer <= 0f;
            // "¡Ahora!" y la cuenta 3-2-1 piden disparar ya, bien alineado o no.
            if (force && canFire)
                puff = true;
            // Equipo detenido: nunca pasa más de idleLimit sin disparar, esté o no bien alineado (docs/10 §15).
            bool stopped = speed < config.stoppedSpeed && Time.time - lastFartTime >= config.idleLimit && gas.Gas >= 5f && !tiltLocked;
            if (hold)
                puff = stopped = false;
            if (puff || stopped)
            {
                source.PressFart();
                puffTimer = Random.Range(config.puffCadenceMin, config.puffCadenceMax);
            }
        }

        private bool IsStraight(Vector3 position, float speed)
        {
            if (flow == null)
                return true;
            float far = STRAIGHT_LOOK_NEAR + Mathf.Max(speed, config.cruiseSpeed) * STRAIGHT_LOOK_SECONDS;
            var near = flow.GetDirectionAhead(position, ref flowHint, STRAIGHT_LOOK_NEAR);
            var ahead = flow.GetDirectionAhead(position, ref flowHint, far);
            return Vector3.Angle(near, ahead) <= config.straightAngle;
        }

        // ---------------------------------------------------------------- en el agua
        private void SwimBack(ref Input.PlayerCommand cmd)
        {
            cmd.SeatMove = 0f;
            cmd.FartHeld = false;
            var home = fall.HomeRaft;
            if (home == null)
            {
                cmd.Swim = Vector2.zero;
                return;
            }

            var toRaft = Flatten(home.transform.position - transform.position, false);
            cmd.Swim = ToStick(toRaft.normalized);
            if (home.DistanceToEdge(transform.position) <= config.climbDistance)
                source.PressJump();
        }

        // La dirección del stick es relativa a la cámara (AvatarSwim.ToWorld).
        private static Vector2 ToStick(Vector3 worldDirection)
        {
            var cam = Camera.main;
            if (cam == null)
                return new Vector2(worldDirection.x, worldDirection.z);
            var forward = Flatten(cam.transform.forward);
            var right = new Vector3(forward.z, 0f, -forward.x);
            return new Vector2(Vector3.Dot(worldDirection, right), Vector3.Dot(worldDirection, forward));
        }

        private static Vector3 Flatten(Vector3 v, bool normalize = true)
        {
            v.y = 0f;
            return normalize ? v.normalized : v;
        }
    }
}
