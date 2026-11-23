using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Modes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Fiesta (docs/06 §1): los 5 minijuegos (los que faltan dicen "Próximamente"), la Fiesta completa (los 5 seguidos) y
    // los puntos acumulados. "Terminar la Fiesta" muestra el ranking final con los frijolitos y vuelve al menú principal.
    public class PartySelectScreen : MenuScreen
    {
        private readonly List<(PartyGameData game, Button button)> cards = new List<(PartyGameData, Button)>();
        private readonly List<GameObject> standingRows = new List<GameObject>();
        private RectTransform standings;
        private LocalizedText played;
        private Button finish;
        private Button back;
        private GameObject finalPanel;
        private RectTransform finalRows;
        private Button finalClose;
        private Button tour;
        private LocalizedText finalBeans;

        public override Selectable FirstSelected => cards.Count > 0 ? cards[0].button : back;
        public IReadOnlyList<(PartyGameData game, Button button)> Cards => cards;
        public Button FinishButton => finish;
        public GameObject FinalPanel => finalPanel;
        public Button TourButton => tour;
        public string FinalBeansText => finalBeans != null ? finalBeans.Label.text : string.Empty;

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "party.title", 78f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 110f), new Vector2(0f, -36f));

            var games = new List<PartyGameData>(ModesConfig.Instance != null ? ModesConfig.Instance.partyGames : new PartyGameData[0]);
            games.RemoveAll(g => g == null);
            games.Sort((a, b) => a.order.CompareTo(b.order));

            var grid = UiFactory.Grid(Root, "Games", new Vector2(360f, 300f), new Vector2(22f, 22f), 3);
            UiFactory.Place((RectTransform)grid.transform, new Vector2(0f, 0.5f), new Vector2(3 * 360f + 2 * 22f, 2 * 300f + 22f), new Vector2(80f + (3 * 360f + 44f) * 0.5f, -10f));
            foreach (var game in games)
                cards.Add((game, BuildCard(grid.transform, game)));
            tour = BuildTourCard(grid.transform);

            var panel = UiFactory.Panel(Root, "Standings", "Panel_Dark");
            UiFactory.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(600f, 622f), new Vector2(-80f - 300f, -10f));
            var cupTitle = UiFactory.Label(panel.transform, "Title", "party.cup_title", 44f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold);
            UiFactory.Place(cupTitle.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 70f), new Vector2(0f, -24f));
            played = UiFactory.Label(panel.transform, "Played", "party.games_played", 30f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Normal, 0);
            UiFactory.Place(played.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 50f), new Vector2(0f, -92f));
            standings = (RectTransform)UiFactory.VerticalList(panel.transform, "Rows", 10f).transform;
            UiFactory.Place(standings, new Vector2(0.5f, 1f), new Vector2(540f, 4 * 80f), new Vector2(0f, -160f));
            finish = UiFactory.MakeButton(panel.transform, "Finish", "party.end", ButtonStyle.Green, new Vector2(500f, 96f), ShowFinal, 40f);
            UiFactory.Place((RectTransform)finish.transform, new Vector2(0.5f, 0f), new Vector2(500f, 96f), new Vector2(0f, 30f));

            back = UiFactory.MakeButton(Root, "Back", "ui.back", ButtonStyle.Red, new Vector2(320f, 100f), () => App.Back(), 44f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(320f, 100f), new Vector2(60f, 36f));
            LinkNavigation();
            BuildFinal();
        }

        private Button BuildCard(Transform parent, PartyGameData game)
        {
            var button = UiFactory.MakeCard(parent, "Game_" + game.id, new Vector2(360f, 300f));
            var icon = UiFactory.Picture(button.transform, "Icon", game.icon, Color.white);
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, 110f), new Vector2(0f, -18f));
            var name = UiFactory.Label(button.transform, "Name", game.NameKey, 38f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(name.Label.rectTransform, new Vector2(0f, 0.37f), new Vector2(1f, 0.55f), new Vector4(18f, 0f, 18f, 0f));
            var desc = UiFactory.Label(button.transform, "Desc", game.DescKey, 25f, TextAlignmentOptions.Top, new Color(0.25f, 0.23f, 0.35f));
            UiFactory.Region(desc.Label.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.36f), new Vector4(22f, 0f, 22f, 0f));
            if (!game.implemented)
            {
                var veil = UiFactory.Picture(button.transform, "Soon", (Sprite)null, new Color(0.08f, 0.07f, 0.16f, 0.9f), false);
                UiFactory.Stretch(veil.rectTransform);
                veil.raycastTarget = false;
                var soon = UiFactory.Label(veil.transform, "Text", "ui.soon", 36f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
                UiFactory.Stretch(soon.Label.rectTransform);
            }
            button.onClick.AddListener(() => Choose(game));
            return button;
        }

        // Sexta tarjeta de la grilla: los 5 minijuegos seguidos, con los puntos desde cero.
        private Button BuildTourCard(Transform parent)
        {
            var button = UiFactory.MakeCard(parent, "Game_All", new Vector2(360f, 300f));
            var icon = UiFactory.Picture(button.transform, "Icon", "Icon_Mode_M5_Fiesta", UiFactory.Gold);
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, 110f), new Vector2(0f, -18f));
            var name = UiFactory.Label(button.transform, "Name", "party.play_all", 38f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(name.Label.rectTransform, new Vector2(0f, 0.37f), new Vector2(1f, 0.55f), new Vector4(18f, 0f, 18f, 0f));
            var desc = UiFactory.Label(button.transform, "Desc", "party.play_all.desc", 25f, TextAlignmentOptions.Top, new Color(0.25f, 0.23f, 0.35f));
            UiFactory.Region(desc.Label.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.36f), new Vector4(22f, 0f, 22f, 0f));
            button.onClick.AddListener(() => PlayAll());
            return button;
        }

        public bool PlayAll() => PartyLauncher.StartTour();

        private void LinkNavigation()
        {
            const int columns = 3;
            var buttons = new List<Button>();
            foreach (var card in cards)
                buttons.Add(card.button);
            buttons.Add(tour);
            for (int i = 0; i < buttons.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % columns > 0) nav.selectOnLeft = buttons[i - 1];
                nav.selectOnRight = i % columns < columns - 1 && i + 1 < buttons.Count ? buttons[i + 1] : finish;
                if (i >= columns) nav.selectOnUp = buttons[i - columns];
                nav.selectOnDown = i + columns < buttons.Count ? buttons[i + columns] : back;
                buttons[i].navigation = nav;
            }
            var finishNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = cards.Count > 0 ? cards[0].button : back };
            finish.navigation = finishNav;
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = cards.Count > 0 ? cards[0].button : null, selectOnRight = finish };
        }

        public bool Choose(PartyGameData game)
        {
            if (!game.implemented)
            {
                App.Toast("ui.soon");
                return false;
            }
            return PartyLauncher.Play(game);
        }

        public override void OnShow()
        {
            played.SetKey("party.games_played", PartyCup.GamesPlayed);
            FillStandings(standings, standingRows, 40f);
            finish.interactable = PartyCup.GamesPlayed > 0;
            finalPanel.SetActive(false);
            // Volvimos del último minijuego de la Fiesta completa.
            if (PartyCup.ShowFinalOnReturn)
            {
                PartyCup.ShowFinalOnReturn = false;
                ShowFinal();
            }
        }

        private static void FillStandings(RectTransform parent, List<GameObject> rows, float size)
        {
            foreach (var row in rows)
                Destroy(row);
            rows.Clear();
            var list = PartyCup.Standings();
            if (list.Count == 0)
            {
                var none = UiFactory.Label(parent, "None", "party.no_games", size * 0.8f, TextAlignmentOptions.Center, UiFactory.Muted);
                none.Label.rectTransform.sizeDelta = new Vector2(parent.sizeDelta.x, 80f);
                rows.Add(none.gameObject);
                return;
            }
            foreach (var entry in list)
            {
                var rowRect = UiFactory.Rect("Row", parent);
                rowRect.sizeDelta = new Vector2(parent.sizeDelta.x, 72f);
                var row = rowRect.gameObject;
                var rank = UiFactory.Label(row.transform, "Rank", "party.rank", size, TextAlignmentOptions.Left, entry.rank == 1 ? UiFactory.Gold : UiFactory.Cream, FontStyles.Bold, entry.rank);
                UiFactory.Region(rank.Label.rectTransform, new Vector2(0f, 0f), new Vector2(0.2f, 1f));
                var chip = UiFactory.Picture(row.transform, "Chip", (Sprite)null, entry.slot.Color, false);
                UiFactory.Place(chip.rectTransform, new Vector2(0.22f, 0.5f), new Vector2(16f, 50f), Vector2.zero);
                var name = UiFactory.Literal(row.transform, "Name", PartyGameBase.PlayerName(entry.slot), size, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Region(name.Label.rectTransform, new Vector2(0.26f, 0f), new Vector2(0.72f, 1f));
                var points = UiFactory.Label(row.transform, "Points", "party.total", size * 0.9f, TextAlignmentOptions.Right, UiFactory.Cream, FontStyles.Normal, entry.points);
                UiFactory.Region(points.Label.rectTransform, new Vector2(0.72f, 0f), new Vector2(1f, 1f));
                rows.Add(row);
            }
        }

        // ---------------------------------------------------------------- ranking final
        private readonly List<GameObject> finalRowObjects = new List<GameObject>();

        private void BuildFinal()
        {
            var dim = UiFactory.Backdrop(Root, "FinalDim");
            finalPanel = dim.gameObject;
            var panel = UiFactory.Panel(dim.transform, "Panel");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 780f), Vector2.zero);
            var title = UiFactory.Label(panel.transform, "Title", "party.final_title", 66f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(820f, 100f), new Vector2(0f, -36f));
            var inset = UiFactory.Panel(panel.transform, "Inset", "Panel_Dark");
            UiFactory.Place(inset.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 380f), new Vector2(0f, -150f));
            finalRows = (RectTransform)UiFactory.VerticalList(inset.transform, "Rows", 10f).transform;
            UiFactory.Place(finalRows, new Vector2(0.5f, 1f), new Vector2(740f, 4 * 82f), new Vector2(0f, -20f));
            finalBeans = UiFactory.Label(panel.transform, "Beans", "party.beans", 40f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold, 0);
            UiFactory.Place(finalBeans.Label.rectTransform, new Vector2(0.5f, 0f), new Vector2(820f, 60f), new Vector2(0f, 175f));
            finalClose = UiFactory.MakeButton(panel.transform, "Close", "ui.continue", ButtonStyle.Green, new Vector2(420f, 100f), EndParty, 44f);
            UiFactory.Place((RectTransform)finalClose.transform, new Vector2(0.5f, 0f), new Vector2(420f, 100f), new Vector2(0f, 40f));
            finalPanel.SetActive(false);
        }

        public void ShowFinal()
        {
            FillStandings(finalRows, finalRowObjects, 46f);
            finalBeans.SetKey("party.beans", PartyCup.AwardBeans());
            if (Services.IsReady && Services.Music != null)
                Services.Music.Play("mus_results");
            finalPanel.SetActive(true);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(finalClose.gameObject);
        }

        private void EndParty()
        {
            PartyCup.Reset();
            finalPanel.SetActive(false);
            App.GoTo<MainMenuScreen>();
        }

        public override bool HandleCancel()
        {
            if (finalPanel != null && finalPanel.activeSelf)
            {
                finalPanel.SetActive(false);
                return true;
            }
            return base.HandleCancel();
        }
    }
}
