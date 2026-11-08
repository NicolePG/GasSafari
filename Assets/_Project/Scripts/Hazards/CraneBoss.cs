using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H24 Grúa con gancho (jefe de W1-5, docs/05): el gancho cae donde marca la sombra roja, con aviso de 1 s y un
    // ataque cada 2-4 s. La grúa (Crane.fbx, +Z local hacia el cauce) corre por el muelle siguiendo a la balsa: 'root'
    // se desliza por su eje X (el del cauce) y el carro ('trolley') por su eje Z (alcance lateral). El gancho baja desde su
    // posición de reposo hasta el agua; el cable es la malla skineada entre carro y gancho, se estira solo.
    // Secuencia determinista (lista de intervalos) que se reinicia al reaparecer en un checkpoint.
    public class CraneBoss : HazardBase
    {
        private enum Phase { Idle, Tracking, Warning, Striking, Retracting }

        // Intervalos entre ataques en s (docs/05: cada 2-4 s); se repiten en bucle.
        private static readonly float[] Pattern = { 3f, 2f, 4f, 2.5f, 2f, 3.5f };
        private const float STRIKE_TIME = 0.15f;
        private const float RETRACT_TIME = 0.7f;

        [SerializeField] private Transform root;
        [SerializeField] private Transform trolley;
        [SerializeField] private Transform hook;
        [SerializeField] private Animator animator;
        [Tooltip("m, alcance lateral extra del carro a cada lado")] [SerializeField] private float reachRange = 3f;
        [Tooltip("m del agua a la que baja el gancho (y de la sombra)")] [SerializeField] private float waterY;
        [SerializeField] private Material shadowMaterial;

        private RaftController raft;
        private Transform shadow;
        private Phase phase = Phase.Idle;
        private float phaseTimer;
        private int patternIndex;
        private int strikesLeft;
        private bool active;
        private float intervalOverride;
        private Vector3 target;
        private Vector3 hookRest;
        private Vector3 trolleyRest;
        private Vector3 rootRest;
        private Vector3 hookOffset;
        private float reachRest;
        private Vector3 hookFrom;

        public bool Active => active;
        public int Strikes { get; private set; }
        public Vector3 Target => target;
        public float Radius => Config.Get("radius", 3f);
        public float TrackSpeed => Config.Get("trackSpeed", 14f);
        public float Impulse => Config.Get("impulse", 35f);
        public bool IsIdle => phase == Phase.Idle;
        public bool IsWarningPhase => phase == Phase.Warning;

        // Intervalo del ataque n (pura: pruebas).
        public static float IntervalAt(int index) => Pattern[index % Pattern.Length];

        public void Configure(Transform craneRoot, Transform trolleyBone, Transform hookBone, Material shadow)
        {
            root = craneRoot;
            trolley = trolleyBone;
            hook = hookBone;
            shadowMaterial = shadow;
        }

        public void SetRaft(RaftController value) => raft = value;

        private void Awake()
        {
            if (root == null) root = transform;
            rootRest = root.position;
            if (hook != null) hookRest = hook.position;
            if (trolley != null)
            {
                trolleyRest = trolley.position;
                reachRest = Vector3.Dot(trolleyRest - rootRest, root.forward);
            }
            if (hook != null && trolley != null) hookOffset = hookRest - trolleyRest;
            // Sin Animator el gancho y el carro se quedan donde los dejo: la grúa se maneja por código.
            if (animator != null)
                animator.enabled = false;
            BuildShadow();
        }

        private void OnEnable() => GameEvents.OnRespawn += ResetSequence;
        private void OnDisable() => GameEvents.OnRespawn -= ResetSequence;

        // El nivel arranca los ataques: count <= 0 = sin límite. 'seconds' > 0 fija un intervalo (sprint final: 2 s).
        public void StartAttacks(int count = 0, float seconds = 0f)
        {
            active = true;
            strikesLeft = count;
            intervalOverride = seconds;
            EnterIdle();
        }

        // Un golpe dirigido a un punto (momento estrella de W1-5: el gancho cae detrás de la balsa sobre un contenedor).
        public void StrikeAt(Vector3 point)
        {
            active = true;
            strikesLeft = 1;
            intervalOverride = 0f;
            shadow.gameObject.SetActive(false);
            target = ClampTarget(point, true);
            phaseTimer = Config.warningTime + 0.2f;
            phase = Phase.Tracking;
        }

        public void StopAttacks()
        {
            active = false;
            EnterIdle();
        }

        // Reaparición en un checkpoint: las fases se reinician.
        public void ResetSequence()
        {
            CancelWarning();
            patternIndex = 0;
            Strikes = 0;
            ReturnToRest();
            EnterIdle();
        }

        private void EnterIdle()
        {
            phase = Phase.Idle;
            phaseTimer = NextInterval();
            shadow.gameObject.SetActive(false);
        }

        private float NextInterval() => intervalOverride > 0f ? intervalOverride : IntervalAt(patternIndex);

        protected override void Update()
        {
            base.Update();
            if (!active)
                return;
            if (raft == null)
                raft = FindObjectOfType<RaftController>();

            float dt = Time.deltaTime * HazardSpeed;
            switch (phase)
            {
                case Phase.Idle:
                    phaseTimer -= dt;
                    // Empieza a seguir a la balsa para llegar con el aviso completo.
                    if (phaseTimer <= Config.warningTime + 0.2f && raft != null)
                    {
                        // Dónde estará la balsa cuando caiga el gancho.
                        var body = raft.Body;
                        target = ClampTarget(body.position + body.velocity * (phaseTimer + Config.warningTime), true);
                        phase = Phase.Tracking;
                    }
                    break;

                case Phase.Tracking:
                    phaseTimer -= dt;
                    if (MoveToTarget(dt) || phaseTimer <= 0f)
                    {
                        phase = Phase.Warning;
                        shadow.position = new Vector3(target.x, waterY + 0.05f, target.z);
                        shadow.gameObject.SetActive(true);
                        Warn();
                    }
                    break;

                case Phase.Warning:
                    // La sombra crece hasta el radio del golpe.
                    float diameter = Radius * 2f * Mathf.Max(0.2f, WarningProgress);
                    shadow.localScale = new Vector3(diameter, 0.01f, diameter);
                    break;

                case Phase.Striking:
                    phaseTimer -= Time.deltaTime;
                    float k = 1f - Mathf.Clamp01(phaseTimer / STRIKE_TIME);
                    SetHook(Vector3.Lerp(hookFrom, new Vector3(target.x, waterY + 0.3f, target.z), k * k));
                    if (phaseTimer <= 0f)
                        Impact();
                    break;

                case Phase.Retracting:
                    phaseTimer -= Time.deltaTime;
                    float r = 1f - Mathf.Clamp01(phaseTimer / RETRACT_TIME);
                    SetHook(Vector3.Lerp(hookFrom, trolley.position + hookOffset, r));
                    if (phaseTimer <= 0f)
                    {
                        phase = Phase.Idle;
                        patternIndex++;
                        if (strikesLeft > 0 && --strikesLeft == 0)
                            active = false;
                        phaseTimer = NextInterval();
                    }
                    break;
            }
        }

        protected override void OnWarn()
        {
            if (Services.IsReady)
                Services.Audio.Play(SoundId.CraneWarn, new Vector3(target.x, waterY, target.z));
        }

        protected override void OnWarningEnded()
        {
            if (phase != Phase.Warning)
                return;
            phase = Phase.Striking;
            phaseTimer = STRIKE_TIME;
            hookFrom = hook != null ? hook.position : target;
        }

        private void Impact()
        {
            var point = new Vector3(target.x, waterY, target.z);
            Strikes++;
            shadow.gameObject.SetActive(false);
            if (Services.IsReady)
                Services.Audio.Play(SoundId.CraneSlam, point);

            // Contenedores y cajas de la zona (momento estrella de W1-5), balsa y animales.
            foreach (var hit in Physics.OverlapSphere(point, Radius))
            {
                if (hit.TryGetComponent<IBreakable>(out var breakable) || hit.GetComponentInParent<IBreakable>() != null)
                    (breakable ?? hit.GetComponentInParent<IBreakable>()).Break(point);
            }
            if (raft != null)
            {
                var flat = new Vector3(raft.Body.position.x - point.x, 0f, raft.Body.position.z - point.z);
                if (flat.magnitude <= Radius)
                {
                    DamageRaft(raft, raft.Body.position);
                    var away = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;
                    PushRaft(raft, away * Impulse, raft.Body.worldCenterOfMass);
                }
            }
            phase = Phase.Retracting;
            phaseTimer = RETRACT_TIME;
            hookFrom = hook != null ? hook.position : point;
        }

        // Desliza la grúa (cauce) y el carro (alcance) hasta poner el gancho sobre 'target'. true = ya llegó.
        private bool MoveToTarget(float dt)
        {
            if (hook == null || trolley == null)
                return true;
            var hookNow = hook.position;
            // Eje del cauce = X local de la grúa; alcance lateral = Z local. El carro y el gancho son hijos de la grúa.
            float alongError = Vector3.Dot(target - hookNow, root.right);
            float reachError = Vector3.Dot(target - hookNow, root.forward);
            float step = TrackSpeed * dt;
            float along = Mathf.Clamp(alongError, -step, step);
            root.position += root.right * along;

            float current = Vector3.Dot(trolley.position - root.position, root.forward) - reachRest;
            float wanted = Mathf.Clamp(current + reachError, -reachRange, reachRange);
            float reach = Mathf.Clamp(wanted - current, -step, step);
            trolley.position += root.forward * reach;

            return Mathf.Abs(alongError - along) < 0.05f && Mathf.Abs(wanted - current - reach) < 0.05f;
        }

        // El carro no llega más allá de su alcance lateral: la sombra y el golpe caen donde llega.
        private Vector3 ClampTarget(Vector3 point, bool snapToWater)
        {
            if (snapToWater)
                point.y = waterY;
            if (hook == null)
                return point;
            float lateral = Vector3.Dot(point - hookRest, root.forward);
            float clamped = Mathf.Clamp(lateral, -reachRange, reachRange);
            return point + root.forward * (clamped - lateral);
        }

        private void SetHook(Vector3 position)
        {
            if (hook != null)
                hook.position = position;
        }

        private void ReturnToRest()
        {
            if (root != null) root.position = rootRest;
            if (trolley != null) trolley.position = trolleyRest;
            if (hook != null) hook.position = trolleyRest + hookOffset;
        }

        private void BuildShadow()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "HookShadow";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(null);
            go.transform.localScale = new Vector3(1f, 0.01f, 1f);
            if (shadowMaterial != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = shadowMaterial;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            shadow = go.transform;
        }

        private void OnDestroy()
        {
            if (shadow != null)
                Destroy(shadow.gameObject);
        }
    }
}
