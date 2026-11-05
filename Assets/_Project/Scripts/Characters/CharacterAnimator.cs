using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Characters
{
    // Poses fuera de la partida que se piden desde la interfaz (parámetro Pose del Animator).
    public enum AnimPose : byte
    {
        None,
        Disappointed,
        Victory,
        Select
    }

    // Traduce el estado del animal a parámetros del Animator (docs/10 §6). Parámetros de CharacterBase.controller:
    // State (int, AvatarState), Seat (float -1..1), Charge (float 0..1), Blast (bool), Pose (int, AnimPose),
    // triggers Puff, Mega, Cough, Ability, Hit y Celebrate. Los toques y megapedos se estiran además por código (FartJuice).
    [RequireComponent(typeof(AvatarStateMachine), typeof(FartThruster), typeof(FartCharger))]
    public class CharacterAnimator : MonoBehaviour
    {
        // Suavizado del desplazamiento por el borde (s): evita saltos entre Shuffle_L, Idle_Sit y Shuffle_R.
        private const float SEAT_SMOOTH = 0.08f;

        private static readonly int StateHash = Animator.StringToHash("State");
        private static readonly int SeatHash = Animator.StringToHash("Seat");
        private static readonly int ChargeHash = Animator.StringToHash("Charge");
        private static readonly int BlastHash = Animator.StringToHash("Blast");
        private static readonly int PoseHash = Animator.StringToHash("Pose");
        private static readonly int PuffHash = Animator.StringToHash("Puff");
        private static readonly int MegaHash = Animator.StringToHash("Mega");
        private static readonly int CoughHash = Animator.StringToHash("Cough");
        private static readonly int AbilityHash = Animator.StringToHash("Ability");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int CelebrateHash = Animator.StringToHash("Celebrate");

        [SerializeField] private Animator animator;

        private PlayerAvatar avatar;
        private AvatarStateMachine stateMachine;
        private FartThruster thruster;
        private FartCharger charger;
        private float seat;

        public Animator Animator => animator;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            stateMachine = GetComponent<AvatarStateMachine>();
            thruster = GetComponent<FartThruster>();
            charger = GetComponent<FartCharger>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.applyRootMotion = false; // el avatar lo mueve la física, no la animación
        }

        private void OnEnable()
        {
            thruster.Farted += OnFarted;
            thruster.Coughed += OnCoughed;
            GameEvents.OnAbilityUsed += OnAbilityUsed;
            GameEvents.OnSync += OnSync;
            GameEvents.OnCheckpoint += OnCheckpoint;
        }

        private void OnDisable()
        {
            thruster.Farted -= OnFarted;
            thruster.Coughed -= OnCoughed;
            GameEvents.OnAbilityUsed -= OnAbilityUsed;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnCheckpoint -= OnCheckpoint;
        }

        private bool Ready => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;

        private void Update()
        {
            if (!Ready)
                return;
            animator.SetInteger(StateHash, (int)stateMachine.State);
            seat = Mathf.MoveTowards(seat, Mathf.Clamp(avatar.Command.SeatMove, -1f, 1f), Time.deltaTime / SEAT_SMOOTH);
            animator.SetFloat(SeatHash, seat);
            animator.SetFloat(ChargeHash, charger.IsCharging ? charger.Charge01 : 0f);
            animator.SetBool(BlastHash, thruster.IsBlasting);
        }

        public void SetPose(AnimPose pose)
        {
            if (Ready)
                animator.SetInteger(PoseHash, (int)pose);
        }

        public void PlayHit() => Trigger(HitHash);
        public void PlayCelebrate() => Trigger(CelebrateHash);

        private void Trigger(int hash)
        {
            if (Ready)
                animator.SetTrigger(hash);
        }

        private void OnFarted(FartType type, float impulse, float multiplier)
        {
            if (type == FartType.Puff)
                Trigger(PuffHash);
            else if (type == FartType.Charged)
                Trigger(MegaHash);
        }

        private void OnCoughed() => Trigger(CoughHash);

        private void OnAbilityUsed(PlayerAvatar who, string id)
        {
            if (who == avatar)
                Trigger(AbilityHash);
        }

        // Sync x4 (docs/03: Celebrate en sync x4, checkpoint y meta).
        private void OnSync(int players, float multiplier)
        {
            if (players >= 4)
                Trigger(CelebrateHash);
        }

        private void OnCheckpoint(int index) => Trigger(CelebrateHash);
    }
}
