using System.Collections;
using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Save;
using GasSafari.Team;
using GasSafari.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.Progression
{
    // Los 26 logros (docs/06 §5). Escucha GameEvents y el guardado, desbloquea cada uno una sola vez, lo guarda, avisa con
    // GameEvents.OnAchievementUnlocked y muestra un aviso con su medalla.
    public class AchievementService : MonoBehaviour
    {
        public const int WORLD_LEVELS = 5;
        public const int SPEEDSTER_LEVELS = 10;
        public const int SINK_GOAL = 10;
        public const int ALL_STARS = 93;
        public const int ALL_MANGOS = 93;
        public const int ENDLESS_GOAL = 1000;
        public const int DAILY_GOAL = 7;
        public const int PARTY_GAMES = 5;
        public const string DIFFICULTY_HARDEST = "AmistadesRotas";

        private ProgressionService subscribed;
        private int choppedChambers;
        private LevelManager trackedLevel;
        private AchievementPopup popup;

        private SaveData Data => Services.Progression != null ? Services.Progression.Data : null;

        // Para la rejilla de logros y las pruebas: cuántos hay y cuáles se tienen.
        public bool IsUnlocked(string id) => Data != null && Data.achievements.Contains(id);
        public int UnlockedCount => Data != null ? Data.achievements.Count : 0;

        private void OnEnable()
        {
            GameEvents.OnFart += OnFart;
            GameEvents.OnSync += OnSync;
            GameEvents.OnSuperFart += OnSuperFart;
            GameEvents.OnSynergy += OnSynergy;
            GameEvents.OnRaftSunk += OnRaftSunk;
            GameEvents.OnChamberPopped += OnChamberPopped;
            GameEvents.OnLevelFinished += OnLevelFinished;
            TrySubscribe();
        }

        private void Start() => TrySubscribe();

        private void OnDisable()
        {
            GameEvents.OnFart -= OnFart;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnSuperFart -= OnSuperFart;
            GameEvents.OnSynergy -= OnSynergy;
            GameEvents.OnRaftSunk -= OnRaftSunk;
            GameEvents.OnChamberPopped -= OnChamberPopped;
            GameEvents.OnLevelFinished -= OnLevelFinished;
            if (subscribed != null)
                subscribed.Changed -= Evaluate;
            subscribed = null;
        }

        private void TrySubscribe()
        {
            if (subscribed != null || Services.Progression == null)
                return;
            subscribed = Services.Progression;
            subscribed.Changed += Evaluate;
        }

        private void Update()
        {
            // Un nivel nuevo empieza con la cuenta de cámaras reventadas en cero.
            if (LevelManager.Current != trackedLevel)
            {
                trackedLevel = LevelManager.Current;
                choppedChambers = 0;
            }
        }

        // ---------------------------------------------------------------- eventos
        private void OnFart(PlayerAvatar avatar, FartType type, float impulse) => Unlock("first_fart");

        private void OnSync(int players, float multiplier)
        {
            if (players >= 2) Unlock("duet");
            if (players >= 4) Unlock("orchestra");
        }

        private void OnSuperFart(float power) => Unlock("super_fart");

        private void OnSynergy(string id, bool firstTime)
        {
            switch (id)
            {
                case AbilitySynergySystem.SLING: Unlock("jungle_slingshot"); break;
                case AbilitySynergySystem.EARTHQUAKE: Unlock("earthquake"); break;
                case AbilitySynergySystem.PARTY: Unlock("total_party"); break;
                case AbilitySynergySystem.HEAVY_DUO: Unlock("heavy_duo"); break;
                case AbilitySynergySystem.ROYAL_RESCUE: Unlock("royal_rescue"); break;
            }
        }

        // Un cuadro después, para que StatsService ya haya contado este hundimiento.
        private void OnRaftSunk() => StartCoroutine(CheckSinksNextFrame());

        private IEnumerator CheckSinksNextFrame()
        {
            yield return null;
            var data = Data;
            if (data != null && data.stats.sinks >= SINK_GOAL)
                Unlock("captain_titanic");
        }

        private void OnChamberPopped(int chamber) => choppedChambers++;

        private void OnLevelFinished(float time)
        {
            var level = LevelManager.Current != null ? LevelManager.Current.Data : null;
            var difficulty = Services.IsReady && Services.Session != null ? Services.Session.Difficulty : null;
            HandleLevelFinished(level, choppedChambers, difficulty != null ? difficulty.id : string.Empty);
        }

        // Al cruzar la meta: "Sin un Rasguño" (ninguna cámara reventada) y el registro de niveles en Amistades Rotas.
        public void HandleLevelFinished(LevelData level, int chambersLost, string difficultyId)
        {
            var data = Data;
            if (level == null || data == null)
                return;
            if (chambersLost == 0 && level.showTimer)
                Unlock("not_a_scratch");
            if (difficultyId == DIFFICULTY_HARDEST && !data.brokenLevels.Contains(level.id))
                data.brokenLevels.Add(level.id);
        }

        // ---------------------------------------------------------------- estado del guardado
        // Mundos terminados, estrellas, mangos, Velocista, Amistades Rotas, Constante y Rey de la Balsa.
        public void Evaluate()
        {
            var data = Data;
            var catalog = GameCatalog.Instance;
            if (data == null)
                return;

            if (catalog != null)
            {
                for (int w = 0; w < catalog.worlds.Length && w < 6; w++)
                {
                    var world = catalog.worlds[w];
                    int finished = 0, perfect = 0;
                    foreach (var level in world.levels)
                    {
                        var record = data.FindLevel(level.id);
                        if (record == null)
                            continue;
                        if (record.stars > 0) finished++;
                        if (record.stars >= 3) perfect++;
                    }
                    if (world.levels.Count > 0 && finished >= world.levels.Count)
                        Unlock("world" + (w + 1));
                    if (world.levels.Count > 0 && perfect >= world.levels.Count)
                        Unlock("perfectionist");
                }
            }

            if (ProgressionRules.TotalStars(data) >= ALL_STARS) Unlock("constellation");
            if (ProgressionRules.TotalMangos(data) >= ALL_MANGOS) Unlock("royal_collector");

            int threeStars = 0;
            foreach (var record in data.levels)
            {
                if (record.stars >= 3)
                    threeStars++;
            }
            if (threeStars >= SPEEDSTER_LEVELS) Unlock("speedster");

            if (BrokenStoryDone(data, catalog)) Unlock("broken_friendships");
            if (data.partyPlayed.Count >= PARTY_GAMES) Unlock("party_animal");
            if (data.endlessMeters >= ENDLESS_GOAL) Unlock("kilometer");
            if (data.daily.streak >= DAILY_GOAL) Unlock("consistent");
            if (ProgressionRules.HasFinished(data, CosmeticsService.FINISH_LEVEL_ID)) Unlock("raft_king");
        }

        // Todos los niveles de la historia principal (mundos 1 a 5) terminados en Amistades Rotas.
        public static bool BrokenStoryDone(SaveData data, GameCatalog catalog)
        {
            if (catalog == null)
                return false;
            for (int w = 0; w < catalog.worlds.Length && w < 5; w++)
            {
                foreach (var level in catalog.worlds[w].levels)
                {
                    if (!data.brokenLevels.Contains(level.id))
                        return false;
                }
            }
            return catalog.worlds.Length >= 5;
        }

        // Progreso de un logro en curso (actual, meta); meta 0 = sin contador.
        public (int current, int goal) Progress(AchievementData achievement)
        {
            var data = Data;
            if (data == null || achievement == null)
                return (0, 0);
            switch (achievement.kind)
            {
                case AchievementKind.Sinks: return (Mathf.Min(data.stats.sinks, achievement.value), achievement.value);
                case AchievementKind.TotalStars: return (Mathf.Min(ProgressionRules.TotalStars(data), achievement.value), achievement.value);
                case AchievementKind.TotalMangos: return (Mathf.Min(ProgressionRules.TotalMangos(data), achievement.value), achievement.value);
                case AchievementKind.ThreeStarLevels:
                {
                    int count = 0;
                    foreach (var record in data.levels)
                        count += record.stars >= 3 ? 1 : 0;
                    return (Mathf.Min(count, achievement.value), achievement.value);
                }
                case AchievementKind.EndlessMeters: return (Mathf.Min(data.endlessMeters, achievement.value), achievement.value);
                case AchievementKind.DailyStreak: return (Mathf.Min(data.daily.streak, achievement.value), achievement.value);
                case AchievementKind.PartyPlayed: return (Mathf.Min(data.partyPlayed.Count, achievement.value), achievement.value);
                case AchievementKind.WorldFinished:
                {
                    var catalog = GameCatalog.Instance;
                    int finished = 0, total = 0;
                    if (catalog != null)
                    {
                        foreach (var world in catalog.worlds)
                        {
                            if (world.id != achievement.target)
                                continue;
                            total = world.levels.Count;
                            foreach (var level in world.levels)
                                finished += ProgressionRules.HasFinished(data, level.id) ? 1 : 0;
                        }
                    }
                    return (finished, total);
                }
            }
            return (0, 0);
        }

        // ---------------------------------------------------------------- API para los modos
        public void RecordPartyGame(string gameId)
        {
            var data = Data;
            if (data == null || string.IsNullOrEmpty(gameId) || data.partyPlayed.Contains(gameId))
                return;
            data.partyPlayed.Add(gameId);
            Services.Progression.Save();
            Evaluate();
        }

        public void RecordEndlessDistance(int meters)
        {
            var data = Data;
            if (data == null || meters <= data.endlessMeters)
                return;
            data.endlessMeters = meters;
            Services.Progression.Save();
            Evaluate();
        }

        // ---------------------------------------------------------------- desbloquear
        public bool Unlock(string id)
        {
            var data = Data;
            if (data == null || data.achievements.Contains(id))
                return false;
            data.achievements.Add(id);
            Services.Progression.Save();
            GameEvents.RaiseAchievementUnlocked(id);
            ShowPopup(id);
            return true;
        }

        private void ShowPopup(string id)
        {
            var catalog = GameCatalog.Instance;
            var data = catalog != null ? catalog.FindAchievement(id) : null;
            if (data == null)
                return;
            if (popup == null)
                popup = AchievementPopup.Create(transform);
            popup.Enqueue(data);
        }
    }

    // Aviso en una esquina con la medalla y el nombre del logro. Una cola: si caen varios, se muestran uno tras otro.
    public class AchievementPopup : MonoBehaviour
    {
        private const float SHOW_TIME = 3.2f;

        private readonly Queue<AchievementData> queue = new Queue<AchievementData>();
        private CanvasGroup group;
        private Image icon;
        private LocalizedText title;
        private LocalizedText nameText;
        private bool running;

        public bool IsShowing => running;

        public static AchievementPopup Create(Transform parent)
        {
            var canvas = UiFactory.CreateCanvas("AchievementPopup", 150, parent);
            var popup = canvas.gameObject.AddComponent<AchievementPopup>();
            popup.Build(canvas.transform);
            return popup;
        }

        private void Build(Transform canvas)
        {
            var panel = UiFactory.Panel(canvas, "Panel", "Panel_Dark");
            UiFactory.Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(760f, 170f), new Vector2(-40f, -40f));
            panel.raycastTarget = false;
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            icon = UiFactory.Picture(panel.transform, "Medal", (Sprite)null, Color.white);
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 120f), new Vector2(26f, 0f));
            title = UiFactory.Label(panel.transform, "Title", "ach.unlocked", 30f, TextAlignmentOptions.Left, UiFactory.Gold, FontStyles.Bold);
            UiFactory.Region(title.Label.rectTransform, new Vector2(0.22f, 0.58f), Vector2.one, new Vector4(10f, 0f, 24f, 14f));
            nameText = UiFactory.Label(panel.transform, "Name", "ach.first_fart.name", 40f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);
            UiFactory.Region(nameText.Label.rectTransform, Vector2.zero, new Vector2(1f, 0.6f), new Vector4(172f, 12f, 24f, 0f));
        }

        public void Enqueue(AchievementData data)
        {
            queue.Enqueue(data);
            if (!running)
                StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            running = true;
            while (queue.Count > 0)
            {
                var data = queue.Dequeue();
                icon.sprite = data.icon;
                icon.enabled = data.icon != null;
                nameText.SetKey(data.NameKey);
                for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
                {
                    group.alpha = t / 0.3f;
                    yield return null;
                }
                group.alpha = 1f;
                yield return new WaitForSecondsRealtime(SHOW_TIME);
                for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
                {
                    group.alpha = 1f - t / 0.4f;
                    yield return null;
                }
                group.alpha = 0f;
            }
            running = false;
        }
    }
}
