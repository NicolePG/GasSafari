using System;
using GasSafari.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Pantalla de resultados (docs/07 §2): tiempo, estrellas animadas, mangos, frijolitos, 3 premios, repetición
    // "¿Quién fue?" y los botones Siguiente / Repetir / Mapa.
    public class ResultsScreen : MonoBehaviour
    {
        private const float STAR_INTERVAL = 0.4f;
        private const float STAR_POP_TIME = 0.25f;

        [SerializeField] private ResultsController controller;
        [SerializeField] private LevelManager level;
        [SerializeField] private UIStrings strings;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image[] stars = new Image[3];
        [SerializeField] private TMP_Text mangoText;
        [SerializeField] private TMP_Text beansText;
        [SerializeField] private TMP_Text[] awardTexts = new TMP_Text[3];
        [SerializeField] private TMP_Text replayTitle;
        [SerializeField] private ReplayPlayer replay;
        [SerializeField] private Button next;
        [SerializeField] private Button retry;
        [SerializeField] private Button map;
        [SerializeField] private Color starOn = new Color(1f, 0.82f, 0.1f);
        [SerializeField] private Color starOff = new Color(0.3f, 0.3f, 0.3f);

        private int starsToShow;
        private int starsShown;
        private float starTimer;
        private float popTimer = float.MaxValue;

        public bool IsVisible => panel.activeSelf;
        public int StarsRevealed => starsShown;

        // Los conecta el cargador de escenas (M6-03).
        public event Action NextRequested;
        public event Action RetryRequested;
        public event Action MapRequested;

        private void Awake()
        {
            panel.SetActive(false);
            next.onClick.AddListener(() => NextRequested?.Invoke());
            retry.onClick.AddListener(() => RetryRequested?.Invoke());
            map.onClick.AddListener(() => MapRequested?.Invoke());
        }

        private void OnEnable()
        {
            controller.ResultsReady += Show;
        }

        private void OnDisable()
        {
            controller.ResultsReady -= Show;
        }

        public void Show(LevelResults results)
        {
            panel.SetActive(true);
            int total = (int)results.Time;
            timeText.SetText("{0:0}:{1:00}", total / 60, total % 60);
            mangoText.SetText(strings.Get("hud.mangos"), results.Mangos, results.MangoTotal);
            beansText.SetText(strings.Get("results.beans"), results.Beans);

            starsToShow = results.Stars;
            starsShown = 0;
            starTimer = 0f;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = starOff;
                stars[i].transform.localScale = Vector3.one;
            }
            ShowAwards(results);
            ShowReplay(results);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(next.gameObject);
        }

        private void ShowAwards(LevelResults results)
        {
            for (int i = 0; i < awardTexts.Length; i++)
            {
                bool has = i < results.Awards.Count;
                awardTexts[i].gameObject.SetActive(has);
                if (!has)
                    continue;
                var award = results.Awards[i];
                var avatar = level.Avatars[award.Player];
                awardTexts[i].SetText(strings.Get("award." + award.Id) + "  J" + (award.Player + 1));
                awardTexts[i].color = avatar.PlayerColor;
            }
        }

        private void ShowReplay(LevelResults results)
        {
            bool has = results.Replay != null;
            replayTitle.gameObject.SetActive(has);
            if (!has)
            {
                replay.Stop();
                return;
            }
            replayTitle.SetText(strings.Get("results.who"));
            int culprit = results.Replay.Culprit;
            var color = culprit >= 0 ? level.Avatars[culprit].PlayerColor : Color.white;
            replay.Play(results.Replay, color, culprit >= 0 ? "J" + (culprit + 1) : string.Empty);
        }

        private void Update()
        {
            if (!panel.activeSelf || starsShown > starsToShow)
                return;

            starTimer += Time.unscaledDeltaTime;
            if (starsShown < starsToShow && starTimer >= STAR_INTERVAL)
            {
                starTimer = 0f;
                stars[starsShown].color = starOn;
                popTimer = 0f;
                starsShown++;
            }

            // La última estrella revelada entra con escala 0 → 1.2 → 1.
            popTimer += Time.unscaledDeltaTime;
            if (starsShown > 0 && popTimer <= STAR_POP_TIME)
            {
                float t = popTimer / STAR_POP_TIME;
                stars[starsShown - 1].transform.localScale = Vector3.one * (t < 0.6f ? Mathf.Lerp(0f, 1.2f, t / 0.6f) : Mathf.Lerp(1.2f, 1f, (t - 0.6f) / 0.4f));
            }
        }
    }
}
