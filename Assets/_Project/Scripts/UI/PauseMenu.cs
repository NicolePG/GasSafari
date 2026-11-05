using GasSafari.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Menú de pausa (docs/07 §4). Se abre y cierra con PauseController y se maneja con mando o teclado.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private PauseController controller;
        [SerializeField] private UIStrings strings;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resume;
        [SerializeField] private Button restartCheckpoint;
        [SerializeField] private Button restartLevel;
        [SerializeField] private Button options;
        [SerializeField] private Button controls;
        [SerializeField] private Button quit;
        [SerializeField] private GameObject votePanel;
        [SerializeField] private TMP_Text voteText;
        [SerializeField] private Button voteYes;
        [SerializeField] private Button voteNo;

        public bool IsVisible => panel.activeSelf;

        private void Awake()
        {
            panel.SetActive(false);
            votePanel.SetActive(false);
            resume.onClick.AddListener(controller.Close);
            restartCheckpoint.onClick.AddListener(() => controller.RequestCheckpointRestart(controller.Owner));
            restartLevel.onClick.AddListener(controller.RestartLevel);
            options.onClick.AddListener(controller.OpenOptions);
            controls.onClick.AddListener(controller.OpenControls);
            quit.onClick.AddListener(controller.QuitToMap);
            voteYes.onClick.AddListener(() => controller.CastVote(controller.NextVoter(), true));
            voteNo.onClick.AddListener(() => controller.CastVote(controller.NextVoter(), false));
        }

        private void OnEnable()
        {
            controller.Opened += OnOpened;
            controller.Closed += OnClosed;
            controller.VoteChanged += RefreshVote;
        }

        private void OnDisable()
        {
            controller.Opened -= OnOpened;
            controller.Closed -= OnClosed;
            controller.VoteChanged -= RefreshVote;
        }

        // Primer botón seleccionado al abrir.
        private void OnOpened(Core.PlayerSlot owner)
        {
            panel.SetActive(true);
            votePanel.SetActive(false);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resume.gameObject);
        }

        private void OnClosed()
        {
            panel.SetActive(false);
        }

        private void RefreshVote()
        {
            var vote = controller.Vote;
            bool pending = vote.Result == VoteResult.Pending;
            votePanel.SetActive(pending);
            if (!pending)
                return;
            voteText.SetText(strings.Get("pause.vote"), vote.Yes, vote.No, vote.Voters / 2 + 1);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(voteYes.gameObject);
        }
    }
}
