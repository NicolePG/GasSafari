using System;
using GasSafari.Core;
using GasSafari.Team;
using GasSafari.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.Level
{
    // Ayuda Real (docs/06 §3): tras 3 reapariciones en el mismo checkpoint aparece una oferta con tres ayudas:
    // 1) ventana de sync +0.1 s hasta el siguiente checkpoint, 2) un checkpoint extra a mitad de camino del siguiente,
    // 3) saltar la sección (el nivel queda con máximo 1 estrella). Se puede rechazar. Solo se ofrece si hay jugadores humanos.
    public class RoyalAssist : MonoBehaviour
    {
        public const int RESPAWNS_FOR_OFFER = 3;
        public const float SYNC_BONUS = 0.1f;
        public const float EXTRA_CHECKPOINT_FRACTION = 0.5f;

        public enum Option
        {
            MoreSyncTime = 1,
            ExtraCheckpoint = 2,
            SkipSection = 3
        }

        private CheckpointSystem checkpoints;
        private LevelManager level;
        private SyncDetector sync;
        private int offeredAt = -1;
        private bool pausedByOffer;
        private bool syncBonusActive;
        private AssistPanel panel;

        public bool IsOffering { get; private set; }
        public bool SectionSkipped { get; private set; }
        public int OffersMade { get; private set; }
        public AssistPanel Panel => panel;

        public event Action OfferOpened;
        public event Action OfferClosed;

        public void Bind(LevelManager levelManager, CheckpointSystem system)
        {
            level = levelManager;
            checkpoints = system;
        }

        private void OnEnable()
        {
            GameEvents.OnRespawn += OnRespawn;
            GameEvents.OnCheckpoint += OnCheckpoint;
        }

        private void OnDisable()
        {
            GameEvents.OnRespawn -= OnRespawn;
            GameEvents.OnCheckpoint -= OnCheckpoint;
            if (IsOffering)
                Close();
        }

        private SyncDetector Sync
        {
            get
            {
                if (sync == null && level != null && level.Raft != null)
                    sync = level.Raft.GetComponent<SyncDetector>();
                return sync;
            }
        }

        private static bool HasHumans()
        {
            if (!Services.IsReady)
                return false;
            foreach (var slot in Services.Session.Players)
            {
                if (!slot.IsBot)
                    return true;
            }
            return false;
        }

        // Las ayudas posibles ahora: sin siguiente checkpoint no hay extra ni salto.
        public bool IsAvailable(Option option)
        {
            switch (option)
            {
                case Option.MoreSyncTime: return Sync != null;
                default: return checkpoints != null && checkpoints.CurrentIndex + 1 < checkpoints.Active.Count;
            }
        }

        private void OnRespawn()
        {
            if (checkpoints == null || IsOffering || !HasHumans())
                return;
            if (checkpoints.RespawnsAtCurrent < RESPAWNS_FOR_OFFER || offeredAt == checkpoints.CurrentIndex)
                return;
            offeredAt = checkpoints.CurrentIndex;
            Open();
        }

        // La ventana extra dura hasta llegar al siguiente checkpoint.
        private void OnCheckpoint(int index)
        {
            if (syncBonusActive && Sync != null)
                Sync.SetWindowBonus(SyncBonusSource.RoyalHelp, 0f, 0f);
            syncBonusActive = false;
        }

        public void Open()
        {
            if (IsOffering)
                return;
            IsOffering = true;
            OffersMade++;
            if (Services.IsReady && !Services.Session.IsOnline)
            {
                Services.TimeController.SetPaused(true);
                if (level != null)
                    level.SetPaused(true);
                pausedByOffer = true;
            }
            if (panel == null)
                panel = AssistPanel.Create(this);
            panel.Show();
            OfferOpened?.Invoke();
        }

        private void Close()
        {
            IsOffering = false;
            if (panel != null)
                panel.Hide();
            if (pausedByOffer && Services.IsReady)
            {
                if (level != null)
                    level.SetPaused(false);
                Services.TimeController.SetPaused(false);
            }
            pausedByOffer = false;
            OfferClosed?.Invoke();
        }

        public void Decline() => Close();

        public bool Choose(Option option)
        {
            if (!IsAvailable(option))
                return false;
            Close();
            switch (option)
            {
                case Option.MoreSyncTime:
                    Sync.SetWindowBonus(SyncBonusSource.RoyalHelp, SYNC_BONUS, float.MaxValue);
                    syncBonusActive = true;
                    break;
                case Option.ExtraCheckpoint:
                    checkpoints.InsertExtraAhead(EXTRA_CHECKPOINT_FRACTION);
                    break;
                case Option.SkipSection:
                    if (checkpoints.SkipToNext())
                    {
                        SectionSkipped = true;
                        var stats = FindFirstObjectByType<LevelStats>();
                        if (stats != null)
                            stats.Data.SectionSkipped = true;
                    }
                    break;
            }
            return true;
        }
    }

    // El panel de la oferta: tres ayudas y "No, gracias". Se maneja con mando, teclado o ratón.
    public class AssistPanel : MonoBehaviour
    {
        private RoyalAssist owner;
        private GameObject root;
        private readonly Button[] options = new Button[3];
        private Button decline;

        public Button[] OptionButtons => options;
        public Button DeclineButton => decline;
        public bool IsVisible => root != null && root.activeSelf;

        public static AssistPanel Create(RoyalAssist assist)
        {
            var canvas = UiFactory.CreateCanvas("AssistOffer", 30, assist.transform);
            var panel = canvas.gameObject.AddComponent<AssistPanel>();
            panel.owner = assist;
            panel.Build(canvas.transform);
            return panel;
        }

        private void Build(Transform canvas)
        {
            root = UiFactory.Backdrop(canvas, "Root").gameObject;
            var box = UiFactory.Panel(root.transform, "Box", "Panel_Light");
            UiFactory.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1100f, 760f), Vector2.zero);
            var title = UiFactory.Label(box.transform, "Title", "assist.offer", 56f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(title.Label.rectTransform, new Vector2(0f, 0.82f), Vector2.one, new Vector4(40f, 0f, 40f, 24f));
            string[] keys = { "assist.opt1", "assist.opt2", "assist.opt3" };
            for (int i = 0; i < 3; i++)
            {
                var option = (RoyalAssist.Option)(i + 1);
                options[i] = UiFactory.MakeButton(box.transform, "Option" + (i + 1), keys[i], ButtonStyle.Blue, new Vector2(960f, 120f), () => owner.Choose(option), 36f);
                UiFactory.Place((RectTransform)options[i].transform, new Vector2(0.5f, 1f), new Vector2(960f, 120f), new Vector2(0f, -150f - i * 138f));
            }
            decline = UiFactory.MakeButton(box.transform, "Decline", "assist.decline", ButtonStyle.Red, new Vector2(520f, 100f), owner.Decline, 38f);
            UiFactory.Place((RectTransform)decline.transform, new Vector2(0.5f, 0f), new Vector2(520f, 100f), new Vector2(0f, 36f));
            UiFactory.LinkVertical(options[0], options[1], options[2], decline);
            root.AddComponent<DeclineOnCancel>().Owner = owner;
            root.SetActive(false);
        }

        private class DeclineOnCancel : MonoBehaviour, ICancelHandler
        {
            public RoyalAssist Owner;

            public void OnCancel(BaseEventData eventData)
            {
                Owner.Decline();
                eventData.Use();
            }
        }

        public void Show()
        {
            root.SetActive(true);
            for (int i = 0; i < options.Length; i++)
                options[i].interactable = owner.IsAvailable((RoyalAssist.Option)(i + 1));
            // El foco empieza en la primera ayuda disponible.
            var first = options[0].interactable ? options[0] : options[1].interactable ? options[1] : decline;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}


