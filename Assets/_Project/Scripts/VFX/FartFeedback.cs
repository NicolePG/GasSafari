using GasSafari.Audio;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.VFX
{
    // Partículas y sonido de los pedos de un animal (docs/09 §6, docs/08 §2).
    [RequireComponent(typeof(FartThruster), typeof(FartCharger))]
    public class FartFeedback : MonoBehaviour
    {
        // docs/08 §2: el pedo que completa un grupo de sync suena +3 dB.
        private const float SYNC_GAIN_DB = 3f;
        // Provisional: cuánto sube el pitch del loop de carga a carga completa.
        private const float CHARGE_PITCH_RISE = 0.5f;

        [Tooltip("Punto de salida del gas (el trasero)")]
        [SerializeField] private Transform emitPoint;
        [SerializeField] private Color coughColor = Color.gray;

        private FartThruster thruster;
        private FartCharger charger;
        private PooledVfx blastFx;
        private int blastLoop;
        private int chargeLoop;

        // Lo fija el cosmético del jugador.
        public Color GasColor { get; set; } = Color.white;

        private void Awake()
        {
            thruster = GetComponent<FartThruster>();
            charger = GetComponent<FartCharger>();
            if (emitPoint == null)
                emitPoint = transform;
        }

        private void OnEnable()
        {
            thruster.Farted += OnFarted;
            thruster.Coughed += OnCoughed;
            charger.ChargeStarted += OnChargeStarted;
        }

        private void OnDisable()
        {
            thruster.Farted -= OnFarted;
            thruster.Coughed -= OnCoughed;
            charger.ChargeStarted -= OnChargeStarted;
            StopBlast();
            StopCharge();
        }

        private Quaternion Exhaust => Quaternion.LookRotation(-thruster.ThrustDirection);

        private void OnFarted(FartType type, float impulse, float multiplier)
        {
            float gain = multiplier > 1f ? SYNC_GAIN_DB : 0f;
            var position = emitPoint.position;
            switch (type)
            {
                case FartType.Puff:
                    Services.Vfx.Play(VfxId.FartPuff, position, Exhaust, GasColor);
                    Services.Audio.Play(SoundId.FartPuff, position, gain);
                    break;
                case FartType.Charged:
                    StopCharge();
                    Services.Vfx.Play(VfxId.FartMega, position, Exhaust, GasColor);
                    Services.Audio.Play(SoundId.FartMega, position, gain);
                    break;
                case FartType.Blast:
                    StopBlast();
                    blastFx = Services.Vfx.Play(VfxId.FartBlast, position, Exhaust, GasColor, emitPoint);
                    blastLoop = Services.Audio.PlayLoop(SoundId.FartBlast, emitPoint);
                    break;
            }
        }

        private void OnCoughed()
        {
            var position = emitPoint.position;
            Services.Vfx.Play(VfxId.Cough, position, Quaternion.identity, coughColor);
            Services.Audio.Play(SoundId.FartCough, position);
        }

        private void OnChargeStarted()
        {
            StopCharge();
            chargeLoop = Services.Audio.PlayLoop(SoundId.FartCharge, emitPoint);
        }

        private void Update()
        {
            if (blastLoop != 0 && !thruster.IsBlasting)
                StopBlast();

            if (chargeLoop == 0)
                return;
            if (!charger.IsCharging)
                StopCharge();
            else
                Services.Audio.SetPitch(chargeLoop, 1f + charger.Charge01 * CHARGE_PITCH_RISE);
        }

        private void StopBlast()
        {
            if (blastFx != null)
                blastFx.Stop();
            blastFx = null;
            if (blastLoop != 0 && Services.IsReady)
                Services.Audio.Stop(blastLoop);
            blastLoop = 0;
        }

        private void StopCharge()
        {
            if (chargeLoop != 0 && Services.IsReady)
                Services.Audio.Stop(chargeLoop);
            chargeLoop = 0;
        }
    }
}
