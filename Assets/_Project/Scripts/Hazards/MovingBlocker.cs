using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H05 Bloqueador móvil (docs/05): contenedor o plataforma que se desliza a 2 m/s y golpea con 30. Etiqueta Heavy
    // (el Terremoto lo rompe). Va y viene entre su posición inicial y 'travel' (local); se detiene 'dwell' s en cada
    // extremo y avisa warningTime antes de arrancar. Movimiento = función del reloj: se puede aprender el patrón.
    [RequireComponent(typeof(Rigidbody))]
    public class MovingBlocker : HazardBase, IBreakable
    {
        [Tooltip("Desplazamiento local desde la posición inicial")] [SerializeField] private Vector3 travel = new Vector3(0f, 0f, 8f);
        [Tooltip("s de desfase")] [SerializeField] private float phase;

        private Rigidbody body;
        private Vector3 origin;
        private Quaternion originRotation;
        private float clock;
        private float lastProgress;
        private bool warnedThisStop;

        public float Length => travel.magnitude;
        public float Speed => Config.Get("speed", 2f);
        public float Dwell => Config.Get("dwell", 1f);
        public float Hit => Config.Get("hit", 30f);
        public bool Broken { get; private set; }
        public float Progress { get; private set; }
        // -1, 0 o 1: sentido del movimiento (0 = parado en un extremo).
        public int Direction { get; private set; }

        public void Configure(Vector3 localTravel, float seconds)
        {
            travel = localTravel;
            phase = seconds;
        }

        // Posición 0..1 en el recorrido de ida y vuelta con paradas, y el sentido (pura: pruebas).
        public static float Evaluate(float time, float length, float speed, float dwell, out int direction, out float secondsToMove)
        {
            float move = length / Mathf.Max(speed, 0.01f);
            float cycle = 2f * (dwell + move);
            float t = Mathf.Repeat(time, cycle);
            secondsToMove = 0f;
            if (t < dwell)
            {
                direction = 0;
                secondsToMove = dwell - t;
                return 0f;
            }
            t -= dwell;
            if (t < move)
            {
                direction = 1;
                return t / move;
            }
            t -= move;
            if (t < dwell)
            {
                direction = 0;
                secondsToMove = dwell - t;
                return 1f;
            }
            t -= dwell;
            direction = -1;
            return 1f - t / move;
        }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            origin = transform.position;
            originRotation = transform.rotation;
        }

        protected override void Update()
        {
            base.Update();
            if (Broken)
                return;
            clock += Time.deltaTime * HazardSpeed;
            lastProgress = Progress;
            Progress = Evaluate(clock + phase, Length, Speed, Dwell, out int direction, out float secondsToMove);
            Direction = direction;

            // Aviso antes de arrancar (si la parada es lo bastante larga).
            if (direction == 0 && Dwell >= Config.warningTime)
            {
                if (!warnedThisStop && secondsToMove <= Config.warningTime)
                {
                    warnedThisStop = true;
                    Warn();
                }
            }
            else if (direction != 0)
            {
                warnedThisStop = false;
            }
            OnProgress(lastProgress, Progress);
        }

        protected virtual void OnProgress(float previous, float current) { }

        private void FixedUpdate()
        {
            if (Broken)
                return;
            body.MovePosition(origin + originRotation * travel * Progress);
        }

        // Golpe: en el sentido del movimiento si va en marcha, si no hacia afuera del bloqueador.
        protected override void OnRaftCollision(RaftController raft, Collision collision)
        {
            if (Broken)
                return;
            var away = raft.transform.position - transform.position;
            away.y = 0f;
            var direction = away.sqrMagnitude > 0.001f ? away.normalized : transform.forward;
            if (Direction != 0)
            {
                var along = originRotation * travel.normalized * Direction;
                along.y = 0f;
                direction = (direction + along.normalized).normalized;
            }
            var point = collision.contactCount > 0 ? collision.GetContact(0).point : raft.Body.worldCenterOfMass;
            PushRaft(raft, direction * Hit, point);
        }

        // Terremoto de Gloria (etiqueta Heavy).
        public void Break(Vector3 source)
        {
            Broken = true;
            gameObject.SetActive(false);
        }

        // Reaparición en un checkpoint: vuelve a empezar su patrón.
        public void ResetPattern()
        {
            Broken = false;
            clock = 0f;
            Progress = 0f;
            warnedThisStop = false;
            gameObject.SetActive(true);
        }
    }
}
