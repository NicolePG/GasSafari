using UnityEngine;

namespace GasSafari.VFX
{
    // Valores de la animación procedural de pedos (skill vfx-juice, docs/09 §4).
    [CreateAssetMenu(menuName = "Gas Safari/Fart Juice", fileName = "FartJuice")]
    public class FartJuiceConfig : ScriptableObject
    {
        [Header("Toque")]
        public float puffSquash;
        public float puffStretch;
        [Tooltip("s")] public float puffDuration;

        [Header("Ráfaga")]
        [Tooltip("Hz")] public float blastFrequency;
        [Tooltip("m")] public float blastAmplitude;

        [Header("Carga")]
        public float chargeHeadScale;
        [Tooltip("Provisional: temblor máximo de la cabeza en m")] public float chargeTremor;
        [Tooltip("Hz")] public float chargeTremorFrequency;

        [Header("Megapedo")]
        [Tooltip("Provisional: escala Y máxima de las caderas")] public float megaStretch;
        [Tooltip("m")] public float megaRecoil;
        [Tooltip("Provisional: s")] public float megaDuration;
    }
}
