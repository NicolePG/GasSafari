using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.VFX
{
    // Animación procedural de pedos (docs/09 §4): aplastar y estirar caderas, inflar la cabeza al cargar,
    // vibrar en la ráfaga. Corre en LateUpdate, después del Animator, sobre la pose que este dejó.
    [RequireComponent(typeof(FartThruster), typeof(FartCharger))]
    public class FartJuice : MonoBehaviour
    {
        [SerializeField] private FartJuiceConfig config;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform head;

        private FartThruster thruster;
        private FartCharger charger;
        private PlayerSeat seatRef;
        private Vector3 hipsScale;
        private Vector3 hipsPosition;
        private Vector3 headScale;
        private Vector3 headPosition;
        private float puffTime = -1f;
        private float megaTime = -1f;

        private void Awake()
        {
            thruster = GetComponent<FartThruster>();
            charger = GetComponent<FartCharger>();
            seatRef = GetComponent<PlayerSeat>();
            if (hips != null)
            {
                hipsScale = hips.localScale;
                hipsPosition = hips.localPosition;
            }
            if (head != null)
            {
                headScale = head.localScale;
                headPosition = head.localPosition;
            }
        }

        private void OnEnable()
        {
            thruster.Farted += OnFarted;
            GameEvents.OnSuperFart += OnSuperFart;
        }

        private void OnDisable()
        {
            thruster.Farted -= OnFarted;
            GameEvents.OnSuperFart -= OnSuperFart;
        }

        private void OnFarted(FartType type, float impulse, float multiplier)
        {
            if (type == FartType.Puff)
                puffTime = 0f;
            else if (type == FartType.Charged)
                megaTime = 0f;
        }

        private void OnSuperFart(float power)
        {
            if (seatRef.IsSeated)
                megaTime = 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (hips != null)
                AnimateHips(dt);
            if (head != null)
                AnimateHead();
        }

        private void AnimateHips(float dt)
        {
            float scaleY = 1f;
            var offset = Vector3.zero;

            if (puffTime >= 0f)
            {
                puffTime += dt;
                float t = puffTime / config.puffDuration;
                scaleY = PuffCurve(t);
                if (t >= 1f)
                    puffTime = -1f;
            }

            if (megaTime >= 0f)
            {
                megaTime += dt;
                float t = megaTime / config.megaDuration;
                float bell = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                scaleY = Mathf.Lerp(1f, config.megaStretch, bell);
                // Retroceso: el empujón lleva al animal hacia el centro de la balsa (su frente).
                offset += Vector3.forward * (config.megaRecoil * bell);
                if (t >= 1f)
                    megaTime = -1f;
            }

            if (thruster.IsBlasting)
                offset += Vector3.right * (Mathf.Sin(Time.time * config.blastFrequency * 2f * Mathf.PI) * config.blastAmplitude);

            hips.localScale = new Vector3(hipsScale.x, hipsScale.y * scaleY, hipsScale.z);
            hips.localPosition = hipsPosition + offset;
        }

        private void AnimateHead()
        {
            float charge = charger.IsCharging ? charger.Charge01 : 0f;
            head.localScale = headScale * Mathf.Lerp(1f, config.chargeHeadScale, charge);
            float shake = Mathf.Sin(Time.time * config.chargeTremorFrequency * 2f * Mathf.PI) * config.chargeTremor * charge;
            head.localPosition = headPosition + Vector3.right * shake;
        }

        // 1 → aplastado → estirado → 1 a lo largo del toque.
        private float PuffCurve(float t)
        {
            const float third = 1f / 3f;
            if (t < third)
                return Mathf.Lerp(1f, config.puffSquash, t / third);
            if (t < 2f * third)
                return Mathf.Lerp(config.puffSquash, config.puffStretch, (t - third) / third);
            return Mathf.Lerp(config.puffStretch, 1f, Mathf.Clamp01((t - 2f * third) / third));
        }
    }
}
