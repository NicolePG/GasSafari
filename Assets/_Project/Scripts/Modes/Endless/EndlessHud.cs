using GasSafari.UI;
using TMPro;
using UnityEngine;

namespace GasSafari.Modes
{
    // Bajo el cronómetro (docs/07 §3) en Infinito: metros (la puntuación), récord, dificultad y balsas que quedan; en el
    // Desafío Diario: metros de 1500, modificador del día y mejor tiempo de hoy. Reemplaza el panel de mangos y checkpoints.
    public class EndlessHud : MonoBehaviour
    {
        private LocalizedText main;
        private LocalizedText second;
        private LocalizedText third;
        private int shownMeters = -1;

        public string MainText => main != null ? main.Label.text : string.Empty;
        public string SecondText => second != null ? second.Label.text : string.Empty;
        public string ThirdText => third != null ? third.Label.text : string.Empty;

        public void Build()
        {
            var canvas = UiFactory.CreateCanvas("EndlessHud", 12, transform);
            main = UiFactory.Literal(canvas.transform, "Distance", string.Empty, 52f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(main.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 70f), new Vector2(0f, -150f));
            second = UiFactory.Literal(canvas.transform, "Second", string.Empty, 30f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined();
            UiFactory.Place(second.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(900f, 44f), new Vector2(0f, -212f));
            third = UiFactory.Literal(canvas.transform, "Third", string.Empty, 30f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined();
            UiFactory.Place(third.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(900f, 44f), new Vector2(0f, -254f));

            var hud = FindFirstObjectByType<LevelHud>();
            if (hud != null && hud.Progress != null)
                hud.Progress.gameObject.SetActive(false);
        }

        // Solo reescribe el texto cuando cambia el metro entero (sin basura por cuadro).
        public void SetMeters(float meters, float total)
        {
            int value = Mathf.FloorToInt(meters);
            if (value == shownMeters)
                return;
            shownMeters = value;
            if (float.IsPositiveInfinity(total))
                main.SetKey("endless.distance", value);
            else
                main.SetKey("daily.hud.distance", value, Mathf.FloorToInt(total));
        }

        public void SetSecond(string key, params object[] args) => second.SetKey(key, args);

        public void SetThird(string key, params object[] args) => third.SetKey(key, args);

        public static void Announce(string key, params object[] args)
        {
            var hud = FindFirstObjectByType<LevelHud>();
            var table = LocalizedText.Table;
            if (hud != null && hud.EventText != null && table != null)
                hud.EventText.Show(table.Format(key, args), true);
        }
    }
}
