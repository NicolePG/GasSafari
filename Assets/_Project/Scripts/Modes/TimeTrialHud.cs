using System.Globalization;
using GasSafari.UI;
using TMPro;
using UnityEngine;

namespace GasSafari.Modes
{
    // Debajo del cronómetro: tiempo del fantasma y, al pasar cada checkpoint, la diferencia con él (verde si van adelante).
    public class TimeTrialHud : MonoBehaviour
    {
        public static readonly Color Ahead = new Color(0.45f, 1f, 0.45f);
        public static readonly Color Behind = new Color(1f, 0.45f, 0.4f);

        private LocalizedText ghostLabel;
        private LocalizedText splitLabel;
        private float splitTimer;
        private float splitSeconds = 3f;

        public string GhostText => ghostLabel != null ? ghostLabel.Label.text : string.Empty;
        public string SplitText => splitLabel != null && splitLabel.gameObject.activeSelf ? splitLabel.Label.text : string.Empty;
        public Color SplitColor => splitLabel != null ? splitLabel.Label.color : Color.white;

        public void Build(float ghostTime, float showSeconds)
        {
            splitSeconds = showSeconds;
            var canvas = UiFactory.CreateCanvas("TimeTrialHud", 12, transform);
            ghostLabel = UiFactory.Label(canvas.transform, "Ghost", ghostTime > 0f ? "trial.ghost_time" : "trial.no_ghost", 30f,
                TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold, LevelSheetScreen.FormatTime(ghostTime)).Outlined();
            UiFactory.Place(ghostLabel.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 44f), new Vector2(0f, -150f));
            splitLabel = UiFactory.Literal(canvas.transform, "Split", string.Empty, 46f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(splitLabel.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(500f, 64f), new Vector2(0f, -196f));
            splitLabel.gameObject.SetActive(false);
        }

        // delta = tiempo actual menos el del fantasma en el mismo punto.
        public void ShowSplit(float delta)
        {
            splitLabel.SetLiteral(FormatDelta(delta));
            splitLabel.Label.color = delta <= 0f ? Ahead : Behind;
            splitLabel.gameObject.SetActive(true);
            splitTimer = splitSeconds;
        }

        public static string FormatDelta(float delta)
        {
            return delta.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " s";
        }

        private void Update()
        {
            if (splitTimer <= 0f)
                return;
            splitTimer -= Time.deltaTime;
            if (splitTimer <= 0f)
                splitLabel.gameObject.SetActive(false);
        }
    }
}
