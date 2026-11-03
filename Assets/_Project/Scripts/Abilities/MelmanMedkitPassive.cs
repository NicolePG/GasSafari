using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Botiquín Hipocondríaco (docs/03): mantener el botón de habilidad repara 1 cámara, 1 vez por checkpoint.
    // Solo cuenta si esa pulsación no activó el Cuello Gancho (no había a quién ni dónde engancharse).
    [RequireComponent(typeof(AbilityRunner))]
    public class MelmanMedkitPassive : PassiveBase
    {
        [SerializeField] private BalanceConfig balance;

        private AbilityRunner runner;
        private RaftChambers chambers;
        private bool timing;
        private float held;

        public bool Used { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            runner = GetComponent<AbilityRunner>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            runner.AbilityPressed += OnAbilityPressed;
            GameEvents.OnCheckpoint += OnCheckpoint;
        }

        protected override void OnDisable()
        {
            runner.AbilityPressed -= OnAbilityPressed;
            GameEvents.OnCheckpoint -= OnCheckpoint;
            base.OnDisable();
        }

        protected override void OnBoard(RaftSeats raft) => chambers = raft.GetComponent<RaftChambers>();

        protected override void OnLeave(RaftSeats raft)
        {
            chambers = null;
            timing = false;
        }

        private void OnAbilityPressed(bool activated)
        {
            if (activated || Used || chambers == null || chambers.IntactCount == RaftChambers.CHAMBER_COUNT)
                return;
            timing = true;
            held = 0f;
        }

        // Solo cuenta mientras se mantiene el botón tras una pulsación válida.
        private void Update()
        {
            if (!timing)
                return;
            if (chambers == null || !Avatar.Command.AbilityHeld)
            {
                timing = false;
                return;
            }

            held += Time.deltaTime;
            if (held >= balance.medkitHoldTime)
                Repair();
        }

        private void Repair()
        {
            timing = false;
            int chamber = chambers.ChamberAt(transform.position);
            if (chambers.IsIntact(chamber))
                chamber = FirstPopped();
            if (chamber < 0)
                return;

            chambers.Repair(chamber);
            Used = true;
            Services.Audio.Play(SoundId.AbilityRepair, transform.position);
        }

        private int FirstPopped()
        {
            for (int i = 0; i < RaftChambers.CHAMBER_COUNT; i++)
            {
                if (!chambers.IsIntact(i))
                    return i;
            }
            return -1;
        }

        private void OnCheckpoint(int index) => Used = false;
    }
}
