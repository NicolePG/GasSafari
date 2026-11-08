using System.Collections.Generic;
using Cinemachine;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.VFX
{
    // Cámara del nivel (docs/02 §12): Framing Transposer sobre un TargetGroup con la balsa y los caídos.
    // El yaw de la cámara es el del rig; la inclinación y las distancias salen de BalanceConfig.
    [RequireComponent(typeof(CinemachineTargetGroup), typeof(CinemachineImpulseSource))]
    public class RaftCameraRig : MonoBehaviour
    {
        // Distancia caído-balsa hasta la que no se abre la cámara y tramo siguiente que sí la abre (m).
        private const float SWIMMER_SPREAD_FREE = 6f;
        private const float SWIMMER_SPREAD_MAX = 12f;
        private const float COLLIDER_MIN_DISTANCE = 2.5f;
        private const float COLLIDER_RADIUS = 0.3f;

        [SerializeField] private BalanceConfig balance;
        [SerializeField] private CinemachineVirtualCamera vcam;
        [SerializeField] private RaftController raft;

        private readonly List<AvatarFall> swimmers = new List<AvatarFall>();
        private CinemachineTargetGroup group;
        private CinemachineImpulseSource impulse;
        private CinemachineFramingTransposer body;
        private float distanceVelocity;

        public float Distance { get; private set; }
        public int TrackedSwimmers => swimmers.Count;
        public CinemachineTargetGroup Group => group;

        private void Awake()
        {
            group = GetComponent<CinemachineTargetGroup>();
            impulse = GetComponent<CinemachineImpulseSource>();

            vcam.DestroyCinemachineComponent<CinemachineComposer>();
            body = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (body == null)
                body = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            body.m_GroupFramingMode = CinemachineFramingTransposer.FramingMode.None;
            body.m_LookaheadTime = balance.cameraLookahead;

            vcam.Follow = group.transform;
            ConfigureCollision();
            vcam.transform.rotation = Quaternion.Euler(balance.cameraTilt, transform.eulerAngles.y, 0f);
            Distance = balance.cameraDistanceMin;
            body.m_CameraDistance = Distance;

            // Icono en el borde de pantalla de los caídos que salgan de cámara.
            if (GetComponent<UI.SwimmerEdgeMarkers>() == null)
                gameObject.AddComponent<UI.SwimmerEdgeMarkers>();
        }

        // La cámara no atraviesa paredes ni techos (docs/11, "nada se traspasa"): si una pared o un techo (capa Environment)
        // queda entre ella y la balsa, se adelanta hasta quedar libre, sin acercarse a menos de COLLIDER_MIN_DISTANCE.
        private void ConfigureCollision()
        {
            if (vcam.LookAt == null)
                vcam.LookAt = group.transform; // sin Aim no gira la cámara: solo le da al Collider el punto que protege
            var collider = vcam.GetComponent<CinemachineCollider>();
            if (collider == null)
                collider = vcam.gameObject.AddComponent<CinemachineCollider>();
            collider.m_AvoidObstacles = true;
            collider.m_CollideAgainst = 1 << GameLayers.Environment;
            collider.m_TransparentLayers = 0;
            collider.m_MinimumDistanceFromTarget = COLLIDER_MIN_DISTANCE;
            collider.m_CameraRadius = COLLIDER_RADIUS;
            collider.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
            collider.m_MaximumEffort = 4;
            collider.m_Damping = 0.4f;
            collider.m_DampingWhenOccluded = 0.05f;
            collider.m_DistanceLimit = 0f;
        }

        private void Start()
        {
            if (raft == null)
                raft = FindFirstObjectByType<RaftController>();
            Bind(raft);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerFell += Track;
            GameEvents.OnChamberPopped += OnChamberPopped;
            GameEvents.OnSuperFart += OnSuperFart;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerFell -= Track;
            GameEvents.OnChamberPopped -= OnChamberPopped;
            GameEvents.OnSuperFart -= OnSuperFart;
        }

        public void Bind(RaftController levelRaft)
        {
            if (raft != null && group.FindMember(raft.transform) >= 0)
                group.RemoveMember(raft.transform);
            raft = levelRaft;
            group.AddMember(raft.transform, balance.cameraRaftWeight, balance.raftRadius);
        }

        public void Shake(float force)
        {
            if (Services.Session.ScreenShake)
                impulse.GenerateImpulseWithForce(force);
        }

        private void Track(PlayerAvatar avatar)
        {
            var fall = avatar.GetComponent<AvatarFall>();
            if (swimmers.Contains(fall))
                return;
            swimmers.Add(fall);
            group.AddMember(avatar.transform, balance.cameraSwimmerWeight, avatar.Character.height * 0.5f);
        }

        private void LateUpdate()
        {
            for (int i = swimmers.Count - 1; i >= 0; i--)
            {
                if (swimmers[i] != null && swimmers[i].IsOverboard)
                    continue;
                if (swimmers[i] != null)
                    group.RemoveMember(swimmers[i].transform);
                swimmers.RemoveAt(i);
            }

            float target = swimmers.Count > 0 ? balance.cameraDistanceMax : balance.cameraDistanceMin;
            // Cuanto más lejos queda el caído de la balsa, más se abre la cámara para encuadrarlo (hasta +6 m).
            float spread = 0f;
            for (int i = 0; i < swimmers.Count; i++)
            {
                var offset = swimmers[i].transform.position - raft.transform.position;
                offset.y = 0f;
                spread = Mathf.Max(spread, offset.magnitude);
            }
            target += Mathf.Clamp(spread - SWIMMER_SPREAD_FREE, 0f, SWIMMER_SPREAD_MAX) * 0.5f;
            var velocity = raft.Body.velocity;
            velocity.y = 0f;
            target += Mathf.Clamp01(velocity.magnitude / balance.cameraZoomSpeedReference) * balance.cameraSpeedZoom;

            Distance = Mathf.SmoothDamp(Distance, target, ref distanceVelocity, balance.cameraZoomSmoothTime);
            body.m_CameraDistance = Distance;
        }

        private void OnChamberPopped(int chamber) => Shake(balance.shakeMedium);
        private void OnSuperFart(float power) => Shake(balance.shakeStrong);
    }
}
