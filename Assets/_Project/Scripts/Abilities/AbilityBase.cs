using GasSafari.Audio;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Habilidad activa (docs/03). El asset no guarda estado de partida: la recarga la lleva AbilityRunner
    // y los efectos con duración van en componentes temporales sobre el avatar o la balsa.
    public abstract class AbilityBase : ScriptableObject
    {
        public string id;
        [Tooltip("Segundos")] public float cooldown;
        public Sprite icon;
        public SoundId sound;

        public virtual bool CanActivate(AbilityContext context) => true;
        public abstract void Activate(AbilityContext context);
    }
}
