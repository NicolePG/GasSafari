using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Input;
using GasSafari.Level;
using GasSafari.Raft;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Bots
{
    // Sincronía con humanos, habilidades, agarrarse, Súper Pedo y pings (docs/10 §15, skill bot-ai).
    public partial class BotBrain
    {
        private const int MAX_ROAR_HITS = 32;
        // Probabilidad si no hay dificultad cargada (pruebas): la de Normal.
        private const float DEFAULT_SYNC_CHANCE = 0.75f;

        private readonly Collider[] roarHits = new Collider[MAX_ROAR_HITS];

        private AbilityRunner runner;
        private RaftSeats team;
        private SuperFartController superFart;
        private PingSystem pings;
        private TeamMeter meter;
        private float holdFireUntil;
        private float grabUntil;
        private float nextAbilityTime;
        private bool forceFire;
        private FartThruster mirrorBlast;
        private bool syncPending;
        private float syncTimer;
        private FartThruster syncFrom;
        private FartType syncType;

        private void AwakeSkills()
        {
            runner = GetComponent<AbilityRunner>();
        }

        private void OnEnableSkills()
        {
            GameEvents.OnFart += OnFartHeard;
            GameEvents.OnPing += OnPingHeard;
            GameEvents.OnHazardWarning += OnWarningHeard;
            GameEvents.OnChamberPopped += OnHit;
        }

        private void OnDisableSkills()
        {
            GameEvents.OnFart -= OnFartHeard;
            GameEvents.OnPing -= OnPingHeard;
            GameEvents.OnHazardWarning -= OnWarningHeard;
            GameEvents.OnChamberPopped -= OnHit;
            if (pings != null)
                pings.CountdownTick -= OnCountdownTick;
            team = null;
            pings = null;
        }

        // Cada frame: el pedo de sincronía se programa con retardo de 0.05-0.15 s, más fino que la decisión de 0.1 s.
        private void UpdateSkills(float dt)
        {
            if (!syncPending)
                return;
            syncTimer -= dt;
            if (syncTimer > 0f)
                return;

            syncPending = false;
            if (states.State != AvatarState.Seated || gas.Gas < config.minGasToFire || thruster.IsCoughing || tiltLocked
                || Time.time < holdFireUntil)
                return;

            if (syncType == FartType.Blast)
            {
                if (syncFrom == null || !syncFrom.IsBlasting)
                    return;
                mirrorBlast = syncFrom;
                blasting = true;
                source.Command.FartHeld = true;
            }
            else
            {
                source.PressFart();
            }
        }

        // La balsa del bot puede cambiar (reaparición): se vuelven a buscar los sistemas de equipo.
        private void FindTeam()
        {
            var raft = seat.Raft;
            if (raft == team)
                return;
            if (pings != null)
                pings.CountdownTick -= OnCountdownTick;
            team = raft;
            superFart = raft != null ? raft.GetComponent<SuperFartController>() : null;
            pings = raft != null ? raft.GetComponent<PingSystem>() : null;
            meter = raft != null ? raft.GetComponent<TeamMeter>() : null;
            if (pings != null)
                pings.CountdownTick += OnCountdownTick;
        }

        private void NavigateSkills(ref PlayerCommand cmd, Vector3 position, Vector3 velocity, Vector3 heading)
        {
            FindTeam();
            cmd.GrabHeld = Time.time < grabUntil;
            cmd.SuperHeld = WantsSuperFart(position, heading);
            TryAbility(position, velocity, heading);
        }

        // Agarrado: se mantiene hasta que pasa el aviso o el golpe y luego se suelta (docs: 0.5 s después).
        private void HoldOn(ref PlayerCommand cmd)
        {
            Idle(ref cmd);
            cmd.GrabHeld = Time.time < grabUntil;
        }

        private static bool DualControlActive()
        {
            if (!Services.IsReady || Services.Session == null)
                return false;
            var players = Services.Session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Partner != null)
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- eventos
        private void OnFartHeard(PlayerAvatar who, FartType type, float impulse)
        {
            if (who == avatar || syncPending || source == null || who.TryGetComponent<BotBrain>(out _))
                return;
            if (seat.Raft == null || who.GetComponent<PlayerSeat>().Raft != seat.Raft)
                return;
            if (states.State != AvatarState.Seated || gas.Gas < config.minGasToFire)
                return;

            var difficulty = Services.IsReady && Services.Session != null ? Services.Session.Difficulty : null;
            float chance = difficulty != null ? difficulty.botSyncChance : DEFAULT_SYNC_CHANCE;
            // Con control doble (un mando para dos animales) los bots compañeros usan la dificultad Normal.
            if (DualControlActive())
                chance = config.dualControlSyncChance;
            if (Random.value > chance)
                return;

            syncPending = true;
            syncTimer = Random.Range(config.syncDelayMin, config.syncDelayMax);
            syncType = type;
            syncFrom = who.GetComponent<FartThruster>();
        }

        // ¡Ahora! = disparar en el siguiente tick; Espera = callar waitPingTime.
        private void OnPingHeard(PlayerAvatar who, PingType type)
        {
            if (who == avatar || source == null || seat.Raft == null || who.GetComponent<PlayerSeat>().Raft != seat.Raft)
                return;
            if (type == PingType.Now)
                forceFire = true;
            else if (type == PingType.Wait)
                holdFireUntil = Time.time + config.waitPingTime;
        }

        private void OnCountdownTick(int value)
        {
            if (value == 0)
                forceFire = true;
        }

        // Fosas y murciélagos (etiqueta Fauna) avisan antes de atacar: el bot se agarra hasta que pase.
        private void OnWarningHeard(HazardBase hazard)
        {
            if (source == null || seat.Raft == null || hazard == null || hazard.Config == null)
                return;
            if (!hazard.TryGetComponent<HazardTags>(out var tags) || !tags.Has(HazardTag.Fauna))
                return;
            var offset = hazard.transform.position - seat.Raft.transform.position;
            offset.y = 0f;
            if (offset.magnitude > config.warningRange)
                return;
            grabUntil = Mathf.Max(grabUntil, Time.time + hazard.Config.warningTime + config.releaseAfter);
        }

        // Una cámara reventada es un golpe: se agarra un momento por si viene otro.
        private void OnHit(int chamber)
        {
            if (source == null || seat.Raft == null)
                return;
            grabUntil = Mathf.Max(grabUntil, Time.time + config.releaseAfter);
        }

        // ---------------------------------------------------------------- Súper Pedo
        // Pulsa apenas se abre la ventana, es decir, cuando alguien más ya pulsó (docs/10 §15). Nunca la abre solo: lanzarse
        // sin un humano que lo decida volcaba la balsa en las pruebas. Única excepción: el despegue de un abismo marcado con
        // BotHint SuperFart (W4-3, W5-2, W6-5), donde sin Súper Pedo no se llega al otro lado.
        private bool WantsSuperFart(Vector3 position, Vector3 heading)
        {
            if (superFart == null || !superFart.IsReady)
                return false;
            if (superFart.IsWindowOpen)
                return true;
            // Se mide desde la balsa (no desde el asiento) para que los 4 bots decidan igual y la ventana se llene.
            var hint = BotHint.FindAheadOrInside(seat.Raft.transform.position, heading, config.superFartHintRange, BotHintType.SuperFart);
            if (hint == null)
                return false;
            // El Súper Pedo sale hacia donde mira la balsa: solo se lanza alineada con el sentido de la marca (el del cauce).
            var raftForward = Flatten(seat.Raft.transform.forward);
            var hintForward = Flatten(hint.transform.forward);
            return Vector3.Angle(raftForward, hintForward) <= config.superFartAlignAngle;
        }

        // ---------------------------------------------------------------- habilidades
        // Un método por personaje (docs/10 §15). AbilityRunner comprueba recarga y CanActivate al recibir la pulsación.
        private void TryAbility(Vector3 position, Vector3 velocity, Vector3 heading)
        {
            if (runner == null || runner.Ability == null || !runner.Cooldown.IsReady || states.State != AvatarState.Seated)
                return;
            if (Time.time < nextAbilityTime)
                return;

            bool use;
            switch (runner.Ability.id)
            {
                case "roar": use = AlexWantsRoar(position); break;
                case "gallop": use = MartyWantsGallop(position, heading); break;
                case "neck_hook": use = MelmanWantsHook(position, velocity); break;
                case "belly_flop": use = GloriaWantsFlop(position); break;
                case "royal_party": use = JulienWantsParty(); break;
                default: use = false; break;
            }
            if (!use)
                return;

            source.PressAbility();
            nextAbilityTime = Time.time + config.abilityRetry;
        }

        // Alex ruge si hay algo Fragile o Fauna en el cono.
        private bool AlexWantsRoar(Vector3 position)
        {
            if (!(runner.Ability is RoarAbility roar))
                return false;
            var forward = Flatten(seat.Raft.transform.forward);
            int mask = (1 << GameLayers.Hazard) | (1 << GameLayers.Environment);
            int count = Physics.OverlapSphereNonAlloc(position, roar.range, roarHits, mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var tags = roarHits[i].GetComponentInParent<HazardTags>();
                if (tags == null || !(tags.Has(HazardTag.Fragile) || tags.Has(HazardTag.Fauna)))
                    continue;
                if (roar.InCone(position, forward, roarHits[i].bounds.ClosestPoint(position)))
                    return true;
            }
            return false;
        }

        // Marty galopa antes de rampas y abismos.
        private bool MartyWantsGallop(Vector3 position, Vector3 heading)
        {
            return BotHint.IsAhead(position, heading, config.hintRange, BotHintType.Ramp)
                || BotHint.IsAhead(position, heading, config.hintRange, BotHintType.Drop);
        }

        // Melman rescata a un caído a su alcance o se engancha a un ancla en una curva cerrada.
        private bool MelmanWantsHook(Vector3 position, Vector3 velocity)
        {
            if (!(runner.Ability is NeckHookAbility hook))
                return false;
            var from = seat.WorldPoint;
            var crew = team.Crew;
            for (int i = 0; i < crew.Count; i++)
            {
                var fall = crew[i].GetComponent<AvatarFall>();
                if (fall.IsOverboard && (fall.transform.position - from).sqrMagnitude <= hook.range * hook.range)
                    return true;
            }
            return Anchor.FindNearest(from, hook.range) != null && CurveAngle(position, velocity.magnitude) >= config.sharpCurveAngle;
        }

        // Gloria hace Panzazo si la balsa gira más de 90°/s o está en un remolino.
        private bool GloriaWantsFlop(Vector3 position)
        {
            float spin = Mathf.Abs(seat.Raft.Controller.Body.angularVelocity.y) * Mathf.Rad2Deg;
            return spin > config.spinForBellyFlop || BotHint.IsInside(position, BotHintType.Whirlpool);
        }

        // Julien activa la Fiesta con 2 o más jugadores y el medidor por debajo de 80.
        private bool JulienWantsParty()
        {
            return team.Seats.Count >= 2 && meter != null && meter.Value < config.partyMeterBelow;
        }

        // Ángulo entre el rumbo del cauce cerca y lejos (la misma medida que IsStraight).
        private float CurveAngle(Vector3 position, float speed)
        {
            if (flow == null)
                return 0f;
            float far = STRAIGHT_LOOK_NEAR + Mathf.Max(speed, config.cruiseSpeed) * STRAIGHT_LOOK_SECONDS;
            var near = flow.GetDirectionAhead(position, ref flowHint, STRAIGHT_LOOK_NEAR);
            var ahead = flow.GetDirectionAhead(position, ref flowHint, far);
            return Vector3.Angle(near, ahead);
        }
    }
}
