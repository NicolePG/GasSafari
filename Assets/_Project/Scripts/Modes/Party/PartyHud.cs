using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GasSafari.Modes
{
    // HUD de un minijuego de Fiesta (docs/06 §1, docs/07): estado arriba (tiempo o ronda), marcador por jugador o equipo,
    // cuenta 3-2-1 y avisos en el centro, y al final el ranking del minijuego con los puntos de la Fiesta. Se arma en
    // ejecución con UiFactory, como los menús.
    public class PartyHud : MonoBehaviour
    {
        public struct Entry
        {
            public Color Color;
            public string Label;
            public string Value;
        }

        private const float REFRESH = 0.25f;
        private const float ANNOUNCE_SECONDS = 1.6f;

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<(Image chip, LocalizedText text)> board = new List<(Image, LocalizedText)>();
        private PartyGameBase game;
        private LocalizedText status;
        private LocalizedText announce;
        private GameObject results;
        private RectTransform rows;
        private Button retry;
        private Button other;
        private float refreshTimer;
        private float announceTimer;

        public GameObject ResultsPanel => results;
        public Button RetryButton => retry;
        public Button OtherButton => other;
        public string AnnounceText => announce != null ? announce.Label.text : string.Empty;

        public void Bind(PartyGameBase partyGame)
        {
            game = partyGame;
            Build();
            game.CountdownTick += OnCountdown;
            game.Announced += OnAnnounced;
            game.StateChanged += OnStateChanged;
            game.ScoresChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (game == null)
                return;
            game.CountdownTick -= OnCountdown;
            game.Announced -= OnAnnounced;
            game.StateChanged -= OnStateChanged;
            game.ScoresChanged -= Refresh;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("PartyHudCanvas", 10, transform);
            var root = (RectTransform)canvas.transform;

            var statusBox = UiFactory.Panel(root, "StatusBox", "Panel_Dark");
            UiFactory.Place(statusBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(420f, 88f), new Vector2(0f, -22f));
            status = UiFactory.Label(statusBox.transform, "Status", null, 56f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold);
            UiFactory.Stretch(status.Label.rectTransform);

            var list = UiFactory.HorizontalList(root, "Board", 18f);
            UiFactory.Place((RectTransform)list.transform, new Vector2(0.5f, 1f), new Vector2(1600f, 76f), new Vector2(0f, -118f));
            for (int i = 0; i < GameSession.MAX_PLAYERS; i++)
            {
                var cell = UiFactory.Panel(list.transform, "Entry" + i, "Panel_Dark");
                cell.rectTransform.sizeDelta = new Vector2(360f, 72f);
                var chip = UiFactory.Picture(cell.transform, "Chip", (Sprite)null, Color.white, false);
                UiFactory.Place(chip.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 52f), new Vector2(22f, 0f));
                var text = UiFactory.Label(cell.transform, "Text", null, 38f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Region(text.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(46f, 0f, 14f, 0f));
                board.Add((chip, text));
                cell.gameObject.SetActive(false);
            }

            announce = UiFactory.Label(root, "Announce", null, 150f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(announce.Label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1700f, 240f), new Vector2(0f, 80f));
            announce.gameObject.SetActive(false);

            BuildResults(root);
            EnsureEventSystem();
        }

        private void BuildResults(RectTransform root)
        {
            var dim = UiFactory.Backdrop(root, "ResultsDim");
            results = dim.gameObject;
            var panel = UiFactory.Panel(dim.transform, "Panel");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1240f, 820f), Vector2.zero);
            var title = UiFactory.Label(panel.transform, "Title", game.Data.NameKey, 70f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100f, 100f), new Vector2(0f, -40f));
            var header = UiFactory.Label(panel.transform, "Header", "party.results_header", 30f, TextAlignmentOptions.Center, UiFactory.Muted, FontStyles.Bold);
            UiFactory.Place(header.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100f, 50f), new Vector2(0f, -140f));

            var list = UiFactory.VerticalList(panel.transform, "Rows", 14f);
            rows = (RectTransform)list.transform;
            UiFactory.Place(rows, new Vector2(0.5f, 1f), new Vector2(1100f, 4 * 96f), new Vector2(0f, -200f));

            var buttons = UiFactory.HorizontalList(panel.transform, "Buttons", 30f);
            UiFactory.Place((RectTransform)buttons.transform, new Vector2(0.5f, 0f), new Vector2(1100f, 110f), new Vector2(0f, 50f));
            retry = UiFactory.MakeButton(buttons.transform, "Retry", "results.retry", ButtonStyle.Blue, new Vector2(440f, 104f), () => PartyLauncher.Play(game.Data), 44f);
            other = UiFactory.MakeButton(buttons.transform, "Other", "party.other_game", ButtonStyle.Green, new Vector2(520f, 104f), PartyLauncher.BackToParty, 44f);
            UiFactory.LinkHorizontal(other, retry);
            results.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (game == null)
                return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = REFRESH;
                Refresh();
            }
            if (announceTimer > 0f)
            {
                announceTimer -= Time.unscaledDeltaTime;
                if (announceTimer <= 0f && game.State != PartyState.Over)
                    announce.gameObject.SetActive(false);
            }
        }

        private void Refresh()
        {
            game.GetStatus(out string key, out object[] args, out string literal);
            if (key != null)
                status.SetKey(key, args);
            else
                status.SetLiteral(literal ?? string.Empty);

            entries.Clear();
            game.FillBoard(entries);
            for (int i = 0; i < board.Count; i++)
            {
                bool used = i < entries.Count;
                board[i].chip.transform.parent.gameObject.SetActive(used);
                if (!used)
                    continue;
                board[i].chip.color = entries[i].Color;
                board[i].text.SetLiteral(entries[i].Label + "  " + entries[i].Value);
            }
        }

        private void OnCountdown(int value)
        {
            if (value > 0)
                Show(null, null, value.ToString());
            else
                Show("hud.go", null, null);
        }

        private void OnAnnounced(string key, object[] args) => Show(key, args, null);

        private void Show(string key, object[] args, string literal)
        {
            announce.gameObject.SetActive(true);
            if (key != null)
                announce.SetKey(key, args ?? new object[0]);
            else
                announce.SetLiteral(literal);
            announceTimer = ANNOUNCE_SECONDS;
        }

        private void OnStateChanged(PartyState state)
        {
            if (state != PartyState.Results)
                return;
            announce.gameObject.SetActive(false);
            FillResults();
            results.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(other.gameObject);
        }

        // Una fila por jugador, del primero al último: puesto, color, nombre, resultado, puntos ganados y total de la Fiesta.
        private void FillResults()
        {
            for (int i = rows.childCount - 1; i >= 0; i--)
                Destroy(rows.GetChild(i).gameObject);
            var order = new List<int>();
            for (int i = 0; i < game.Players.Count; i++)
                order.Add(i);
            order.Sort((a, b) => game.Ranks[a] != game.Ranks[b] ? game.Ranks[a].CompareTo(game.Ranks[b]) : game.Players[a].Index.CompareTo(game.Players[b].Index));
            foreach (int i in order)
            {
                var slot = game.Players[i];
                var row = UiFactory.Panel(rows, "Row" + i, "Panel_Dark");
                row.rectTransform.sizeDelta = new Vector2(1100f, 86f);
                int rank = game.Ranks[i];
                var place = UiFactory.Label(row.transform, "Rank", "party.rank", 44f, TextAlignmentOptions.Center, rank == 1 ? UiFactory.Gold : UiFactory.Cream, FontStyles.Bold, rank);
                UiFactory.Region(place.Label.rectTransform, new Vector2(0f, 0f), new Vector2(0.12f, 1f));
                var chip = UiFactory.Picture(row.transform, "Chip", (Sprite)null, slot.Color, false);
                UiFactory.Place(chip.rectTransform, new Vector2(0.13f, 0.5f), new Vector2(18f, 60f), Vector2.zero);
                var name = UiFactory.Literal(row.transform, "Name", PartyGameBase.PlayerName(slot), 42f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Region(name.Label.rectTransform, new Vector2(0.15f, 0f), new Vector2(0.45f, 1f));
                var result = UiFactory.Literal(row.transform, "Result", game.ResultText(i), 40f, TextAlignmentOptions.Center, UiFactory.Cream);
                UiFactory.Region(result.Label.rectTransform, new Vector2(0.45f, 0f), new Vector2(0.65f, 1f));
                var gained = UiFactory.Label(row.transform, "Gained", "party.points", 38f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold, PartyCup.PointsForRank(rank));
                UiFactory.Region(gained.Label.rectTransform, new Vector2(0.65f, 0f), new Vector2(0.8f, 1f));
                var total = UiFactory.Label(row.transform, "Total", "party.total", 36f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Normal, PartyCup.Points(slot.Index));
                UiFactory.Region(total.Label.rectTransform, new Vector2(0.8f, 0f), new Vector2(1f, 1f));
            }
        }

        public static string Clock(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}
