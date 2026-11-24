using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Lobby de Carrera por Equipos (docs/06 §1, docs/07 §2): después de elegir animales, cada jugador se mueve a la
    // izquierda (equipo Azul) o a la derecha (equipo Naranja), hasta 2 animales por balsa; los bots completan los lugares
    // libres. Debajo, las 3 pistas de la serie (al azar entre las compatibles abiertas) con "Sortear pistas", y "¡A correr!".
    // Cada jugador mueve lo suyo con su propio mando o teclado; el primero también recorre los botones (arriba / abajo).
    public class TeamRaceScreen : MenuScreen
    {
        private const float STICK = 0.6f;

        private readonly LocalizedText[][] members = new LocalizedText[TeamRaceSeries.TEAMS][];
        private readonly Image[][] chips = new Image[TeamRaceSeries.TEAMS][];
        private readonly LocalizedText[] trackLines = new LocalizedText[TeamRaceSeries.RACES];
        private readonly List<PlayerSlot> blue = new List<PlayerSlot>();
        private readonly List<PlayerSlot> orange = new List<PlayerSlot>();
        private readonly List<LevelData> tracks = new List<LevelData>();
        private readonly Dictionary<PlayerSlot, int> heldX = new Dictionary<PlayerSlot, int>();
        private readonly Dictionary<PlayerSlot, int> heldY = new Dictionary<PlayerSlot, int>();
        private Button start;
        private Button shuffle;
        private Button back;
        private Button[] bar;
        private int focus;
        private LocalizedText status;
        private bool leaving;

        public override bool HandlesOwnNavigation => true;
        public override Selectable FirstSelected => start;
        public Button StartButton => start;
        public Button ShuffleButton => shuffle;
        public IReadOnlyList<LevelData> Tracks => tracks;
        public IReadOnlyList<PlayerSlot> Blue => blue;
        public IReadOnlyList<PlayerSlot> Orange => orange;
        // Las pruebas fijan el azar de las pistas.
        public static System.Random Rng { get; set; } = new System.Random();

        private static IReadOnlyList<PlayerSlot> Session => Services.IsReady ? Services.Session.Players : new List<PlayerSlot>();

        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "teamrace.lobby.title", 70f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 96f), new Vector2(0f, -20f));
            var hint = UiFactory.Label(Root, "Hint", "teamrace.lobby.hint", 34f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Place(hint.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 56f), new Vector2(0f, -116f));

            for (int team = 0; team < TeamRaceSeries.TEAMS; team++)
                BuildTeam(team);

            var tracksBox = UiFactory.Panel(Root, "Tracks", "Panel_Dark");
            UiFactory.Place(tracksBox.rectTransform, new Vector2(0.5f, 0f), new Vector2(980f, 250f), new Vector2(-300f, 40f));
            var tracksTitle = UiFactory.Label(tracksBox.transform, "Title", "teamrace.lobby.tracks", 34f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold);
            UiFactory.Place(tracksTitle.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(900f, 50f), new Vector2(0f, -12f));
            for (int i = 0; i < trackLines.Length; i++)
            {
                trackLines[i] = UiFactory.Label(tracksBox.transform, "Track" + i, null, 34f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Place(trackLines[i].Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(880f, 52f), new Vector2(0f, -66f - i * 56f));
            }

            var list = UiFactory.VerticalList(Root, "Buttons", 16f);
            UiFactory.Place((RectTransform)list.transform, new Vector2(0.5f, 0f), new Vector2(520f, 3 * 82f + 32f), new Vector2(520f, 40f));
            start = UiFactory.MakeButton(list.transform, "Start", "teamrace.lobby.start", ButtonStyle.Green, new Vector2(520f, 82f), StartSeries, 42f);
            shuffle = UiFactory.MakeButton(list.transform, "Shuffle", "teamrace.lobby.shuffle", ButtonStyle.Blue, new Vector2(520f, 82f), Shuffle, 38f);
            back = UiFactory.MakeButton(list.transform, "Back", "ui.back", ButtonStyle.Red, new Vector2(520f, 82f), () => App.Back(), 38f);
            bar = new[] { start, shuffle, back };
            foreach (var button in bar)
                button.navigation = new Navigation { mode = Navigation.Mode.None };

            status = UiFactory.Label(Root, "Status", null, 30f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Place(status.Label.rectTransform, new Vector2(0.5f, 0f), new Vector2(1500f, 44f), new Vector2(0f, 300f));
        }

        private void BuildTeam(int team)
        {
            var box = UiFactory.Panel(Root, "Team" + team, "Panel_Dark");
            UiFactory.Place(box.rectTransform, new Vector2(team == 0 ? 0.28f : 0.72f, 1f), new Vector2(720f, 380f), new Vector2(0f, -190f));
            var header = UiFactory.Label(box.transform, "Header", TeamRaceMode.TeamKeys[team], 54f, TextAlignmentOptions.Center, TeamRaceMode.TeamColors[team], FontStyles.Bold).Outlined();
            UiFactory.Place(header.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(680f, 80f), new Vector2(0f, -18f));
            members[team] = new LocalizedText[TeamRaceSeries.CREW];
            chips[team] = new Image[TeamRaceSeries.CREW];
            for (int i = 0; i < TeamRaceSeries.CREW; i++)
            {
                var row = UiFactory.Panel(box.transform, "Member" + i, "Panel_Inset");
                UiFactory.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(640f, 104f), new Vector2(0f, -116f - i * 120f));
                chips[team][i] = UiFactory.Picture(row.transform, "Chip", (Sprite)null, Color.white, false);
                UiFactory.Region(chips[team][i].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector4(16f, 16f, -40f, 16f));
                members[team][i] = UiFactory.Label(row.transform, "Name", null, 40f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold);
                UiFactory.Region(members[team][i].Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(60f, 0f, 16f, 0f));
            }
        }

        public override void OnShow()
        {
            leaving = false;
            focus = 0;
            heldX.Clear();
            heldY.Clear();
            TeamRaceSeries.AutoAssign(Session);
            Shuffle();
            SetUiMaps(true);
            Select();
        }

        public override void OnHide()
        {
            SetUiMaps(false);
        }

        // ---------------------------------------------------------------- acciones
        public void Shuffle()
        {
            var data = Services.IsReady && Services.Progression != null ? Services.Progression.Data : null;
            tracks.Clear();
            tracks.AddRange(TeamRaceSeries.PickTracks(TeamRaceSeries.AvailableTracks(GameCatalog.Instance, data), Rng));
            Refresh();
        }

        // Cambia de equipo a ese jugador (dirección -1 = Azul, +1 = Naranja). false si el otro equipo está lleno.
        public bool Move(PlayerSlot slot, int direction)
        {
            int team = direction < 0 ? 0 : 1;
            bool moved = TeamRaceSeries.SetTeam(slot, team, Session);
            if (moved)
                status.SetLiteral(string.Empty);
            else
                status.SetKey("teamrace.lobby.full");
            Refresh();
            return moved;
        }

        public void StartSeries()
        {
            if (leaving || tracks.Count == 0)
                return;
            leaving = true;
            SetUiMaps(false);
            TeamRaceLauncher.Begin(tracks);
        }

        public void Refresh()
        {
            TeamRaceSeries.BuildCrews(Session, blue, orange);
            Fill(0, blue);
            Fill(1, orange);
            var table = LocalizedText.Table;
            for (int i = 0; i < trackLines.Length; i++)
            {
                if (i >= tracks.Count)
                {
                    trackLines[i].SetLiteral(string.Empty);
                    continue;
                }
                string name = table != null ? table.Get(tracks[i].NameKey) : tracks[i].id;
                trackLines[i].SetLiteral((i + 1) + ". " + tracks[i].id + "  " + name);
            }
        }

        private void Fill(int team, List<PlayerSlot> crew)
        {
            for (int i = 0; i < TeamRaceSeries.CREW; i++)
            {
                bool used = i < crew.Count;
                chips[team][i].color = used ? crew[i].Color : Color.clear;
                members[team][i].SetLiteral(used ? MemberName(crew[i]) : string.Empty);
            }
        }

        private static string MemberName(PlayerSlot slot)
        {
            var table = LocalizedText.Table;
            string animal = PartyGameBase.PlayerName(slot);
            if (slot.IsBot)
                return animal + " · " + (table != null ? table.Get("select.bot") : "Bot");
            string tag = table != null ? table.Format("hud.player_tag", slot.Index + 1) : "J" + (slot.Index + 1);
            return tag + " · " + animal;
        }

        // ---------------------------------------------------------------- entrada de cada jugador
        private void Update()
        {
            if (leaving)
                return;
            var session = Session;
            PlayerSlot leader = null;
            for (int i = 0; i < session.Count; i++)
            {
                var slot = session[i];
                if (slot.IsBot || slot.Input == null)
                    continue;
                if (leader == null)
                    leader = slot;
                Poll(slot, slot == leader);
                if (leaving)
                    return;
            }
        }

        private void Poll(PlayerSlot slot, bool leader)
        {
            var actions = slot.Input.actions;
            var navigate = actions.FindAction("UI/Navigate");
            var submit = actions.FindAction("UI/Submit");
            var cancel = actions.FindAction("UI/Cancel");
            if (navigate == null || submit == null || cancel == null)
                return;
            var value = navigate.ReadValue<Vector2>();
            int x = Edge(heldX, slot, value.x > STICK ? 1 : value.x < -STICK ? -1 : 0);
            int y = Edge(heldY, slot, value.y > STICK ? -1 : value.y < -STICK ? 1 : 0);
            if (x != 0)
                Move(slot, x);
            if (y != 0 && leader)
            {
                focus = Mathf.Clamp(focus + y, 0, bar.Length - 1);
                Select();
            }
            if (submit.WasPressedThisFrame())
                bar[focus].onClick.Invoke();
            else if (cancel.WasPressedThisFrame())
                App.Back();
        }

        private static int Edge(Dictionary<PlayerSlot, int> held, PlayerSlot slot, int now)
        {
            held.TryGetValue(slot, out int before);
            held[slot] = now;
            return now != before ? now : 0;
        }

        private void Select()
        {
            if (EventSystem.current != null && bar != null)
                EventSystem.current.SetSelectedGameObject(bar[focus].gameObject);
        }

        private static void SetUiMaps(bool enabled)
        {
            var session = Session;
            for (int i = 0; i < session.Count; i++)
            {
                var slot = session[i];
                if (slot.IsBot || slot.Input == null)
                    continue;
                var map = slot.Input.actions.FindActionMap("UI");
                if (map == null)
                    continue;
                if (enabled) map.Enable(); else map.Disable();
            }
        }
    }
}
