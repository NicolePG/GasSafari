using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H15 Cocodrilo (docs/05): duerme hasta que un pedo fuerte (ráfaga, cargado o Súper Pedo; los toques no) suena a menos de 8 m.
    // Despierto se acerca a la balsa, avisa 'warningTime' y muerde: revienta 1 cámara. Después se aleja 'retreatTime' y
    // vuelve a dormir. Se supera con toques suaves, el Rugido o la cebolla (IScareable: huye y no ataca).
    public class Crocodile : HazardBase, IScareable
    {
        public enum State : byte { Sleeping, Chasing, Biting, Retreating }

        private RaftController raft;
        private Vector3 home;
        private Quaternion homeRotation;
        private float stateTimer;
        private float scaredUntil = -1f;

        public State Current { get; private set; }
        public float WakeDistance => Config.Get("wakeDistance", 8f);
        public float Speed => Config.Get("speed", 3f);
        public float BiteDistance => Config.Get("biteDistance", 2.5f);
        public float RetreatTime => Config.Get("retreatTime", 4f);

        private void Awake()
        {
            home = transform.position;
            homeRotation = transform.rotation;
        }

        private void OnEnable()
        {
            GameEvents.OnFart += OnFart;
            GameEvents.OnSuperFart += OnSuperFart;
            GameEvents.OnRespawn += Reset;
        }

        private void OnDisable()
        {
            GameEvents.OnFart -= OnFart;
            GameEvents.OnSuperFart -= OnSuperFart;
            GameEvents.OnRespawn -= Reset;
        }

        private void Reset()
        {
            Current = State.Sleeping;
            transform.SetPositionAndRotation(home, homeRotation);
            scaredUntil = -1f;
            CancelWarning();
        }

        public void Scare(float duration, Vector3 source)
        {
            scaredUntil = Time.time + duration;
            CancelWarning();
            Current = State.Retreating;
            stateTimer = duration;
        }

        // Despierta ya (estampida de W3-4): persigue a la balsa aunque nadie haya soplado.
        public void Wake(RaftController target)
        {
            if (Current != State.Sleeping || Time.time < scaredUntil)
                return;
            raft = target;
            Current = State.Chasing;
        }

        // Un pedo fuerte cerca despierta al cocodrilo.
        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            if (Current != State.Sleeping || type == FartType.Puff || Time.time < scaredUntil)
                return;
            if ((avatar.transform.position - transform.position).magnitude > WakeDistance)
                return;
            raft = avatar.GetComponentInParent<RaftController>();
            if (raft == null)
                raft = FindFirstObjectByType<RaftController>();
            Current = State.Chasing;
        }

        // El Súper Pedo es el más fuerte de todos: también lo despierta si la balsa está a menos de 8 m.
        private void OnSuperFart(float power)
        {
            if (Current != State.Sleeping || Time.time < scaredUntil)
                return;
            var target = FindFirstObjectByType<RaftController>();
            if (target == null || (target.transform.position - transform.position).magnitude > WakeDistance)
                return;
            raft = target;
            Current = State.Chasing;
        }

        protected override void Update()
        {
            base.Update();
            if (Current == State.Sleeping || raft == null)
                return;

            var flat = raft.transform.position - transform.position;
            flat.y = 0f;
            switch (Current)
            {
                case State.Chasing:
                    if (DistanceToRaft() <= BiteDistance)
                    {
                        Current = State.Biting;
                        Warn();
                        break;
                    }
                    Move(flat.normalized, Speed);
                    break;
                case State.Biting:
                    // Sigue a la balsa mientras avisa; el mordisco llega en OnWarningEnded.
                    if (DistanceToRaft() > BiteDistance * 1.5f)
                    {
                        CancelWarning();
                        Current = State.Chasing;
                    }
                    break;
                case State.Retreating:
                    stateTimer -= Time.deltaTime;
                    var back = home - transform.position;
                    back.y = 0f;
                    if (back.magnitude > 0.3f)
                        Move(back.normalized, Speed);
                    if (stateTimer <= 0f && back.magnitude <= 0.5f)
                        Current = State.Sleeping;
                    break;
            }
        }

        // Distancia horizontal al borde de la balsa (no a su centro): el collider del cocodrilo le impide acercarse más que
        // la suma de los dos tamaños, y el mordisco tiene que alcanzar desde ahí.
        private float DistanceToRaft()
        {
            var closest = raft.Body.ClosestPointOnBounds(transform.position);
            closest.y = transform.position.y;
            return Vector3.Distance(transform.position, closest);
        }

        private void Move(Vector3 direction, float speed)
        {
            transform.position += direction * (speed * HazardSpeed * Time.deltaTime);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 6f * Time.deltaTime);
        }

        protected override void OnWarningEnded()
        {
            if (Current != State.Biting || raft == null || Time.time < scaredUntil)
                return;
            if (DistanceToRaft() <= BiteDistance * 1.5f)
            {
                var point = raft.Body.ClosestPointOnBounds(transform.position);
                DamageRaft(raft, point);
            }
            Current = State.Retreating;
            stateTimer = RetreatTime;
        }
    }
}
