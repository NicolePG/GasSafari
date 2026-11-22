using GasSafari.Hazards;
using UnityEngine;

namespace GasSafari.Bots
{
    // Reacción de los bots a los avisos de peligro (GameEvents.OnHazardWarning) que no son de agarrarse:
    // el cocodrilo gigante los apura y los obstáculos con radio de aviso (remolino, pinchos, red) los hacen rodear.
    public partial class BotBrain
    {
        // m de holgura lateral sobre el radio del obstáculo (media balsa y un poco más).
        private const float WARNED_CLEARANCE = 3f;

        private HazardBase warnedHazard;
        private GiantCrocChaser chaser;

        // Persecución en curso del cocodrilo gigante que avisó.
        public bool IsChased => chaser != null && chaser.Chasing;
        public HazardBase WarnedHazard => warnedHazard;

        // true si el aviso ya tiene su reacción aquí.
        private bool HearWarning(HazardBase hazard)
        {
            if (hazard is GiantCrocChaser croc)
            {
                chaser = croc;
                return true;
            }
            if (hazard.WarningRadius <= 0f)
                return false;
            var offset = hazard.WarningCenter - seat.Raft.transform.position;
            offset.y = 0f;
            if (offset.magnitude - hazard.WarningRadius > config.warningRange)
                return true;
            if (warnedHazard == null || DistanceTo(hazard) < DistanceTo(warnedHazard))
                warnedHazard = hazard;
            return true;
        }

        private float DistanceTo(HazardBase hazard)
        {
            var offset = hazard.WarningCenter - seat.Raft.transform.position;
            offset.y = 0f;
            return offset.magnitude - hazard.WarningRadius;
        }

        // Sin obstáculo sólido delante: si el rumbo pasa por el radio del peligro avisado, gira hacia el lado más corto.
        // Se olvida cuando queda atrás, se aleja o deja de avisar (red rota).
        private Vector3 AvoidWarned(Vector3 position, Vector3 heading)
        {
            if (warnedHazard == null || !warnedHazard.isActiveAndEnabled)
            {
                warnedHazard = null;
                return heading;
            }
            var toCenter = Flatten(warnedHazard.WarningCenter - position, false);
            float radius = warnedHazard.WarningRadius;
            float ahead = Vector3.Dot(toCenter, heading);
            if (radius <= 0f || ahead < -radius || toCenter.magnitude - radius > config.warningRange)
            {
                warnedHazard = null;
                return heading;
            }
            float lateral = Vector3.Cross(heading, toCenter).y; // + : el centro queda a la derecha
            float clearance = radius + WARNED_CLEARANCE;
            if (Mathf.Abs(lateral) >= clearance)
                return heading;
            float side = lateral >= 0f ? 1f : -1f;
            float closeness = 1f - Mathf.Clamp01((ahead - radius) / config.warningRange);
            return Quaternion.AngleAxis(-side * config.avoidAngle * (0.5f + 0.5f * closeness), Vector3.up) * heading;
        }
    }
}
