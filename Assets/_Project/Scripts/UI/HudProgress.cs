using GasSafari.Core;
using GasSafari.Level;
using TMPro;
using UnityEngine;

namespace GasSafari.UI
{
    // Arriba a la derecha (docs/07 §3): mangos dorados del nivel y checkpoint actual.
    public class HudProgress : MonoBehaviour
    {
        private const string GOLDEN_MANGO_ID = "P07";

        [SerializeField] private TMP_Text mangoText;
        [SerializeField] private TMP_Text checkpointText;

        private UIStrings strings;
        private CheckpointSystem checkpoints;
        private int mangoTotal;
        private int mangos;

        public int Mangos => mangos;

        public void Bind(LevelManager level, CheckpointSystem checkpointSystem, UIStrings uiStrings)
        {
            strings = uiStrings;
            checkpoints = checkpointSystem;
            mangoTotal = level.Data != null ? level.Data.mangoCount : 3;
            GameEvents.OnPowerUp += OnPowerUp;
            GameEvents.OnCheckpoint += OnCheckpoint;
            RefreshMangos();
            RefreshCheckpoint();
        }

        private void OnDestroy()
        {
            GameEvents.OnPowerUp -= OnPowerUp;
            GameEvents.OnCheckpoint -= OnCheckpoint;
        }

        private void OnPowerUp(PowerUps.PowerUpData data, Characters.PlayerAvatar taker)
        {
            if (data.id != GOLDEN_MANGO_ID)
                return;
            mangos++;
            RefreshMangos();
        }

        private void OnCheckpoint(int index) => RefreshCheckpoint();

        private void RefreshMangos()
        {
            mangoText.SetText(strings.Get("hud.mangos"), mangos, mangoTotal);
        }

        private void RefreshCheckpoint()
        {
            if (checkpoints == null)
                return;
            checkpointText.SetText(strings.Get("hud.checkpoint_progress"), checkpoints.CurrentIndex, checkpoints.Active.Count - 1);
        }
    }
}
