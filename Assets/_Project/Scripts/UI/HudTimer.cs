using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Team;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Arriba al centro (docs/07 §3): cronómetro y medidor de equipo; al llenarse brilla con "¡SÚPER PEDO LISTO!".
    public class HudTimer : MonoBehaviour
    {
        private const float GLOW_SPEED = 6f;

        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image meterFill;
        [SerializeField] private Image meterGlow;
        [SerializeField] private TMP_Text superReadyText;

        private LevelTimer timer;
        private TeamMeter meter;
        private int lastSecond = -1;

        public void Bind(LevelManager level, UIStrings strings)
        {
            timer = level.Timer;
            meter = level.Raft.GetComponent<TeamMeter>();
            superReadyText.SetText(strings.Get("hud.superfart_ready"));
            GameEvents.OnTeamMeterChanged += OnMeterChanged;
            OnMeterChanged(meter.Value);
            Refresh(0f);
        }

        private void OnDestroy()
        {
            GameEvents.OnTeamMeterChanged -= OnMeterChanged;
        }

        private void OnMeterChanged(float value)
        {
            meterFill.fillAmount = meter.Normalized;
            bool full = meter.IsFull;
            superReadyText.gameObject.SetActive(full);
            meterGlow.gameObject.SetActive(full);
        }

        private void Update()
        {
            if (timer == null)
                return;
            // El texto solo cambia al cambiar el segundo.
            int second = (int)timer.Total;
            if (second != lastSecond)
                Refresh(timer.Total);
            if (meterGlow.gameObject.activeSelf)
            {
                var color = meterGlow.color;
                color.a = 0.4f + 0.4f * Mathf.Sin(Time.unscaledTime * GLOW_SPEED);
                meterGlow.color = color;
            }
        }

        private void Refresh(float total)
        {
            lastSecond = (int)total;
            timeText.SetText("{0:0}:{1:00}", lastSecond / 60, lastSecond % 60);
        }
    }
}
