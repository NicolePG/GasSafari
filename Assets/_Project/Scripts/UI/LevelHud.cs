using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.UI
{
    // HUD del nivel (docs/07 §3). Se enlaza una vez con el LevelManager y después todo se actualiza por eventos.
    // Corre después del resto de sistemas del nivel para encontrar la balsa y los checkpoints ya listos.
    [DefaultExecutionOrder(100)]
    public class LevelHud : MonoBehaviour
    {
        private const float PANEL_MARGIN = 24f;
        private const float PANEL_HEIGHT = 150f;
        private const int MAX_COUNTDOWN_TEXTS = 4;

        [SerializeField] private UIStrings strings;
        [SerializeField] private HudTimer timer;
        [SerializeField] private HudChambers chambers;
        [SerializeField] private HudProgress progress;
        [SerializeField] private HudEventText eventText;
        [SerializeField] private RectTransform playersRoot;
        [SerializeField] private HudPlayerPanel panelPrefab;
        [SerializeField] private AvatarOverlay overlayPrefab;

        private readonly string[] countdownTexts = new string[MAX_COUNTDOWN_TEXTS];
        private readonly System.Collections.Generic.List<GameObject> playerWidgets = new System.Collections.Generic.List<GameObject>();
        private LevelManager level;

        public HudEventText EventText => eventText;
        public HudProgress Progress => progress;
        public HudChambers Chambers => chambers;
        public UIStrings Strings => strings;

        private void Awake()
        {
            for (int i = 0; i < countdownTexts.Length; i++)
                countdownTexts[i] = i.ToString();
        }

        private void Start()
        {
            if (LevelManager.Current != null && LevelManager.Current.Raft != null)
                Bind(LevelManager.Current);
        }

        private void OnDestroy()
        {
            GameEvents.OnSync -= OnSync;
            GameEvents.OnSynergy -= OnSynergy;
            GameEvents.OnCheckpoint -= OnCheckpoint;
            GameEvents.OnHazardWarning -= OnHazardWarning;
            if (level != null)
                level.CountdownTick -= OnCountdownTick;
        }

        public void Bind(LevelManager manager)
        {
            level = manager;
            var checkpoints = FindFirstObjectByType<CheckpointSystem>();
            timer.Bind(level, strings);
            chambers.Bind(level);
            progress.Bind(level, checkpoints, strings);
            BuildPlayers(level);

            level.CountdownTick += OnCountdownTick;
            GameEvents.OnSync += OnSync;
            GameEvents.OnSynergy += OnSynergy;
            GameEvents.OnCheckpoint += OnCheckpoint;
            GameEvents.OnHazardWarning += OnHazardWarning;
        }

        private void BuildPlayers(LevelManager manager)
        {
            var avatars = manager.Avatars;
            for (int i = 0; i < avatars.Count; i++)
                AddPlayer(avatars[i], i, i + 1);
        }

        // Carrera por Equipos: rehace los paneles con los animales de las dos balsas. place: 0 y 2 a la izquierda, 1 y 3 a
        // la derecha (cada equipo en su mitad de la pantalla); number: el número de jugador del panel.
        public void ShowPlayers(System.Collections.Generic.IReadOnlyList<PlayerAvatar> avatars,
            System.Collections.Generic.IReadOnlyList<int> places, System.Collections.Generic.IReadOnlyList<int> numbers)
        {
            foreach (var widget in playerWidgets)
            {
                if (widget != null)
                    Destroy(widget);
            }
            playerWidgets.Clear();
            for (int i = 0; i < avatars.Count; i++)
                AddPlayer(avatars[i], places[i], numbers[i]);
        }

        private void AddPlayer(PlayerAvatar avatar, int place, int number)
        {
            var panel = Instantiate(panelPrefab, playersRoot);
            PlacePanel((RectTransform)panel.transform, place);
            panel.Bind(avatar, number);
            playerWidgets.Add(panel.gameObject);

            var overlay = Instantiate(overlayPrefab, avatar.transform);
            overlay.Bind(avatar, strings);
            playerWidgets.Add(overlay.gameObject);
        }

        // Jugadores 1 y 3 a la izquierda, 2 y 4 a la derecha; los pares suben una fila.
        private static void PlacePanel(RectTransform rect, int index)
        {
            bool left = index % 2 == 0;
            var anchor = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            float row = index / 2;
            rect.anchoredPosition = new Vector2(left ? PANEL_MARGIN : -PANEL_MARGIN, PANEL_MARGIN + row * (PANEL_HEIGHT + PANEL_MARGIN));
        }

        // 3, 2, 1 y 0 = ¡YA!
        private void OnCountdownTick(int value)
        {
            eventText.Show(value > 0 && value < countdownTexts.Length ? countdownTexts[value] : strings.Get("hud.go"), true);
        }

        private void OnSync(int players, float multiplier)
        {
            if (players >= 2 && players <= GameSession.MAX_PLAYERS)
                eventText.Show(strings.Get("hud.sync" + players));
        }

        private void OnSynergy(string id, bool firstTime)
        {
            if (firstTime)
                eventText.Show(strings.Get("synergy." + id));
        }

        private void OnCheckpoint(int index) => eventText.Show(strings.Get("hud.checkpoint"));

        // Aviso de peligro del abismo H22, 30 m antes del hueco (el borde rojo lo pone el propio aviso).
        private void OnHazardWarning(Hazards.HazardBase hazard)
        {
            if (hazard is Hazards.AbyssWarning)
                eventText.Show(strings.Get("hud.abyss"));
        }
    }
}
