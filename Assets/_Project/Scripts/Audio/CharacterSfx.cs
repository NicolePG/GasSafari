using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.Audio
{
    // Sonido de la habilidad activa del personaje (docs/08 §4: ability_*). Los pedos los reproduce FartFeedback.
    [RequireComponent(typeof(PlayerAvatar))]
    public class CharacterSfx : MonoBehaviour
    {
        private PlayerAvatar avatar;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
        }

        private void OnEnable()
        {
            GameEvents.OnAbilityUsed += OnAbilityUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnAbilityUsed -= OnAbilityUsed;
        }

        private void OnAbilityUsed(PlayerAvatar who, string id)
        {
            if (who != avatar || Services.Audio == null)
                return;
            var set = avatar.Character != null ? avatar.Character.sfxSet : null;
            if (set != null && set.ability != null)
                Services.Audio.Play(set.ability, transform.position, 0f, null, false);
        }
    }
}
