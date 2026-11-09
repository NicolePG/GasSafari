using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using GasSafari.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Ficha del nivel (docs/07 §2): nombre, mecánica nueva en una frase, tiempos oro y plata, mejor tiempo, mangos 0/3 y los
    // botones Jugar, Contrarreloj (si está desbloqueado) y Modificadores.
    public class LevelSheetScreen : MenuScreen
    {
        private LocalizedText worldName;
        private LocalizedText levelName;
        private LocalizedText mechanic;
        private LocalizedText gold;
        private LocalizedText silver;
        private LocalizedText best;
        private LocalizedText mangosText;
        private readonly Image[] stars = new Image[3];
        private readonly Image[] mangos = new Image[3];
        private readonly LocalizedText[] criteria = new LocalizedText[3];
        private Button play;
        private Button timeTrial;
        private Button modifiers;
        private Button back;
        private Image timeTrialLock;
        private Image modifiersLock;

        public LevelData Level { get; set; }

        public override Selectable FirstSelected => play;
        public Button PlayButton => play;
        public Button TimeTrialButton => timeTrial;
        public Button ModifiersButton => modifiers;
        public string LevelNameText => levelName != null ? levelName.Label.text : string.Empty;
        public string BestTimeText => best != null ? best.Label.text : string.Empty;
        public string GoldText => gold != null ? gold.Label.text : string.Empty;
        public string SilverText => silver != null ? silver.Label.text : string.Empty;
        public string MangosLabelText => mangosText != null ? mangosText.Label.text : string.Empty;
        public bool TimesVisible => gold != null && gold.transform.parent.gameObject.activeSelf;

        public static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        protected override void Build()
        {
            var card = UiFactory.Panel(Root, "Card", "Panel_Light");
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 900f), Vector2.zero);

            worldName = UiFactory.Label(card.transform, "World", "world.w1.name", 34f, TextAlignmentOptions.Left, new Color(0.35f, 0.32f, 0.5f), FontStyles.Bold);
            TopRow(worldName.Label.rectTransform, 40f, 46f, 0f, 0.6f, 70f, 0f);
            levelName = UiFactory.Label(card.transform, "Name", "level.w1_1.name", 70f, TextAlignmentOptions.Left, UiFactory.Ink, FontStyles.Bold);
            TopRow(levelName.Label.rectTransform, 84f, 96f, 0f, 1f, 70f, 70f);

            // Mecánica nueva.
            var mechanicTitle = UiFactory.Label(card.transform, "MechanicTitle", "sheet.new_mechanic", 32f, TextAlignmentOptions.Left, new Color(0.85f, 0.45f, 0.1f), FontStyles.Bold);
            TopRow(mechanicTitle.Label.rectTransform, 196f, 44f, 0f, 1f, 70f, 70f);
            mechanic = UiFactory.Label(card.transform, "Mechanic", "level.w1_1.mechanic", 38f, TextAlignmentOptions.TopLeft, UiFactory.Ink);
            TopRow(mechanic.Label.rectTransform, 240f, 110f, 0f, 1f, 70f, 70f);

            // Tiempos (izquierda) y estrellas con sus condiciones (derecha).
            var times = UiFactory.Panel(card.transform, "Times", "Panel_Inset");
            TopRow(times.rectTransform, 372f, 238f, 0f, 0.44f, 70f, 10f);
            gold = TimeRow(times.transform, 0, "sheet.gold", "Icon_Star_Full");
            silver = TimeRow(times.transform, 1, "sheet.silver", "Icon_Star_Empty");
            best = TimeRow(times.transform, 2, "sheet.best_time", null);

            var starsPanel = UiFactory.Panel(card.transform, "StarsPanel", "Panel_Inset");
            TopRow(starsPanel.rectTransform, 372f, 238f, 0.44f, 1f, 10f, 70f);
            for (int i = 0; i < 3; i++)
            {
                float y = 18f + i * 70f;
                stars[i] = UiFactory.Picture(starsPanel.transform, "Star" + i, "Icon_Star_Empty", Color.white);
                UiFactory.Place(stars[i].rectTransform, new Vector2(0f, 1f), new Vector2(58f, 58f), new Vector2(34f, -y - 4f));
                stars[i].rectTransform.pivot = new Vector2(0f, 1f);
                criteria[i] = UiFactory.Label(starsPanel.transform, "Criteria" + i, "sheet.star" + (i + 1), 30f, TextAlignmentOptions.MidlineLeft, UiFactory.Ink, FontStyles.Bold);
                TopRow(criteria[i].Label.rectTransform, y, 62f, 0f, 1f, 110f, 24f);
            }

            // Mangos.
            for (int i = 0; i < 3; i++)
            {
                mangos[i] = UiFactory.Picture(card.transform, "Mango" + i, "Icon_GoldenMango_Empty", Color.white);
                UiFactory.Place(mangos[i].rectTransform, new Vector2(0f, 1f), new Vector2(84f, 84f), new Vector2(80f + i * 96f, -634f));
                mangos[i].rectTransform.pivot = new Vector2(0f, 1f);
            }
            mangosText = UiFactory.Label(card.transform, "Mangos", "sheet.mangos", 40f, TextAlignmentOptions.MidlineLeft, UiFactory.Ink, FontStyles.Bold, 0);
            TopRow(mangosText.Label.rectTransform, 646f, 64f, 0f, 0.5f, 390f, 0f);

            // Botones.
            var buttons = UiFactory.HorizontalList(card.transform, "Buttons", 22f, TextAnchor.MiddleCenter);
            var buttonsRect = (RectTransform)buttons.transform;
            buttonsRect.anchorMin = new Vector2(0f, 0f);
            buttonsRect.anchorMax = new Vector2(1f, 0f);
            buttonsRect.pivot = new Vector2(0.5f, 0f);
            buttonsRect.sizeDelta = new Vector2(-80f, 112f);
            buttonsRect.anchoredPosition = new Vector2(0f, 38f);
            play = UiFactory.MakeButton(buttons.transform, "Play", "sheet.play", ButtonStyle.Green, new Vector2(400f, 110f), Play, 46f);
            timeTrial = UiFactory.MakeButton(buttons.transform, "TimeTrial", "sheet.time_trial", ButtonStyle.Blue, new Vector2(340f, 110f), TimeTrial, 36f);
            modifiers = UiFactory.MakeButton(buttons.transform, "Modifiers", "sheet.modifiers", ButtonStyle.Blue, new Vector2(340f, 110f), Modifiers, 36f);
            back = UiFactory.MakeButton(buttons.transform, "Back", "ui.back", ButtonStyle.Red, new Vector2(280f, 110f), () => App.Back(), 40f);
            timeTrialLock = UiFactory.Picture(timeTrial.transform, "Lock", "Icon_Lock", Color.white);
            UiFactory.Place(timeTrialLock.rectTransform, new Vector2(1f, 1f), new Vector2(46f, 46f), new Vector2(-4f, 6f));
            modifiersLock = UiFactory.Picture(modifiers.transform, "Lock", "Icon_Lock", Color.white);
            UiFactory.Place(modifiersLock.rectTransform, new Vector2(1f, 1f), new Vector2(46f, 46f), new Vector2(-4f, 6f));
            UiFactory.LinkHorizontal(play, timeTrial, modifiers, back);
        }

        private LocalizedText TimeRow(Transform parent, int row, string labelKey, string iconSprite)
        {
            float y = 16f + row * 70f;
            if (iconSprite != null)
            {
                var icon = UiFactory.Picture(parent, "Icon" + row, iconSprite, Color.white);
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(54f, 54f), new Vector2(26f, -y - 4f));
                icon.rectTransform.pivot = new Vector2(0f, 1f);
            }
            var label = UiFactory.Label(parent, "Label" + row, labelKey, 32f, TextAlignmentOptions.MidlineLeft, UiFactory.Ink, FontStyles.Bold);
            TopRow(label.Label.rectTransform, y, 62f, 0f, 0.62f, 90f, 0f);
            var value = UiFactory.Literal(parent, "Value" + row, "--:--", 40f, TextAlignmentOptions.MidlineRight, UiFactory.Ink, FontStyles.Bold);
            TopRow(value.Label.rectTransform, y, 62f, 0.6f, 1f, 0f, 28f);
            return value;
        }

        private static void TopRow(RectTransform rect, float top, float height, float xMin, float xMax, float padLeft, float padRight)
        {
            rect.anchorMin = new Vector2(xMin, 1f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(padLeft, -top - height);
            rect.offsetMax = new Vector2(-padRight, -top);
        }

        private static SaveData Data => Services.IsReady && Services.Progression != null ? Services.Progression.Data : new SaveData();

        public override void OnShow()
        {
            if (Level == null)
                return;
            var data = Data;
            var world = GameCatalog.Instance != null ? GameCatalog.Instance.WorldOf(Level) : Level.world;
            worldName.SetKey(world != null ? world.NameKey : "world.t.name");
            levelName.SetKey(Level.NameKey);
            mechanic.SetKey(Level.MechanicKey);

            gold.SetLiteral(FormatTime(Level.goldTime));
            silver.SetLiteral(FormatTime(Level.silverTime));
            var record = data.FindLevel(Level.id);
            best.SetLiteral(record != null && record.best > 0f ? FormatTime(record.best) : LocalizedText.Table != null ? LocalizedText.Table.Get("sheet.no_time") : "--:--");

            int earned = record != null ? record.stars : 0;
            for (int i = 0; i < 3; i++)
                stars[i].sprite = UiFactory.Sprite(i < earned ? "Icon_Star_Full" : "Icon_Star_Empty");
            int mask = record != null ? record.mangos : 0;
            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                bool has = (mask & (1 << i)) != 0;
                count += has ? 1 : 0;
                mangos[i].sprite = UiFactory.Sprite(has ? "Icon_GoldenMango_Full" : "Icon_GoldenMango_Empty");
            }
            mangosText.SetKey("sheet.mangos", count);

            // El tutorial no tiene tiempos, modos ni mangos.
            bool tutorial = !Level.showTimer;
            bool trialUnlocked = ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_TIME_TRIAL, data);
            bool modsUnlocked = ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_MODIFIERS, data);
            timeTrial.gameObject.SetActive(!tutorial);
            modifiers.gameObject.SetActive(!tutorial);
            timeTrialLock.gameObject.SetActive(!trialUnlocked);
            modifiersLock.gameObject.SetActive(!modsUnlocked);
            SetLocked(timeTrial, !trialUnlocked, "Blue");
            SetLocked(modifiers, !modsUnlocked, "Blue");
            gold.transform.parent.gameObject.SetActive(!tutorial);
            UiFactory.LinkHorizontal(play, timeTrial.gameObject.activeSelf ? timeTrial : null, modifiers.gameObject.activeSelf ? modifiers : null, back);
        }

        // Un botón bloqueado se ve gris pero se puede enfocar, y al pulsarlo dice qué falta.
        private static void SetLocked(Button button, bool locked, string style)
        {
            var image = button.GetComponent<Image>();
            string state = locked ? "Disabled" : "Normal";
            image.sprite = UiFactory.Sprite("Button_" + style + "_" + state);
            var swap = button.spriteState;
            if (locked)
            {
                swap.highlightedSprite = image.sprite;
                swap.selectedSprite = image.sprite;
                swap.pressedSprite = image.sprite;
            }
            else
            {
                swap.highlightedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted");
                swap.selectedSprite = swap.highlightedSprite;
                swap.pressedSprite = UiFactory.Sprite("Button_" + style + "_Pressed");
            }
            button.spriteState = swap;
        }

        private void Play()
        {
            if (Level == null)
                return;
            if (Services.IsReady)
                Services.Session.ModeId = ProgressionRules.MODE_STORY;
            SceneLoader.LoadLevel(Level);
        }

        private void TimeTrial()
        {
            var data = Data;
            if (!ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_TIME_TRIAL, data))
            {
                App.Toast("lock.any_level");
                return;
            }
            App.Toast("ui.soon");
        }

        private void Modifiers()
        {
            var data = Data;
            if (!ProgressionRules.IsModeUnlocked(ProgressionRules.MODE_MODIFIERS, data))
            {
                App.Toast("lock.stars", 30);
                return;
            }
            App.Toast("ui.soon");
        }
    }
}
