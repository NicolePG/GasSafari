using GasSafari.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.Modes
{
    // HUD de Carrera por Equipos (docs/06 §1, docs/07): línea entre las dos mitades, arriba de cada mitad el equipo con su
    // puesto en la carrera y sus victorias, número de carrera bajo el cronómetro, avisos grandes en el centro y, al final,
    // el panel de la serie (ganador de cada pista y el marcador) con "Siguiente pista" o "Volver al menú". Se arma en
    // ejecución con UiFactory, como el HUD de la Fiesta.
    public class TeamRaceHud : MonoBehaviour
    {
        private const float REFRESH = 0.25f;
        private const float ANNOUNCE_SECONDS = 2.5f;

        private readonly LocalizedText[] teamLines = new LocalizedText[TeamRaceSeries.TEAMS];
        private readonly LocalizedText[] winLines = new LocalizedText[TeamRaceSeries.TEAMS];
        private readonly int[] shownPlace = { -1, -1 };
        private readonly int[] shownWins = { -1, -1 };
        private TeamRaceMode mode;
        private LocalizedText raceLabel;
        private LocalizedText announce;
        private GameObject results;
        private LocalizedText resultsHeadline;
        private LocalizedText resultsScore;
        private RectTransform rows;
        private Button next;
        private Button menu;
        private float refreshTimer;
        private float announceTimer;

        public GameObject ResultsPanel => results;
        public Button NextButton => next;
        public Button MenuButton => menu;
        public string AnnounceText => announce != null ? announce.Label.text : string.Empty;
        public string Headline => resultsHeadline != null ? resultsHeadline.Label.text : string.Empty;

        public void Bind(TeamRaceMode raceMode)
        {
            mode = raceMode;
            Build();
            mode.RaceWon += OnRaceWon;
            Refresh();
        }

        private void OnDestroy()
        {
            if (mode != null)
                mode.RaceWon -= OnRaceWon;
        }

        private static string Text(string key, params object[] args)
        {
            var table = LocalizedText.Table;
            if (table == null)
                return key;
            return args.Length > 0 ? table.Format(key, args) : table.Get(key);
        }

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("TeamRaceHudCanvas", 15, transform);
            var root = (RectTransform)canvas.transform;

            var divider = UiFactory.Picture(root, "Divider", (Sprite)null, new Color(0.08f, 0.06f, 0.04f, 1f), false);
            UiFactory.Region(divider.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector4(-5f, 0f, -5f, 0f));

            for (int team = 0; team < TeamRaceSeries.TEAMS; team++)
            {
                var box = UiFactory.Panel(root, "Team" + team, "Panel_Dark");
                UiFactory.Place(box.rectTransform, new Vector2(team == 0 ? 0.25f : 0.75f, 1f), new Vector2(520f, 116f), new Vector2(0f, -20f));
                var chip = UiFactory.Picture(box.transform, "Chip", (Sprite)null, TeamRaceMode.TeamColors[team], false);
                UiFactory.Region(chip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector4(14f, 14f, -38f, 14f));
                teamLines[team] = UiFactory.Label(box.transform, "Team", null, 44f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Region(teamLines[team].Label.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector4(54f, 0f, 14f, 6f));
                winLines[team] = UiFactory.Label(box.transform, "Wins", null, 32f, TextAlignmentOptions.Left, UiFactory.Gold, FontStyles.Bold);
                UiFactory.Region(winLines[team].Label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector4(54f, 8f, 14f, 0f));
            }

            var raceBox = UiFactory.Panel(root, "Race", "Panel_Dark");
            UiFactory.Place(raceBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(300f, 60f), new Vector2(0f, -150f));
            raceLabel = UiFactory.Label(raceBox.transform, "Label", "teamrace.race", 34f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold,
                TeamRaceSeries.RaceIndex + 1, TeamRaceSeries.RACES);
            UiFactory.Stretch(raceLabel.Label.rectTransform);

            announce = UiFactory.Label(root, "Announce", null, 120f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(announce.Label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1800f, 220f), new Vector2(0f, 120f));
            announce.gameObject.SetActive(false);

            BuildResults(root);
            EnsureEventSystem();
        }

        private void BuildResults(RectTransform root)
        {
            var dim = UiFactory.Backdrop(root, "ResultsDim");
            results = dim.gameObject;
            var panel = UiFactory.Panel(dim.transform, "Panel");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1240f, 860f), Vector2.zero);
            var title = UiFactory.Label(panel.transform, "Title", "mode.teamrace.name", 64f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100f, 90f), new Vector2(0f, -34f));
            resultsHeadline = UiFactory.Label(panel.transform, "Headline", null, 54f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Place(resultsHeadline.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100f, 80f), new Vector2(0f, -126f));
            resultsScore = UiFactory.Label(panel.transform, "Score", null, 46f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Place(resultsScore.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100f, 70f), new Vector2(0f, -206f));

            var list = UiFactory.VerticalList(panel.transform, "Rows", 14f);
            rows = (RectTransform)list.transform;
            UiFactory.Place(rows, new Vector2(0.5f, 1f), new Vector2(1100f, TeamRaceSeries.RACES * 96f), new Vector2(0f, -290f));

            var buttons = UiFactory.HorizontalList(panel.transform, "Buttons", 30f);
            UiFactory.Place((RectTransform)buttons.transform, new Vector2(0.5f, 0f), new Vector2(1100f, 110f), new Vector2(0f, 46f));
            next = UiFactory.MakeButton(buttons.transform, "Next", "teamrace.next", ButtonStyle.Green, new Vector2(560f, 104f), TeamRaceLauncher.Next, 42f);
            menu = UiFactory.MakeButton(buttons.transform, "Menu", "teamrace.to_menu", ButtonStyle.Blue, new Vector2(440f, 104f), TeamRaceLauncher.Exit, 42f);
            UiFactory.LinkHorizontal(next, menu);
            results.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (mode == null)
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
                if (announceTimer <= 0f)
                    announce.gameObject.SetActive(false);
            }
        }

        // Solo reescribe los textos cuando cambia el puesto o las victorias.
        private void Refresh()
        {
            int leader = mode.Winner >= 0 ? mode.Winner : mode.Leader;
            for (int team = 0; team < TeamRaceSeries.TEAMS; team++)
            {
                int place = team == leader ? 1 : 2;
                if (place != shownPlace[team])
                {
                    shownPlace[team] = place;
                    teamLines[team].SetLiteral(Text(TeamRaceMode.TeamKeys[team]) + "  " + Text("party.rank", place));
                }
                int wins = TeamRaceSeries.Wins(team);
                if (wins != shownWins[team])
                {
                    shownWins[team] = wins;
                    winLines[team].SetKey("teamrace.wins", wins, TeamRaceSeries.WINS_NEEDED);
                }
            }
        }

        private void OnRaceWon(int team)
        {
            Refresh();
            announce.gameObject.SetActive(true);
            announce.SetKey("party.winner", Text(TeamRaceMode.TeamKeys[team]));
            announceTimer = ANNOUNCE_SECONDS;
        }

        // Panel final: quién ganó esta pista, el marcador de la serie y una fila por pista.
        public void ShowResults()
        {
            announce.gameObject.SetActive(false);
            int series = TeamRaceSeries.SeriesWinner;
            if (series >= 0)
                resultsHeadline.SetKey("teamrace.series_won", Text(TeamRaceMode.TeamKeys[series]));
            else if (mode.Winner >= 0)
                resultsHeadline.SetKey("party.winner", Text(TeamRaceMode.TeamKeys[mode.Winner]));
            resultsScore.SetLiteral(Text(TeamRaceMode.TeamKeys[0]) + "  " + TeamRaceSeries.Wins(0) + " – " + TeamRaceSeries.Wins(1) + "  " + Text(TeamRaceMode.TeamKeys[1]));
            FillRows();

            bool over = TeamRaceSeries.IsOver;
            next.gameObject.SetActive(!over);
            if (!over)
                next.GetComponentInChildren<LocalizedText>(true).SetKey("teamrace.next", TeamRaceSeries.RaceIndex + 2, TeamRaceSeries.RACES);
            results.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(over ? menu.gameObject : next.gameObject);
        }

        private void FillRows()
        {
            for (int i = rows.childCount - 1; i >= 0; i--)
                Destroy(rows.GetChild(i).gameObject);
            var tracks = TeamRaceSeries.Tracks;
            for (int i = 0; i < TeamRaceSeries.RACES; i++)
            {
                var row = UiFactory.Panel(rows, "Row" + i, "Panel_Dark");
                row.rectTransform.sizeDelta = new Vector2(1100f, 86f);
                string track = i < tracks.Count && tracks[i] != null ? Text(tracks[i].NameKey) : string.Empty;
                var name = UiFactory.Literal(row.transform, "Track", Text("teamrace.race", i + 1, TeamRaceSeries.RACES) + " · " + track, 36f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Region(name.Label.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector4(24f, 0f, 0f, 0f));
                bool played = i < TeamRaceSeries.Winners.Count;
                int winner = played ? TeamRaceSeries.Winners[i] : -1;
                var who = UiFactory.Literal(row.transform, "Winner", played ? Text(TeamRaceMode.TeamKeys[winner]) : Text("teamrace.pending"), 36f,
                    TextAlignmentOptions.Center, played ? TeamRaceMode.TeamColors[winner] : UiFactory.Muted, FontStyles.Bold);
                UiFactory.Region(who.Label.rectTransform, new Vector2(0.55f, 0f), new Vector2(0.82f, 1f));
                var time = UiFactory.Literal(row.transform, "Time", played ? UI.LevelSheetScreen.FormatTime(TeamRaceSeries.Times[i]) : "—", 36f,
                    TextAlignmentOptions.Center, UiFactory.Cream);
                UiFactory.Region(time.Label.rectTransform, new Vector2(0.82f, 0f), new Vector2(1f, 1f));
            }
        }
    }
}
