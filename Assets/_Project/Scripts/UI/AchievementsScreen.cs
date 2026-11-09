using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Progression;
using GasSafari.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Logros y estadísticas (docs/07 §2): rejilla de 26 medallas con progreso y una pestaña de estadísticas.
    public class AchievementsScreen : MenuScreen
    {
        private class Cell
        {
            public AchievementData Data;
            public Button Button;
            public Image Medal;
            public LocalizedText Name;
            public LocalizedText Progress;
            public Image Lock;
        }

        private readonly List<Cell> cells = new List<Cell>();
        private readonly List<LocalizedText> statLabels = new List<LocalizedText>();
        private readonly List<LocalizedText> statValues = new List<LocalizedText>();
        private GameObject gridRoot;
        private GameObject statsRoot;
        private Button tabAchievements;
        private Button tabStats;
        private Button back;
        private LocalizedText counter;
        private LocalizedText detailName;
        private LocalizedText detailDesc;
        private LocalizedText detailProgress;
        private bool showStats;

        public override Selectable FirstSelected => showStats ? tabStats : cells.Count > 0 ? cells[0].Button : tabAchievements;
        public int CellCount => cells.Count;
        public bool ShowingStats => showStats;
        public Button TabAchievements => tabAchievements;
        public Button TabStats => tabStats;

        public string CellNameText(int index) => cells[index].Name.Label.text;
        public bool CellUnlocked(int index) => IsUnlocked(cells[index].Data);
        public string DetailNameText => detailName.Label.text;
        public string StatValueText(int index) => statValues[index].Label.text;

        private static SaveData Data => Services.IsReady && Services.Progression != null ? Services.Progression.Data : new SaveData();
        private static bool IsUnlocked(AchievementData data) => Data.achievements.Contains(data.id);

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "ach.title", 70f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0f, 1f), new Vector2(700f, 100f), new Vector2(60f, -24f));
            counter = UiFactory.Label(Root, "Counter", "ach.progress", 44f, TextAlignmentOptions.Right, UiFactory.Gold, FontStyles.Bold, 0, 26).Outlined();
            UiFactory.Place(counter.Label.rectTransform, new Vector2(1f, 1f), new Vector2(360f, 80f), new Vector2(-60f, -34f));

            tabAchievements = UiFactory.MakeButton(Root, "TabAchievements", "ach.title", ButtonStyle.Green, new Vector2(380f, 84f), () => SetTab(false), 36f);
            UiFactory.Place((RectTransform)tabAchievements.transform, new Vector2(0.5f, 1f), new Vector2(380f, 84f), new Vector2(-210f, -132f));
            tabStats = UiFactory.MakeButton(Root, "TabStats", "ach.tab_stats", ButtonStyle.Blue, new Vector2(380f, 84f), () => SetTab(true), 36f);
            UiFactory.Place((RectTransform)tabStats.transform, new Vector2(0.5f, 1f), new Vector2(380f, 84f), new Vector2(210f, -132f));
            UiFactory.LinkHorizontal(tabAchievements, tabStats);

            BuildGrid();
            BuildStats();

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(300f, 92f), () => App.Back(), 40f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(300f, 92f), new Vector2(40f, 24f));
        }

        private void BuildGrid()
        {
            gridRoot = UiFactory.Rect("Grid", Root).gameObject;
            UiFactory.Stretch((RectTransform)gridRoot.transform);
            var panel = UiFactory.Panel(gridRoot.transform, "Panel", "Panel_Dark");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1840f, 740f), new Vector2(0f, -70f));
            panel.raycastTarget = false;
            var grid = UiFactory.Grid(panel.transform, "Cells", new Vector2(292f, 106f), new Vector2(10f, 8f), 6);
            var gridRect = (RectTransform)grid.transform;
            UiFactory.Place(gridRect, new Vector2(0.5f, 1f), new Vector2(6 * 292f + 5 * 10f, 5 * 106f + 4 * 8f), new Vector2(0f, -22f));
            grid.childAlignment = TextAnchor.UpperCenter;

            var catalog = GameCatalog.Instance;
            var list = new List<AchievementData>(catalog != null ? catalog.achievements : new AchievementData[0]);
            list.Sort((a, b) => a.order.CompareTo(b.order));
            foreach (var data in list)
                cells.Add(BuildCell(grid.transform, data));
            LinkCells(6);

            detailName = UiFactory.Label(panel.transform, "DetailName", "ach.first_fart.name", 40f, TextAlignmentOptions.Left, UiFactory.Gold, FontStyles.Bold).Outlined();
            Bottom(detailName.Label.rectTransform, 22f, 0f, 0.3f);
            detailDesc = UiFactory.Label(panel.transform, "DetailDesc", "ach.first_fart.desc", 32f, TextAlignmentOptions.Left, Color.white);
            Bottom(detailDesc.Label.rectTransform, 22f, 0.3f, 0.85f);
            detailProgress = UiFactory.Label(panel.transform, "DetailProgress", "ach.progress", 36f, TextAlignmentOptions.Right, UiFactory.Cream, FontStyles.Bold, 0, 0);
            Bottom(detailProgress.Label.rectTransform, 22f, 0.85f, 1f);
        }

        // Franja inferior del panel: 'bottom' px sobre el borde; xMin/xMax en fracción del ancho.
        private static void Bottom(RectTransform rect, float bottom, float xMin, float xMax)
        {
            rect.anchorMin = new Vector2(xMin, 0f);
            rect.anchorMax = new Vector2(xMax, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(40f, bottom);
            rect.offsetMax = new Vector2(-24f, bottom + 62f);
        }

        private Cell BuildCell(Transform parent, AchievementData data)
        {
            var button = UiFactory.MakeCard(parent, "Ach_" + data.id, new Vector2(292f, 106f), "Panel_Inset");
            var cell = new Cell { Data = data, Button = button };
            cell.Medal = UiFactory.Picture(button.transform, "Medal", data.icon, Color.white);
            UiFactory.Place(cell.Medal.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 90f), new Vector2(18f, 0f));
            cell.Name = UiFactory.Label(button.transform, "Name", data.NameKey, 25f, TextAlignmentOptions.Left, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(cell.Name.Label.rectTransform, new Vector2(0.36f, 0.34f), new Vector2(1f, 1f), new Vector4(0f, 0f, 12f, 8f));
            cell.Progress = UiFactory.Label(button.transform, "Progress", "ach.progress", 24f, TextAlignmentOptions.Left, new Color(0.35f, 0.3f, 0.5f), FontStyles.Bold, 0, 0);
            UiFactory.Region(cell.Progress.Label.rectTransform, new Vector2(0.36f, 0f), new Vector2(1f, 0.34f), new Vector4(0f, 6f, 12f, 0f));
            cell.Lock = UiFactory.Picture(button.transform, "Lock", "Icon_Lock", Color.white);
            UiFactory.Place(cell.Lock.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 40f), new Vector2(43f, 0f));
            var selectCatcher = button.gameObject.AddComponent<CellSelect>();
            selectCatcher.Owner = this;
            selectCatcher.Cell = data;
            return cell;
        }

        private class CellSelect : MonoBehaviour, UnityEngine.EventSystems.ISelectHandler, UnityEngine.EventSystems.IPointerEnterHandler
        {
            public AchievementsScreen Owner;
            public AchievementData Cell;

            public void OnSelect(UnityEngine.EventSystems.BaseEventData eventData) => Owner.ShowDetail(Cell);
            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => Owner.ShowDetail(Cell);
        }

        private void LinkCells(int columns)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % columns > 0) nav.selectOnLeft = cells[i - 1].Button;
                if (i % columns < columns - 1 && i + 1 < cells.Count) nav.selectOnRight = cells[i + 1].Button;
                nav.selectOnUp = i >= columns ? cells[i - columns].Button : tabAchievements;
                nav.selectOnDown = i + columns < cells.Count ? cells[i + columns].Button : back;
                cells[i].Button.navigation = nav;
            }
        }

        private void BuildStats()
        {
            statsRoot = UiFactory.Rect("Stats", Root).gameObject;
            UiFactory.Stretch((RectTransform)statsRoot.transform);
            var panel = UiFactory.Panel(statsRoot.transform, "Panel", "Panel_Light");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 700f), new Vector2(0f, -70f));
            panel.raycastTarget = false;
            string[] keys = { "stats.farts", "stats.sync2", "stats.sync3", "stats.sync4", "stats.super_farts", "stats.falls",
                              "stats.rescues", "stats.sinkings", "stats.distance", "stats.play_time", "stats.fav_character" };
            for (int i = 0; i < keys.Length; i++)
            {
                int column = i / 6;
                int row = i % 6;
                float x = 50f + column * 730f;
                float y = 40f + row * 106f;
                var label = UiFactory.Label(panel.transform, "Label" + i, keys[i], 34f, TextAlignmentOptions.MidlineLeft, UiFactory.Ink, FontStyles.Bold);
                var value = UiFactory.Literal(panel.transform, "Value" + i, "0", 40f, TextAlignmentOptions.MidlineRight, new Color(0.2f, 0.45f, 0.2f), FontStyles.Bold);
                Anchor(label.Label.rectTransform, x, y, 440f, 90f);
                Anchor(value.Label.rectTransform, x + 440f, y, 220f, 90f);
                statLabels.Add(label);
                statValues.Add(value);
            }
        }

        private static void Anchor(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        // ---------------------------------------------------------------- contenido
        public override void OnShow()
        {
            SetTab(showStats, false);
            Refresh();
        }

        public void SetTab(bool stats, bool focus = true)
        {
            showStats = stats;
            gridRoot.SetActive(!stats);
            statsRoot.SetActive(stats);
            SetStyle(tabAchievements, stats ? "Blue" : "Green");
            SetStyle(tabStats, stats ? "Green" : "Blue");
            Refresh();
            LinkCells(6);
            // Abajo desde las pestañas llega a la rejilla o al botón Atrás.
            var tabDown = stats ? (Selectable)back : cells.Count > 0 ? cells[0].Button : back;
            tabAchievements.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = tabStats, selectOnDown = tabDown };
            tabStats.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = tabAchievements, selectOnDown = tabDown };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = stats ? (Selectable)tabStats : cells.Count > 0 ? cells[cells.Count - 1].Button : tabAchievements };
        }

        private static void SetStyle(Button button, string style)
        {
            var image = button.GetComponent<Image>();
            image.sprite = UiFactory.Sprite("Button_" + style + "_Normal");
            button.spriteState = new SpriteState
            {
                highlightedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted"),
                selectedSprite = UiFactory.Sprite("Button_" + style + "_Highlighted"),
                pressedSprite = UiFactory.Sprite("Button_" + style + "_Pressed"),
                disabledSprite = UiFactory.Sprite("Button_" + style + "_Disabled")
            };
        }

        public void Refresh()
        {
            var data = Data;
            int unlocked = 0;
            foreach (var cell in cells)
            {
                bool done = data.achievements.Contains(cell.Data.id);
                unlocked += done ? 1 : 0;
                bool hidden = cell.Data.secret && !done;
                cell.Medal.color = done ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.9f);
                cell.Lock.gameObject.SetActive(!done);
                if (hidden)
                    cell.Name.SetLiteral("???");
                else
                    cell.Name.SetKey(cell.Data.NameKey);
                (int current, int goal) progress = Services.Achievements != null ? Services.Achievements.Progress(cell.Data) : (0, 0);
                bool showProgress = !done && progress.goal > 0;
                cell.Progress.gameObject.SetActive(showProgress);
                if (showProgress)
                    cell.Progress.SetKey("ach.progress", progress.current, progress.goal);
            }
            counter.SetKey("ach.progress", unlocked, cells.Count);
            if (cells.Count > 0)
                ShowDetail(cells[0].Data);
            RefreshStats(data);
        }

        public void ShowDetail(AchievementData achievement)
        {
            var data = Data;
            bool done = data.achievements.Contains(achievement.id);
            bool hidden = achievement.secret && !done;
            if (hidden)
            {
                detailName.SetLiteral("???");
                detailDesc.SetLiteral("???");
            }
            else
            {
                detailName.SetKey(achievement.NameKey);
                detailDesc.SetKey(achievement.DescKey);
            }
            (int current, int goal) progress = Services.Achievements != null ? Services.Achievements.Progress(achievement) : (0, 0);
            bool show = !done && progress.goal > 0;
            detailProgress.gameObject.SetActive(show);
            if (show)
                detailProgress.SetKey("ach.progress", progress.current, progress.goal);
        }

        private void RefreshStats(SaveData data)
        {
            var stats = data.stats;
            statValues[0].SetLiteral(stats.farts.ToString());
            statValues[1].SetLiteral(stats.sync2.ToString());
            statValues[2].SetLiteral(stats.sync3.ToString());
            statValues[3].SetLiteral(stats.sync4.ToString());
            statValues[4].SetLiteral(stats.superFarts.ToString());
            statValues[5].SetLiteral(stats.falls.ToString());
            statValues[6].SetLiteral(stats.rescues.ToString());
            statValues[7].SetLiteral(stats.sinks.ToString());
            statValues[8].SetLiteral(stats.meters + " m");
            statValues[9].SetLiteral(FormatPlayTime(stats.playSeconds));
            string favorite = StatsService.FavoriteCharacter(stats);
            var catalog = GameCatalog.Instance;
            var character = favorite != null && catalog != null ? catalog.FindCharacter(favorite) : null;
            if (character != null)
                statValues[10].SetKey(character.NameKey);
            else
                statValues[10].SetLiteral("-");
        }

        public static string FormatPlayTime(int seconds)
        {
            int hours = seconds / 3600;
            int minutes = seconds % 3600 / 60;
            return hours + ":" + minutes.ToString("00") + ":" + (seconds % 60).ToString("00");
        }
    }
}
