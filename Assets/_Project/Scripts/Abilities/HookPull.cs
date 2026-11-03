using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Efecto temporal del Cuello Gancho: lleva al caído hasta la balsa en 'duration' segundos.
    public class HookPull : MonoBehaviour
    {
        private AvatarFall fall;
        private PlayerAvatar rescuer;
        private Rigidbody body;
        private Vector3 start;
        private float duration;
        private float elapsed;

        public static void Begin(AvatarFall target, PlayerAvatar rescuer, float duration)
        {
            var pull = target.gameObject.AddComponent<HookPull>();
            pull.fall = target;
            pull.rescuer = rescuer;
            pull.duration = duration;
            pull.body = target.GetComponent<Rigidbody>();
            pull.start = target.transform.position;
            pull.body.isKinematic = true;
            target.GetComponent<AvatarStateMachine>().Enter(AvatarState.Climbing);
        }

        private void FixedUpdate()
        {
            if (!fall.IsOverboard)
            {
                // Ya volvió por otra vía (reaparición).
                Destroy(this);
                return;
            }

            elapsed += Time.fixedDeltaTime;
            var raft = fall.HomeRaft;
            float angle = raft.AngleTowards(start);
            var target = raft.transform.TransformPoint(raft.LocalSeatPosition(angle));
            body.MovePosition(Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            if (elapsed < duration)
                return;

            fall.Board(angle);
            GameEvents.RaisePlayerRescued(fall.GetComponent<PlayerAvatar>(), rescuer);
            Destroy(this);
        }
    }
}
