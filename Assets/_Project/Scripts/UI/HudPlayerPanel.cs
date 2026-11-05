using GasSafari.Abilities;
using GasSafari.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Panel de un jugador en las esquinas inferiores (docs/07 §3): retrato con el color del jugador, barra de gas,
    // icono de habilidad con recarga radial y el Botiquín de Melman.
    public class HudPlayerPanel : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image portrait;
        [SerializeField] private Image gasFill;
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image abilityCooldown;
        [SerializeField] private GameObject medkitIcon;
        [SerializeField] private TMP_Text numberText;

        private PlayerAvatar avatar;
        private AbilityRunner runner;
        private MelmanMedkitPassive medkit;

        public PlayerAvatar Avatar => avatar;

        public void Bind(PlayerAvatar target, int playerNumber)
        {
            avatar = target;
            runner = target.GetComponent<AbilityRunner>();
            medkit = target.GetComponent<MelmanMedkitPassive>();

            var character = target.Character;
            frame.color = target.PlayerColor;
            portrait.color = character.uiColor;
            if (character.icon != null)
                portrait.sprite = character.icon;
            numberText.SetText("J{0}", playerNumber);
            if (character.ability != null && character.ability.icon != null)
                abilityIcon.sprite = character.ability.icon;
            medkitIcon.SetActive(medkit != null);
        }

        private void Update()
        {
            if (avatar == null)
                return;
            gasFill.fillAmount = avatar.Gas.Normalized;
            if (runner != null)
                abilityCooldown.fillAmount = 1f - runner.Cooldown.Progress;
            if (medkit != null)
                medkitIcon.SetActive(!medkit.Used);
        }
    }
}
