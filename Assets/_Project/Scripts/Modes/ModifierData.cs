using UnityEngine;

namespace GasSafari.Modes
{
    // Un modificador del modo Caos (docs/06 §1): efecto, valor y estrellas que pide.
    [CreateAssetMenu(menuName = "Gas Safari/Modifier", fileName = "Modifier")]
    public class ModifierData : ScriptableObject
    {
        [Tooltip("soapy, infinite_gas, moon, reverse, bigheads, turbo, dizzy, single (claves mod.<id>.name/desc) (se guarda en GameSession.Modifiers)")] public string id;
        public ModifierKind kind;
        [Tooltip("Multiplicador del efecto: arrastre x0.5, gravedad x0.4, corriente x1.5, cabeza x2; grados de balanceo en Cámara mareada")]
        public float value = 1f;
        [Tooltip("Estrellas totales para desbloquearlo")] public int starsRequired = 30;
        public int order;

        public string NameKey => "mod." + id + ".name";
        public string DescKey => "mod." + id + ".desc";
    }
}
