using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Arriba a la izquierda (docs/07 §3): 4 cámaras de aire (verde sana, gris reventada) y borde azul con escudo de coco.
    public class HudChambers : MonoBehaviour
    {
        [SerializeField] private Image[] icons = new Image[RaftChambers.CHAMBER_COUNT];
        [SerializeField] private Image shieldFrame;
        [SerializeField] private Color intactColor = new Color(0.3f, 0.8f, 0.35f);
        [SerializeField] private Color poppedColor = new Color(0.45f, 0.45f, 0.45f);

        private RaftChambers chambers;

        public void Bind(LevelManager level)
        {
            chambers = level.Raft.GetComponent<RaftChambers>();
            GameEvents.OnChamberPopped += OnChanged;
            GameEvents.OnChamberRepaired += OnChanged;
            GameEvents.OnRespawn += Refresh;
            GameEvents.OnPowerUp += OnPowerUp;
            chambers.ShieldConsumed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            GameEvents.OnChamberPopped -= OnChanged;
            GameEvents.OnChamberRepaired -= OnChanged;
            GameEvents.OnRespawn -= Refresh;
            GameEvents.OnPowerUp -= OnPowerUp;
            if (chambers != null)
                chambers.ShieldConsumed -= Refresh;
        }

        private void OnChanged(int chamber) => Refresh();
        private void OnPowerUp(PowerUps.PowerUpData data, Characters.PlayerAvatar taker) => Refresh();

        private void Refresh()
        {
            for (int i = 0; i < icons.Length; i++)
                icons[i].color = chambers.IsIntact(i) ? intactColor : poppedColor;
            shieldFrame.gameObject.SetActive(chambers.HasShield);
        }
    }
}
