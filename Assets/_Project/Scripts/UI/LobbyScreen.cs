using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Selección de personajes (docs/07 §2, docs/03 Reglas de selección): hasta 4 columnas, "Pulsa A para unirte", carrusel con la
    // pose de selección, stats en barras, sombrero y color de gas, "Listo", y la barra del anfitrión (J1) con Aleatorio, bots y
    // dificultad. Cada jugador maneja su columna con su propio mando o con el teclado; el ratón también funciona.
    public class LobbyScreen : MenuScreen
    {
        public const int COLUMNS = 4;
        private const int ROW_CHARACTER = 0;
        private const int ROW_HAT = 1;
        private const int ROW_GAS = 2;
        private const int ROW_BAR = 3;
        private const float ARM_TIME = 0.35f;
        private const float START_DELAY = 0.9f;
        private const float STICK = 0.6f;
        private const float REPEAT_DELAY = 0.4f;
        private const float REPEAT_RATE = 0.15f;

        private static readonly string[] StatKeys = { "stat.gas", "stat.puff", "stat.blast", "stat.recharge", "stat.weight" };

        public class Column
        {
            public int Index;
            public GameObject Root;
            public GameObject Empty;
            public GameObject Filled;
            public Image Ring;
            public Image Badge;
            public Image Pose;
            public LocalizedText Tag;
            public LocalizedText Name;
            public LocalizedText Role;
            public LocalizedText Ability;
            public LocalizedText Difficulty;
            public Image[] Bars = new Image[5];
            public Image HatIcon;
            public LocalizedText HatValue;
            public Image GasIcon;
            public LocalizedText GasValue;
            public Image[] RowHighlight = new Image[3];
            public Button ReadyButton;
            public LocalizedText ReadyLabel;
            public GameObject ReadyBadge;
            public GameObject TakenNote;

            public PlayerSlot Slot;
            public CharacterData Character;
            public bool Ready;
            public int Row;
            public float Arm;
            public int HeldX, HeldY;
            public float Repeat;
            public bool IsBot => Slot != null && Slot.IsBot;
            public bool IsOccupied => Slot != null;
        }

        private readonly Column[] columns = new Column[COLUMNS];
        private readonly List<PlayerSlot> addedBots = new List<PlayerSlot>();
        private Button random;
        private Button addBot;
        private Button removeBot;
        private Button difficultyButton;
        private LocalizedText difficultyLabel;
        private Button back;
        private LocalizedText status;
        private Button[] bar;
        private int barIndex;
        private float startTimer = -1f;
        private bool proceeding;
        private bool subscribed;
        private float[] statMax = new float[5];

        public override bool HandlesOwnNavigation => true;
        public IReadOnlyList<Column> Columns => columns;
        public Button AddBotButton => addBot;
        public Button RemoveBotButton => removeBot;
        public Button DifficultyButton => difficultyButton;
        public Button RandomButton => random;
        public Button BackButton => back;
        public bool IsStarting => startTimer >= 0f;
        public int BotCount
        {
            get
            {
                int count = 0;
                foreach (var column in columns)
                    count += column.IsBot ? 1 : 0;
                return count;
            }
        }

        public int HumanCount
        {
            get
            {
                int count = 0;
                foreach (var column in columns)
                    count += column.IsOccupied && !column.IsBot ? 1 : 0;
                return count;
            }
        }

        // ---------------------------------------------------------------- construcción
        protected override void Build()
        {
            var title = UiFactory.Label(Root, "Title", "select.title", 70f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold).Outlined(0.3f);
            UiFactory.Place(title.Label.rectTransform, new Vector2(0.5f, 1f), new Vector2(1500f, 96f), new Vector2(0f, -16f));

            ComputeStatMax();
            var strip = UiFactory.HorizontalList(Root, "Columns", 20f, TextAnchor.UpperCenter);
            UiFactory.Place((RectTransform)strip.transform, new Vector2(0.5f, 1f), new Vector2(COLUMNS * 450f + 3 * 20f, 770f), new Vector2(0f, -116f));
            for (int i = 0; i < COLUMNS; i++)
                columns[i] = BuildColumn(strip.transform, i);

            BuildBar();
        }

        private void ComputeStatMax()
        {
            var catalog = GameCatalog.Instance;
            if (catalog == null)
                return;
            foreach (var c in catalog.characters)
            {
                statMax[0] = Mathf.Max(statMax[0], c.gasCapacity);
                statMax[1] = Mathf.Max(statMax[1], c.puffImpulse);
                statMax[2] = Mathf.Max(statMax[2], c.blastForce);
                statMax[3] = Mathf.Max(statMax[3], c.regenPerSecond);
                statMax[4] = Mathf.Max(statMax[4], c.weight);
            }
        }

        private Column BuildColumn(Transform parent, int index)
        {
            var column = new Column { Index = index };
            var panel = UiFactory.Panel(parent, "Column" + (index + 1), "Panel_Dark");
            panel.rectTransform.sizeDelta = new Vector2(450f, 770f);
            column.Root = panel.gameObject;

            // Marco del color del jugador.
            var palette = GameCatalog.Instance != null ? GameCatalog.Instance.playerPalette : null;
            column.Badge = UiFactory.Picture(panel.transform, "Badge", (Sprite)null, Color.white);
            UiFactory.Place(column.Badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(112f, 112f), new Vector2(-150f, -14f));
            var ring = UiFactory.Picture(panel.transform, "Ring", "Frame_J" + (index + 1), Color.white);
            UiFactory.Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(112f, 112f), new Vector2(-150f, -14f));
            column.Ring = ring;

            // ---- vacío
            column.Empty = UiFactory.Rect("Empty", panel.transform).gameObject;
            UiFactory.Stretch((RectTransform)column.Empty.transform);
            var emptyTag = UiFactory.Literal(column.Empty.transform, "Tag", "J" + (index + 1), 64f, TextAlignmentOptions.Center, PlayerColor(index), FontStyles.Bold).Outlined();
            UiFactory.Region(emptyTag.Label.rectTransform, new Vector2(0f, 0.62f), Vector2.one, new Vector4(20f, 0f, 20f, 40f));
            var join = UiFactory.Label(column.Empty.transform, "Join", "ui.join", 44f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            UiFactory.Region(join.Label.rectTransform, new Vector2(0f, 0.28f), new Vector2(1f, 0.6f), new Vector4(40f, 0f, 40f, 0f));

            // ---- ocupada
            column.Filled = UiFactory.Rect("Filled", panel.transform).gameObject;
            UiFactory.Stretch((RectTransform)column.Filled.transform);
            var filled = column.Filled.transform;

            column.Tag = UiFactory.Literal(filled, "Tag", "J" + (index + 1), 54f, TextAlignmentOptions.Left, PlayerColor(index), FontStyles.Bold).Outlined();
            TopRow(column.Tag.Label.rectTransform, 28f, 80f, 0.5f, 1f, 20f, 28f);

            column.Pose = UiFactory.Picture(filled, "Pose", (Sprite)null, Color.white);
            UiFactory.Place(column.Pose.rectTransform, new Vector2(0.5f, 1f), new Vector2(210f, 210f), new Vector2(0f, -100f));

            column.Name = UiFactory.Label(filled, "Name", "char.alex.name", 50f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            TopRow(column.Name.Label.rectTransform, 318f, 56f);
            column.Role = UiFactory.Label(filled, "Role", "char.alex.role", 28f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold);
            TopRow(column.Role.Label.rectTransform, 372f, 38f);

            // Stats en barras.
            for (int s = 0; s < StatKeys.Length; s++)
            {
                float y = 414f + s * 29f;
                var label = UiFactory.Label(filled, "StatLabel" + s, StatKeys[s], 24f, TextAlignmentOptions.MidlineLeft, UiFactory.Cream);
                TopRow(label.Label.rectTransform, y, 28f, 0f, 0.46f, 30f, 0f);
                var back = UiFactory.Picture(filled, "StatBack" + s, "Meter_Back", Color.white, false);
                back.type = Image.Type.Sliced;
                TopRow(back.rectTransform, y + 4f, 20f, 0.46f, 1f, 0f, 30f);
                var fill = UiFactory.Picture(back.transform, "Fill", "Meter_Fill", Color.white, false);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                UiFactory.Stretch(fill.rectTransform);
                column.Bars[s] = fill;
            }
            column.Difficulty = UiFactory.Label(filled, "Difficulty", "char.diff.easy", 24f, TextAlignmentOptions.MidlineLeft, UiFactory.Muted, FontStyles.Bold);
            TopRow(column.Difficulty.Label.rectTransform, 562f, 28f, 0f, 0.46f, 30f, 0f);
            column.Ability = UiFactory.Label(filled, "Ability", "char.alex.ability_name", 26f, TextAlignmentOptions.MidlineRight, UiFactory.Cream, FontStyles.Bold);
            TopRow(column.Ability.Label.rectTransform, 562f, 28f, 0.46f, 1f, 0f, 30f);

            BuildRow(filled, column, ROW_HAT, "Hat", "Icon_Hat_H01_ZooCap", 598f, out column.HatIcon, out column.HatValue);
            BuildRow(filled, column, ROW_GAS, "Gas", "Icon_Gas_G01_Classic", 650f, out column.GasIcon, out column.GasValue);
            // La fila del personaje se resalta alrededor de la pose.
            var charHighlight = UiFactory.Picture(filled, "CharHighlight", "Panel_Inset", new Color(1f, 0.85f, 0.3f, 0.35f), false);
            charHighlight.type = Image.Type.Sliced;
            UiFactory.Place(charHighlight.rectTransform, new Vector2(0.5f, 1f), new Vector2(400f, 250f), new Vector2(0f, -88f));
            charHighlight.transform.SetSiblingIndex(0);
            column.RowHighlight[ROW_CHARACTER] = charHighlight;

            column.ReadyButton = UiFactory.MakeButton(filled, "Ready", "ui.ready", ButtonStyle.Green, new Vector2(300f, 70f), null, 36f);
            UiFactory.Place((RectTransform)column.ReadyButton.transform, new Vector2(1f, 0f), new Vector2(200f, 52f), new Vector2(-24f, 12f));
            column.ReadyLabel = column.ReadyButton.GetComponentInChildren<LocalizedText>();
            int captured = index;
            column.ReadyButton.onClick.AddListener(() => ToggleReady(columns[captured]));
            column.ReadyButton.navigation = new Navigation { mode = Navigation.Mode.None };

            column.ReadyBadge = UiFactory.Label(filled, "Bot", "select.bot", 34f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold).gameObject;
            UiFactory.Place(column.ReadyBadge.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(200f, 52f), new Vector2(30f, 12f));

            column.TakenNote = UiFactory.Label(filled, "Taken", "select.taken", 24f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).gameObject;
            UiFactory.Place(column.TakenNote.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(300f, 36f), new Vector2(0f, -290f));
            column.TakenNote.SetActive(false);
            return column;
        }

        private static void BuildRow(Transform parent, Column column, int row, string name, string iconSprite, float yTop, out Image icon, out LocalizedText value)
        {
            var background = UiFactory.Panel(parent, name + "Row", "Panel_Inset");
            TopRow(background.rectTransform, yTop, 48f, 0f, 1f, 30f, 30f);
            background.raycastTarget = false;
            column.RowHighlight[row] = background;
            icon = UiFactory.Picture(background.transform, "Icon", iconSprite, Color.white);
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 40f), new Vector2(16f, 0f));
            value = UiFactory.Label(background.transform, "Value", "shop.hat.zoo_cap", 26f, TextAlignmentOptions.MidlineLeft, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(value.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(66f, 2f, 14f, 2f));
        }

        // Franja horizontal que arranca a 'top' px del borde de arriba; xMin/xMax en fracción del ancho, márgenes en px.
        private static void TopRow(RectTransform rect, float top, float height, float xMin = 0f, float xMax = 1f, float padLeft = 20f, float padRight = 20f)
        {
            rect.anchorMin = new Vector2(xMin, 1f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(padLeft, -top - height);
            rect.offsetMax = new Vector2(-padRight, -top);
        }

        private void BuildBar()
        {
            var row = UiFactory.HorizontalList(Root, "Bar", 18f, TextAnchor.MiddleCenter);
            UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 0f), new Vector2(1840f, 100f), new Vector2(0f, 24f));
            var size = new Vector2(330f, 92f);
            random = UiFactory.MakeButton(row.transform, "Random", "ui.random", ButtonStyle.Blue, size, RandomizeLeader, 34f);
            addBot = UiFactory.MakeButton(row.transform, "AddBot", "select.add_bot", ButtonStyle.Blue, size, () => AddBot(), 34f);
            removeBot = UiFactory.MakeButton(row.transform, "RemoveBot", "select.remove_bot", ButtonStyle.Blue, size, () => RemoveBot(), 34f);
            difficultyButton = UiFactory.MakeButton(row.transform, "Difficulty", null, ButtonStyle.Green, new Vector2(520f, 92f), CycleDifficulty, 34f);
            difficultyLabel = UiFactory.Label(difficultyButton.transform, "Label", "stat.difficulty", 32f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            UiFactory.Region(difficultyLabel.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(24f, 22f, 24f, 12f));
            back = UiFactory.MakeButton(row.transform, "Back", "ui.back", ButtonStyle.Red, new Vector2(280f, 92f), LeaveLobby, 34f);
            bar = new[] { random, addBot, removeBot, difficultyButton, back };
            foreach (var button in bar)
                button.navigation = new Navigation { mode = Navigation.Mode.None };

            status = UiFactory.Label(Root, "Status", "select.waiting", 36f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Place(status.Label.rectTransform, new Vector2(0.5f, 0f), new Vector2(1500f, 56f), new Vector2(0f, 132f));
        }

        private static Color PlayerColor(int index)
        {
            var palette = GameCatalog.Instance != null ? GameCatalog.Instance.playerPalette : null;
            return palette != null && palette.colors != null && palette.colors.Length > 0 ? palette.Get(index) : Color.white;
        }

        // ---------------------------------------------------------------- ciclo de vida
        public override void OnShow()
        {
            proceeding = false;
            startTimer = -1f;
            addedBots.Clear();
            barIndex = 0;
            foreach (var column in columns)
            {
                column.Ready = false;
                column.Row = ROW_CHARACTER;
                column.Slot = null;
                column.Character = null;
            }

            if (Services.IsReady && Services.Session != null)
            {
                var session = Services.Session;
                var catalog = GameCatalog.Instance;
                var saved = catalog != null && Services.Progression != null ? catalog.FindDifficulty(Services.Progression.Data.settings.difficulty) : null;
                if (saved != null)
                    session.Difficulty = saved;

                // Los bots de una vuelta anterior no se heredan.
                for (int i = session.Players.Count - 1; i >= 0; i--)
                {
                    if (session.Players[i].IsBot)
                        session.RemovePlayer(session.Players[i]);
                }
                foreach (var slot in session.Players)
                    Bind(slot);

                Localization.Changed -= Refresh;
                Localization.Changed += Refresh;
                if (Services.InputHub != null)
                {
                    Services.InputHub.EnableJoining();
                    if (!subscribed)
                    {
                        Services.InputHub.PlayerJoined += OnPlayerJoined;
                        Services.InputHub.PlayerLeft += OnPlayerLeft;
                        subscribed = true;
                    }
                }
            }
            Refresh();
        }

        protected override void Teardown()
        {
            Localization.Changed -= Refresh;
            if (subscribed && Services.IsReady && Services.InputHub != null)
            {
                Services.InputHub.PlayerJoined -= OnPlayerJoined;
                Services.InputHub.PlayerLeft -= OnPlayerLeft;
                subscribed = false;
            }
        }

        public override void OnHide()
        {
            Localization.Changed -= Refresh;
            if (Services.IsReady && Services.InputHub != null)
            {
                Services.InputHub.DisableJoining();
                if (subscribed)
                {
                    Services.InputHub.PlayerJoined -= OnPlayerJoined;
                    Services.InputHub.PlayerLeft -= OnPlayerLeft;
                    subscribed = false;
                }
            }
            SetUiMaps(false);
            if (!proceeding)
                RemoveBots();
        }

        private void OnPlayerJoined(PlayerSlot slot)
        {
            Bind(slot);
            Refresh();
        }

        private void OnPlayerLeft(PlayerSlot slot)
        {
            foreach (var column in columns)
            {
                if (column.Slot == slot)
                {
                    column.Slot = null;
                    column.Character = null;
                    column.Ready = false;
                    column.Row = ROW_CHARACTER;
                }
            }
            startTimer = -1f;
            Refresh();
        }

        // Asigna un lugar de la partida a la columna de su índice y le da el primer personaje libre.
        private void Bind(PlayerSlot slot)
        {
            if (slot.Index < 0 || slot.Index >= COLUMNS)
                return;
            var column = columns[slot.Index];
            column.Slot = slot;
            column.Ready = slot.IsBot;
            column.Arm = ARM_TIME;
            column.Row = ROW_CHARACTER;
            column.Character = slot.Character != null && !IsTaken(slot.Character, column) ? slot.Character : FirstFreeCharacter(column);
            slot.Character = column.Character;
            if (slot.Input != null && !slot.IsBot)
            {
                var map = slot.Input.actions.FindActionMap("UI");
                if (map != null)
                    map.Enable();
            }
        }

        private void SetUiMaps(bool enabled)
        {
            foreach (var column in columns)
            {
                if (column.Slot == null || column.Slot.Input == null || column.IsBot)
                    continue;
                var map = column.Slot.Input.actions.FindActionMap("UI");
                if (map == null)
                    continue;
                if (enabled) map.Enable(); else map.Disable();
            }
        }

        // ---------------------------------------------------------------- personajes
        private CharacterData[] AllCharacters => GameCatalog.Instance != null ? GameCatalog.Instance.characters : new CharacterData[0];

        private bool IsTaken(CharacterData character, Column except)
        {
            foreach (var column in columns)
            {
                if (column != except && column.Character == character && column.Slot != null)
                    return true;
            }
            return false;
        }

        private CharacterData FirstFreeCharacter(Column column)
        {
            foreach (var character in AllCharacters)
            {
                if (!IsTaken(character, column))
                    return character;
            }
            return AllCharacters.Length > 0 ? AllCharacters[0] : null;
        }

        // Siguiente personaje que nadie más tiene (con vuelta al principio).
        public void CycleCharacter(Column column, int direction)
        {
            var all = AllCharacters;
            if (all.Length == 0 || column.Slot == null)
                return;
            int start = System.Array.IndexOf(all, column.Character);
            for (int step = 1; step <= all.Length; step++)
            {
                var candidate = all[((start + direction * step) % all.Length + all.Length) % all.Length];
                if (!IsTaken(candidate, column))
                {
                    SetCharacter(column, candidate);
                    return;
                }
            }
        }

        public void SetCharacter(Column column, CharacterData character)
        {
            column.Character = character;
            if (column.Slot != null)
                column.Slot.Character = character;
            Refresh();
        }

        private void RandomizeLeader()
        {
            var leader = columns[0].Slot != null ? columns[0] : FirstHuman();
            if (leader == null)
                return;
            var free = new List<CharacterData>();
            foreach (var character in AllCharacters)
            {
                if (!IsTaken(character, leader))
                    free.Add(character);
            }
            if (free.Count > 0)
                SetCharacter(leader, free[Random.Range(0, free.Count)]);
        }

        private Column FirstHuman()
        {
            foreach (var column in columns)
            {
                if (column.IsOccupied && !column.IsBot)
                    return column;
            }
            return null;
        }

        // ---------------------------------------------------------------- bots y dificultad
        public bool AddBot()
        {
            if (!Services.IsReady)
                return false;
            Column free = null;
            foreach (var column in columns)
            {
                if (!column.IsOccupied)
                {
                    free = column;
                    break;
                }
            }
            if (free == null)
                return false;
            var character = FirstFreeCharacter(free);
            if (character == null)
                return false;
            var palette = GameCatalog.Instance != null ? GameCatalog.Instance.playerPalette : null;
            var color = palette != null && palette.colors != null && palette.colors.Length > 0 ? palette.Get(free.Index) : Color.white;
            var slot = new PlayerSlot(free.Index, color, null, null) { IsBot = true, Character = character };
            Services.Session.AddPlayer(slot);
            addedBots.Add(slot);
            Bind(slot);
            Refresh();
            return true;
        }

        public bool RemoveBot()
        {
            for (int i = COLUMNS - 1; i >= 0; i--)
            {
                var column = columns[i];
                if (!column.IsBot)
                    continue;
                Services.Session.RemovePlayer(column.Slot);
                addedBots.Remove(column.Slot);
                column.Slot = null;
                column.Character = null;
                column.Ready = false;
                startTimer = -1f;
                Refresh();
                return true;
            }
            return false;
        }

        private void RemoveBots()
        {
            if (!Services.IsReady)
                return;
            foreach (var column in columns)
            {
                if (column.IsBot)
                {
                    Services.Session.RemovePlayer(column.Slot);
                    column.Slot = null;
                }
            }
            addedBots.Clear();
        }

        public void CycleDifficulty()
        {
            var catalog = GameCatalog.Instance;
            if (catalog == null || catalog.difficulties.Length == 0 || !Services.IsReady)
                return;
            int index = System.Array.IndexOf(catalog.difficulties, Services.Session.Difficulty);
            var next = catalog.difficulties[(index + 1) % catalog.difficulties.Length];
            Services.Session.Difficulty = next;
            if (Services.Progression != null)
            {
                Services.Progression.Data.settings.difficulty = next.id;
                Services.Progression.Save();
            }
            Refresh();
        }

        // ---------------------------------------------------------------- listo / salir
        public void ToggleReady(Column column)
        {
            if (!column.IsOccupied || column.IsBot)
                return;
            column.Ready = !column.Ready;
            startTimer = -1f;
            Refresh();
        }

        public void LeaveLobby()
        {
            App.Back();
        }

        private void LeaveColumn(Column column)
        {
            if (column.Slot == null || column.IsBot)
                return;
            // El anfitrión (J1) sale de la pantalla; los demás dejan su lugar.
            if (column.Index == FirstHuman().Index)
            {
                LeaveLobby();
                return;
            }
            if (Services.InputHub != null)
                Services.InputHub.RemovePlayer(column.Slot);
        }

        // ---------------------------------------------------------------- entrada por jugador
        private void Update()
        {
            foreach (var column in columns)
            {
                if (column.Slot == null)
                    continue;
                column.Arm -= Time.unscaledDeltaTime;
                if (column.Slot.Input != null && !column.IsBot && !proceeding && column.Arm <= 0f)
                    Poll(column);
            }

            // Todos listos → cuenta atrás corta y se pasa al siguiente paso.
            if (startTimer >= 0f && !proceeding)
            {
                startTimer += Time.unscaledDeltaTime;
                if (startTimer >= START_DELAY)
                    Proceed();
            }
            UpdateHighlights();
        }

        private void Poll(Column column)
        {
            var input = column.Slot.Input;
            var navigate = input.actions.FindAction("UI/Navigate");
            var submit = input.actions.FindAction("UI/Submit");
            var cancel = input.actions.FindAction("UI/Cancel");
            if (navigate == null || submit == null || cancel == null)
                return;

            var value = navigate.ReadValue<Vector2>();
            int x = value.x > STICK ? 1 : value.x < -STICK ? -1 : 0;
            int y = value.y > STICK ? -1 : value.y < -STICK ? 1 : 0;
            int moveX = Edge(ref column.HeldX, x, column, true);
            int moveY = Edge(ref column.HeldY, y, column, false);
            bool leader = FirstHuman() == column;

            if (moveY != 0)
                MoveRow(column, moveY, leader);
            if (moveX != 0)
                MoveHorizontal(column, moveX, leader);

            if (submit.WasPressedThisFrame())
                Submit(column, leader);
            if (cancel.WasPressedThisFrame())
                CancelPressed(column);
        }

        private int Edge(ref int held, int now, Column column, bool horizontal)
        {
            if (now == 0)
            {
                held = 0;
                return 0;
            }
            if (now != held)
            {
                held = now;
                column.Repeat = Time.unscaledTime + REPEAT_DELAY;
                return now;
            }
            if (Time.unscaledTime >= column.Repeat)
            {
                column.Repeat = Time.unscaledTime + REPEAT_RATE;
                return now;
            }
            return 0;
        }

        private void MoveRow(Column column, int step, bool leader)
        {
            if (column.Ready)
                return;
            int max = leader ? ROW_BAR : ROW_GAS;
            column.Row = Mathf.Clamp(column.Row + step, ROW_CHARACTER, max);
            if (column.Row == ROW_BAR)
                SelectBar();
            else
                ClearBarSelection();
        }

        private void MoveHorizontal(Column column, int step, bool leader)
        {
            if (column.Ready)
                return;
            switch (column.Row)
            {
                case ROW_CHARACTER:
                    CycleCharacter(column, step);
                    break;
                case ROW_HAT:
                    CycleCosmetic(column, CosmeticCategory.Hat, step, true);
                    break;
                case ROW_GAS:
                    CycleCosmetic(column, CosmeticCategory.Gas, step, false);
                    break;
                case ROW_BAR:
                    barIndex = Mathf.Clamp(barIndex + step, 0, bar.Length - 1);
                    SelectBar();
                    break;
            }
        }

        private void Submit(Column column, bool leader)
        {
            if (column.Row == ROW_BAR && leader)
            {
                bar[barIndex].onClick.Invoke();
                return;
            }
            ToggleReady(column);
        }

        private void CancelPressed(Column column)
        {
            if (column.Ready)
            {
                column.Ready = false;
                startTimer = -1f;
                Refresh();
                return;
            }
            if (column.Row == ROW_BAR)
            {
                column.Row = ROW_GAS;
                ClearBarSelection();
                return;
            }
            LeaveColumn(column);
        }

        private void SelectBar()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(bar[barIndex].gameObject);
        }

        private void ClearBarSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // ---------------------------------------------------------------- cosméticos
        public void CycleCosmetic(Column column, CosmeticCategory category, int step, bool allowNone)
        {
            var service = Services.Cosmetics;
            if (service == null || column.Slot == null)
                return;
            var options = new List<CosmeticData>();
            if (allowNone)
                options.Add(null);
            options.AddRange(service.OwnedIn(category));
            if (options.Count <= 1)
                return;
            string equipped = service.Equipped(column.Slot.Index, category);
            int current = options.FindIndex(o => o != null && o.id == equipped);
            if (current < 0)
                current = 0;
            var next = options[((current + step) % options.Count + options.Count) % options.Count];
            service.Equip(column.Slot.Index, category, next);
            Refresh();
        }

        // ---------------------------------------------------------------- avanzar
        private void CheckAllReady()
        {
            int humans = 0;
            bool allReady = true;
            foreach (var column in columns)
            {
                if (!column.IsOccupied || column.IsBot)
                    continue;
                humans++;
                allReady &= column.Ready;
            }
            if (humans > 0 && allReady)
            {
                if (startTimer < 0f)
                    startTimer = 0f;
            }
            else
            {
                startTimer = -1f;
            }
            status.SetKey(humans > 0 && allReady ? "select.all_ready" : "select.waiting");
        }

        public void Proceed()
        {
            if (proceeding)
                return;
            proceeding = true;
            SetUiMaps(false);
            foreach (var column in columns)
            {
                if (column.Slot == null)
                    continue;
                column.Slot.Character = column.Character;
                column.Slot.IsReady = true;
                if (Services.Cosmetics != null)
                    Services.Cosmetics.Apply(column.Slot);
            }
            App.Push<WorldMapScreen>();
        }

        // ---------------------------------------------------------------- pintar
        public void Refresh()
        {
            var catalog = GameCatalog.Instance;
            foreach (var column in columns)
            {
                bool occupied = column.IsOccupied;
                column.Empty.SetActive(!occupied);
                column.Filled.SetActive(occupied);
                column.Ring.gameObject.SetActive(occupied);
                if (!occupied)
                    continue;

                var character = column.Character;
                column.Tag.SetLiteral("J" + (column.Index + 1));
                column.ReadyButton.gameObject.SetActive(!column.IsBot);
                column.ReadyBadge.SetActive(column.IsBot);
                column.ReadyLabel.SetKey("ui.ready");
                column.ReadyButton.image.color = column.Ready ? new Color(0.65f, 1f, 0.65f) : Color.white;

                if (character != null)
                {
                    column.Badge.sprite = UiFactory.Sprite("Portrait_" + character.id);
                    column.Badge.enabled = column.Badge.sprite != null;
                    column.Pose.sprite = UiFactory.Sprite("Select_" + character.id);
                    column.Pose.enabled = column.Pose.sprite != null;
                    column.Name.SetKey(character.NameKey);
                    column.Role.SetKey(character.KeyBase + ".role");
                    column.Ability.SetKey(character.KeyBase + ".ability_name");
                    column.Difficulty.SetKey(DifficultyKey(character.id));
                    SetBar(column, 0, character.gasCapacity);
                    SetBar(column, 1, character.puffImpulse);
                    SetBar(column, 2, character.blastForce);
                    SetBar(column, 3, character.regenPerSecond);
                    SetBar(column, 4, character.weight);
                }
                RefreshCosmeticRow(column, CosmeticCategory.Hat, column.HatIcon, column.HatValue, "Icon_Hat_H01_ZooCap", true);
                RefreshCosmeticRow(column, CosmeticCategory.Gas, column.GasIcon, column.GasValue, "Icon_Gas_G01_Classic", false);
            }

            if (difficultyLabel != null && Services.IsReady && Services.Session.Difficulty != null)
                difficultyLabel.SetKey("select.difficulty_format", Localized(Services.Session.Difficulty.NameKey));
            removeBot.interactable = BotCount > 0;
            addBot.interactable = FreeSlots() > 0;
            CheckAllReady();
        }

        private static string Localized(string key) => LocalizedText.Table != null ? LocalizedText.Table.Get(key) : key;

        private int FreeSlots()
        {
            int free = 0;
            foreach (var column in columns)
                free += column.IsOccupied ? 0 : 1;
            return free;
        }

        private void SetBar(Column column, int stat, float value)
        {
            column.Bars[stat].fillAmount = statMax[stat] > 0f ? Mathf.Clamp01(value / statMax[stat]) : 0f;
        }

        private static string DifficultyKey(string characterId)
        {
            switch (characterId)
            {
                case "Alex": return "char.diff.easy";
                case "Julien": return "char.diff.hard";
                default: return "char.diff.medium";
            }
        }

        private void RefreshCosmeticRow(Column column, CosmeticCategory category, Image icon, LocalizedText value, string fallbackIcon, bool allowNone)
        {
            var service = Services.Cosmetics;
            var catalog = GameCatalog.Instance;
            string equipped = service != null && column.Slot != null ? service.Equipped(column.Slot.Index, category) : string.Empty;
            var item = catalog != null && !string.IsNullOrEmpty(equipped) ? catalog.FindCosmetic(equipped) : null;
            if (item != null)
            {
                icon.sprite = item.icon;
                icon.enabled = item.icon != null;
                value.SetKey(item.nameKey);
            }
            else
            {
                icon.enabled = false;
                value.SetKey("select.none");
            }
        }

        private void UpdateHighlights()
        {
            foreach (var column in columns)
            {
                if (!column.IsOccupied)
                    continue;
                bool human = !column.IsBot;
                for (int row = 0; row < 3; row++)
                {
                    var highlight = column.RowHighlight[row];
                    bool active = human && column.Row == row && !column.Ready;
                    if (row == ROW_CHARACTER)
                    {
                        highlight.color = active ? new Color(1f, 0.85f, 0.3f, 0.3f) : new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        highlight.color = active ? new Color(1f, 0.88f, 0.45f, 1f) : Color.white;
                    }
                }
                column.TakenNote.SetActive(false);
            }
        }
    }
}
