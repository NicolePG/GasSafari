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
        private LevelManager level;

        public HudEventText EventText => eventText;
        public HudProgress Progress => progress;

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
        }

        private void BuildPlayers(LevelManager manager)
        {
            var avatars = manager.Avatars;
            for (int i = 0; i < avatars.Count; i++)
            {
                var panel = Instantiate(panelPrefab, playersRoot);
                PlacePanel((RectTransform)panel.transform, i);
                panel.Bind(avatars[i], i + 1);

                var overlay = Instantiate(overlayPrefab, avatars[i].transform);
                overlay.Bind(avatars[i], strings);
            }
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
    }
}
