using System.Collections;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Carga asíncrona de escenas con la pantalla de carga de docs/07 §2: fondo del mundo, consejo aleatorio y la balsa que cabecea.
    // Existe una sola, persistente, y se crea sola la primera vez que se pide.
    public class SceneLoader : MonoBehaviour
    {
        public const string MENU_SCENE = "01_Title";
        // Tiempo mínimo en pantalla para que se alcance a leer el consejo.
        public const float MIN_SHOW_TIME = 1.2f;

        private static readonly string[] TipKeys =
        {
            "loading.tip.together", "loading.tip.tap_blast", "loading.tip.hold_on", "loading.tip.gloria",
            "loading.tip.pings", "loading.tip.neck_hook", "loading.tip.beans"
        };

        private static SceneLoader instance;

        private CanvasGroup group;
        private Image background;
        private Image barFill;
        private RectTransform raft;
        private LocalizedText tipText;
        private GameObject tipBox;
        private bool loading;

        public static bool IsLoading => instance != null && instance.loading;

        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SceneLoader");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<SceneLoader>();
                    instance.Build();
                }
                return instance;
            }
        }

        // ---------------------------------------------------------------- API
        public static void LoadLevel(LevelData level)
        {
            if (level == null || IsLoading)
                return;
            if (Services.IsReady)
                Services.Session.CurrentLevel = level;
            LevelFlow.AssistArmed = true;
            LevelFlow.InGameFlow = true;
            var catalog = GameCatalog.Instance;
            var world = catalog != null ? catalog.WorldOf(level) : level.world;
            string backgroundSprite = "Loading_" + (world != null ? world.id : "W1");
            Instance.Begin(level.sceneName, backgroundSprite, withTip: true);
        }

        public static void LoadScene(string sceneName, string backgroundSprite = "Loading_W1")
        {
            if (IsLoading)
                return;
            Instance.Begin(sceneName, backgroundSprite, withTip: true);
        }

        // Vuelve a los menús (por ejemplo al mapa desde un nivel).
        public static void LoadMenu(MenuRequest.Start start, LevelData focus = null)
        {
            if (IsLoading)
                return;
            LevelFlow.InGameFlow = false;
            MenuRequest.StartAt = start;
            MenuRequest.FocusLevel = focus;
            MenuRequest.FocusWorld = focus != null && GameCatalog.Instance != null ? GameCatalog.Instance.WorldOf(focus) : null;
            Instance.Begin(MENU_SCENE, "Loading_W1", withTip: false);
        }

        // Muestra la pantalla de carga sin cargar nada (revisión de capturas y pruebas).
        public static void ShowPreview(string backgroundSprite, float progress = 0.6f)
        {
            var loader = Instance;
            loader.StopAllCoroutines();
            loader.loading = false;
            var sprite = UiFactory.Sprite(backgroundSprite);
            loader.background.sprite = sprite;
            loader.background.color = sprite != null ? Color.white : Color.black;
            loader.tipBox.SetActive(true);
            loader.tipText.SetKey(TipKeys[0]);
            loader.barFill.fillAmount = progress;
            loader.group.alpha = 1f;
        }

        // Pruebas: quita la pantalla de carga persistente.
        public static void Dispose()
        {
            if (instance != null)
                Destroy(instance.gameObject);
            instance = null;
        }

        public static void HidePreview()
        {
            if (instance != null)
                instance.group.alpha = 0f;
        }

        public static string TipText => instance != null ? instance.tipText.Label.text : string.Empty;

        // ---------------------------------------------------------------- construcción
        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("LoadingCanvas", 200, transform);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            background = UiFactory.Picture(canvas.transform, "Background", (Sprite)null, Color.black, false);
            UiFactory.Stretch(background.rectTransform);
            background.raycastTarget = true;
            var shade = UiFactory.Picture(canvas.transform, "Shade", (Sprite)null, new Color(0f, 0f, 0f, 0.38f), false);
            UiFactory.Place(shade.rectTransform, new Vector2(0.5f, 0f), new Vector2(1920f, 330f), Vector2.zero);

            var loadingLabel = UiFactory.Label(canvas.transform, "Loading", "ui.loading", 54f, TextAlignmentOptions.Left, UiFactory.Cream, FontStyles.Bold).Outlined();
            UiFactory.Place(loadingLabel.Label.rectTransform, new Vector2(0f, 0f), new Vector2(560f, 80f), new Vector2(90f, 220f));

            tipBox = UiFactory.Rect("Tip", canvas.transform).gameObject;
            UiFactory.Place((RectTransform)tipBox.transform, new Vector2(0.5f, 0f), new Vector2(1500f, 120f), new Vector2(0f, 90f));
            var tipLabel = UiFactory.Label(tipBox.transform, "TipLabel", "loading.tip_label", 30f, TextAlignmentOptions.Center, UiFactory.Gold, FontStyles.Bold).Outlined();
            UiFactory.Region(tipLabel.Label.rectTransform, new Vector2(0f, 0.62f), Vector2.one);
            tipText = UiFactory.Label(tipBox.transform, "TipText", TipKeys[0], 40f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
            UiFactory.Region(tipText.Label.rectTransform, Vector2.zero, new Vector2(1f, 0.64f));

            var barBack = UiFactory.Picture(canvas.transform, "BarBack", "Meter_Back", Color.white, false);
            barBack.type = Image.Type.Sliced;
            UiFactory.Place(barBack.rectTransform, new Vector2(1f, 0f), new Vector2(760f, 56f), new Vector2(-90f, 230f));
            barFill = UiFactory.Picture(barBack.transform, "BarFill", "Meter_Fill", Color.white, false);
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            UiFactory.Stretch(barFill.rectTransform);

            // La balsa naranja del inicio cabecea sobre la barra.
            var raftImage = UiFactory.Picture(canvas.transform, "Raft", "Icon_Raft_R00_Orange", Color.white);
            raft = raftImage.rectTransform;
            UiFactory.Place(raft, new Vector2(1f, 0f), new Vector2(120f, 120f), new Vector2(-90f - 640f, 276f));
        }

        // ---------------------------------------------------------------- carga
        private void Begin(string sceneName, string backgroundSprite, bool withTip)
        {
            loading = true;
            Time.timeScale = 1f;
            var sprite = UiFactory.Sprite(backgroundSprite);
            background.sprite = sprite;
            background.color = sprite != null ? Color.white : Color.black;
            tipBox.SetActive(withTip);
            tipText.SetKey(TipKeys[Random.Range(0, TipKeys.Length)]);
            barFill.fillAmount = 0f;
            StartCoroutine(Run(sceneName));
        }

        private IEnumerator Run(string sceneName)
        {
            group.blocksRaycasts = true;
            yield return Fade(1f, 0.25f);

            float shown = 0f;
            AsyncOperation operation = null;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                operation = SceneManager.LoadSceneAsync(sceneName);
                operation.allowSceneActivation = false;
            }
            else
            {
                Debug.LogWarning("SceneLoader: la escena '" + sceneName + "' no está en Build Settings");
            }

            while (true)
            {
                shown += Time.unscaledDeltaTime;
                float progress = operation != null ? Mathf.Clamp01(operation.progress / 0.9f) : 1f;
                barFill.fillAmount = Mathf.MoveTowards(barFill.fillAmount, Mathf.Min(progress, shown / MIN_SHOW_TIME), Time.unscaledDeltaTime * 2f);
                raft.anchoredPosition = new Vector2(-90f - (1f - barFill.fillAmount) * 640f, 276f + Mathf.Sin(shown * 5f) * 8f);
                raft.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(shown * 4f) * 6f);
                if (shown >= MIN_SHOW_TIME && progress >= 1f && barFill.fillAmount >= 0.999f)
                    break;
                yield return null;
            }

            if (operation != null)
            {
                operation.allowSceneActivation = true;
                while (!operation.isDone)
                    yield return null;
            }
            yield return null;
            yield return Fade(0f, 0.25f);
            group.blocksRaycasts = false;
            loading = false;
        }

        private IEnumerator Fade(float target, float time)
        {
            float start = group.alpha;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / time);
                yield return null;
            }
            group.alpha = target;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }
    }
}
