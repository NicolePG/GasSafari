using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H23 Cocodrilo gigante (jefe de W5, docs/05): al entrar la balsa en su zona de salida, persigue por 'path' (el cauce) a
    // 'speedFactor' (90 %) de la velocidad media del equipo (media móvil de 4 s, entre 'minSpeed' y 'maxSpeed'). Si alcanza la
    // balsa (a menos de 'catchDistance' m), reaparición en el checkpoint. El Rugido (IScareable) lo frena 'duration' s (2).
    // En el final, Snap() lo pone detrás de la balsa y cierra la boca (momento estrella).
    [RequireComponent(typeof(BoxCollider))]
    public class GiantCrocChaser : HazardBase, IScareable
    {
        private const float AVERAGE_SECONDS = 4f;

        [SerializeField] private Transform body;
        [SerializeField] private Vector3[] path;
        [SerializeField] private float speedFactor = 0.9f;
        [SerializeField] private float minSpeed = 3f;
        [SerializeField] private float maxSpeed = 16f;
        [SerializeField] private float catchDistance = 5f;

        private RaftController raft;
        private Animator animator;
        private int index;
        private float averageSpeed;
        private float scaredUntil = -1f;
        private bool snapped;

        public bool Chasing { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float AverageRaftSpeed => averageSpeed;
        public bool Scared => Time.time < scaredUntil;
        public int Catches { get; private set; }

        public void Configure(Transform crocBody, Vector3[] waypoints, float factor = 0.9f, float catchRadius = 5f)
        {
            body = crocBody;
            path = waypoints;
            speedFactor = factor;
            catchDistance = catchRadius;
        }

        private void Awake()
        {
            if (body != null)
                animator = body.GetComponentInChildren<Animator>();
            ResetChase();
        }

        private void OnEnable() => GameEvents.OnRespawn += OnRespawned;
        private void OnDisable() => GameEvents.OnRespawn -= OnRespawned;

        // Al reaparecer en un checkpoint que ya está pasada la zona de salida, la persecución sigue desde ~28 m detrás de la balsa
        // (no vuelve a cruzar la zona que la arranca).
        private void OnRespawned()
        {
            var level = LevelManager.Current;
            ResetChase();
            if (level == null || level.Raft == null || path == null || path.Length == 0 || body == null)
                return;
            var position = level.Raft.transform.position;
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < path.Length; i++)
            {
                float d = (path[i] - position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = i;
                }
            }
            if (nearest < 4 || best > 400f)
                return;
            raft = level.Raft;
            index = Mathf.Max(0, nearest - 3);
            body.position = path[index];
            Chasing = true;
            averageSpeed = FlatSpeed(raft);
            Trigger("Swim");
        }

        private void ResetChase()
        {
            Chasing = false;
            snapped = false;
            index = 0;
            averageSpeed = 0f;
            scaredUntil = -1f;
            CurrentSpeed = 0f;
            if (body != null && path != null && path.Length > 0)
                body.position = path[0];
            Trigger("Idle");
        }

        protected override void OnRaftEnter(RaftController hit)
        {
            if (Chasing || snapped)
                return;
            raft = hit;
            Chasing = true;
            averageSpeed = FlatSpeed(hit);
            Trigger("Swim");
            if (Services.IsReady && Services.Audio != null)
                Services.Audio.Play(Audio.SoundId.RockWarn, body != null ? body.position : transform.position);
        }

        // Rugido de Alex (u otra fuente): lo frena.
        public void Scare(float duration, Vector3 source)
        {
            scaredUntil = Time.time + duration;
        }

        protected override void Update()
        {
            base.Update();
            if (!Chasing || raft == null || body == null || path == null || path.Length == 0)
                return;

            // Media móvil de la velocidad de la balsa.
            float k = 1f - Mathf.Exp(-Time.deltaTime / AVERAGE_SECONDS);
            averageSpeed = Mathf.Lerp(averageSpeed, FlatSpeed(raft), k);
            CurrentSpeed = Scared ? 0f : Mathf.Clamp(averageSpeed * speedFactor, minSpeed, maxSpeed) * HazardSpeed;

            if (index < path.Length - 1)
            {
                var target = path[index + 1];
                var before = body.position;
                body.position = Vector3.MoveTowards(body.position, target, CurrentSpeed * Time.deltaTime);
                var heading = body.position - before;
                heading.y = 0f;
                if (heading.sqrMagnitude > 1e-6f)
                    body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(heading), 6f * Time.deltaTime);
                if ((body.position - target).sqrMagnitude < 0.04f)
                    index++;
            }

            if (Vector3.Distance(body.position, raft.transform.position) <= catchDistance)
            {
                Catches++;
                Chasing = false;
                Trigger("Bite");
                var checkpoints = FindFirstObjectByType<CheckpointSystem>();
                if (checkpoints != null)
                    checkpoints.RequestRespawn();
            }
        }

        // Momento estrella (final): se coloca justo detrás de la balsa y cierra la boca.
        public void Snap(RaftController target)
        {
            if (snapped || body == null)
                return;
            snapped = true;
            Chasing = false;
            var behind = -target.transform.forward;
            behind.y = 0f;
            var snapPosition = target.transform.position + behind.normalized * (catchDistance + 3f);
            snapPosition.y = target.transform.position.y - 0.3f; // sobre el agua, no flotando
            body.position = snapPosition;
            body.rotation = Quaternion.LookRotation(-behind.normalized);
            Trigger("Bite");
        }

        private static float FlatSpeed(RaftController r)
        {
            var v = r.Body.velocity;
            return new Vector2(v.x, v.z).magnitude;
        }

        private void Trigger(string name)
        {
            if (animator == null)
                return;
            foreach (var p in animator.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(name);
                    return;
                }
            }
        }
    }
}
