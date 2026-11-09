using System;
using System.Collections;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Qué pide el resto del juego al volver a los menús (por ejemplo "Mapa" desde los resultados de un nivel).
    public static class MenuRequest
    {
        public enum Start
        {
            Splash,
            MainMenu,
            WorldMap
        }

        public static Start StartAt = Start.Splash;
        // Mundo que el mapa muestra primero (el del nivel que se acaba de jugar).
        public static WorldData FocusWorld;
        public static LevelData FocusLevel;
    }

    // Raíz de los menús (docs/07 §1): el Canvas, el EventSystem, la pila de pantallas, avisos y diálogos de confirmación.
    // Vive en la escena 01_Title, sobre el escenario 3D animado.
    public class MenuApp : MonoBehaviour
    {
        public static MenuApp Current { get; private set; }

        // Una instancia que se abre encima de un nivel (Opciones desde la pausa) en lugar de ser la raíz de los menús.
        private static bool creatingOverlay;
        private bool isOverlay;
        private GameObject previousSelection;
        public bool IsOverlay => isOverlay;
        public event Action OverlayClosed;

        public static MenuApp CreateOverlay()
        {
            creatingOverlay = true;
            var go = new GameObject("MenuOverlay");
            var app = go.AddComponent<MenuApp>();
            creatingOverlay = false;
            return app;
        }

        private readonly Dictionary<Type, MenuScreen> screens = new Dictionary<Type, MenuScreen>();
        private readonly List<MenuScreen> stack = new List<MenuScreen>();
        private RectTransform screensRoot;
        private RectTransform overlayRoot;
        private Image dim;
        private LocalizedText toast;
        private float toastTimer;
        private GameObject confirmRoot;
        private LocalizedText confirmText;
        private Action confirmYes;
        private Button confirmYesButton;
        private Button confirmNoButton;
        private GameObject lastSelected;
        private EventSystem eventSystem;

        public Canvas Canvas { get; private set; }
        public MenuScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public IReadOnlyList<MenuScreen> Stack => stack;
        public bool IsConfirmOpen => confirmRoot != null && confirmRoot.activeSelf;
        public event Action<MenuScreen> ScreenShown;

        private void Awake()
        {
            isOverlay = creatingOverlay;
            if (!isOverlay)
                Current = this;
            BuildCanvas();
            if (isOverlay)
            {
                Canvas.sortingOrder = 60;
                previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            }
            EnsureEventSystem();
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        private IEnumerator Start()
        {
            // Un cuadro para que los servicios (Boot) terminen de arrancar.
            yield return null;
            if (isOverlay)
                yield break;
            LocalizedText.SetTextScale(Services.IsReady && Services.Progression != null ? Services.Progression.Data.settings.textScale : 1f);
            switch (MenuRequest.StartAt)
            {
                case MenuRequest.Start.MainMenu:
                    GoTo<MainMenuScreen>();
                    break;
                case MenuRequest.Start.WorldMap:
                    GoTo<MainMenuScreen>();
                    Push<WorldMapScreen>();
                    break;
                default:
                    GoTo<TitleScreen>();
                    break;
            }
            MenuRequest.StartAt = MenuRequest.Start.MainMenu;
        }

        // ---------------------------------------------------------------- construcción
        private void BuildCanvas()
        {
            Canvas = UiFactory.CreateCanvas("MenuCanvas", 0, transform);
            dim = UiFactory.Backdrop(Canvas.transform, "Dim");
            screensRoot = UiFactory.Rect("Screens", Canvas.transform);
            UiFactory.Stretch(screensRoot);
            overlayRoot = UiFactory.Rect("Overlays", Canvas.transform);
            UiFactory.Stretch(overlayRoot);

            BuildToast();
            BuildConfirm();
        }

        private void EnsureEventSystem()
        {
            eventSystem = EventSystem.current != null ? EventSystem.current : FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                go.transform.SetParent(transform, false);
                eventSystem = go.GetComponent<EventSystem>();
            }
            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            // Mando y teclado: Navegar (flechas, WASD, cruceta, palanca), Aceptar (Enter, Espacio, A) y Cancelar (Esc, B).
            if (module.actionsAsset == null)
                module.AssignDefaultActions();
        }

        public T Get<T>() where T : MenuScreen
        {
            if (screens.TryGetValue(typeof(T), out var existing))
                return (T)existing;

            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(screensRoot, false);
            var screen = go.AddComponent<T>();
            screens[typeof(T)] = screen;
            screen.Init(this);
            go.SetActive(false);
            return screen;
        }

        // ---------------------------------------------------------------- navegación entre pantallas
        public T Push<T>() where T : MenuScreen
        {
            var screen = Get<T>();
            if (Top != null)
            {
                Top.OnHide();
                Top.gameObject.SetActive(false);
            }
            stack.Remove(screen);
            stack.Add(screen);
            Activate(screen);
            return screen;
        }

        // Reemplaza todo el recorrido por una sola pantalla.
        public T GoTo<T>() where T : MenuScreen
        {
            var screen = Get<T>();
            for (int i = 0; i < stack.Count; i++)
            {
                stack[i].OnHide();
                stack[i].gameObject.SetActive(false);
            }
            stack.Clear();
            stack.Add(screen);
            Activate(screen);
            return screen;
        }

        // Cierra una instancia superpuesta y devuelve el foco a lo que lo tenía.
        public void CloseOverlay()
        {
            if (!isOverlay)
                return;
            if (eventSystem != null)
            {
                eventSystem.sendNavigationEvents = true;
                if (previousSelection != null && previousSelection.activeInHierarchy)
                    eventSystem.SetSelectedGameObject(previousSelection);
            }
            OverlayClosed?.Invoke();
            Destroy(gameObject);
        }

        public void Back()
        {
            if (stack.Count <= 1)
            {
                CloseOverlay();
                return;
            }
            var leaving = Top;
            leaving.OnHide();
            leaving.gameObject.SetActive(false);
            stack.RemoveAt(stack.Count - 1);
            Activate(Top);
        }

        private void Activate(MenuScreen screen)
        {
            dim.enabled = screen.DimBackground;
            screen.gameObject.SetActive(true);
            screen.transform.SetAsLastSibling();
            screen.OnShow();
            if (eventSystem != null)
                eventSystem.sendNavigationEvents = !screen.HandlesOwnNavigation;
            StartCoroutine(SelectNextFrame(screen));
            PlayMusicFor(screen);
            ScreenShown?.Invoke(screen);
        }

        private IEnumerator SelectNextFrame(MenuScreen screen)
        {
            yield return null;
            Select(screen.FirstSelected);
        }

        // Música de los menús (docs/08): título, selección de personajes y mapa.
        private void PlayMusicFor(MenuScreen screen)
        {
            if (isOverlay || !Services.IsReady || Services.Music == null)
                return;
            string id = screen is LobbyScreen ? "mus_select" : screen is WorldMapScreen || screen is LevelSheetScreen ? "mus_map" : "mus_title";
            if (Services.Music.CurrentId != id)
                Services.Music.Play(id);
        }

        public void Select(Selectable selectable)
        {
            if (selectable == null || eventSystem == null || !selectable.gameObject.activeInHierarchy)
                return;
            eventSystem.SetSelectedGameObject(selectable.gameObject);
            lastSelected = selectable.gameObject;
        }

        private void Update()
        {
            if (toast != null && toast.gameObject.activeSelf)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f)
                    toast.gameObject.SetActive(false);
            }

            if (eventSystem == null || !eventSystem.sendNavigationEvents)
                return;
            var current = eventSystem.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy)
            {
                lastSelected = current;
                return;
            }
            // Un clic en vacío o un botón que se apagó: devolver el foco para que mando y teclado sigan funcionando.
            GameObject target = IsConfirmOpen ? confirmYesButton.gameObject
                : lastSelected != null && lastSelected.activeInHierarchy ? lastSelected
                : Top != null && Top.FirstSelected != null ? Top.FirstSelected.gameObject : null;
            if (target != null)
                eventSystem.SetSelectedGameObject(target);
        }

        // ---------------------------------------------------------------- aviso breve
        private void BuildToast()
        {
            var panel = UiFactory.Panel(overlayRoot, "ToastPanel", "Panel_Dark");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(1100f, 110f), new Vector2(0f, 70f));
            panel.raycastTarget = false;
            toast = UiFactory.Label(panel.transform, "ToastText", null, 40f, TextAlignmentOptions.Center, UiFactory.Cream, FontStyles.Bold);
            UiFactory.Stretch(toast.Label.rectTransform, 24f);
            panel.gameObject.SetActive(false);
            toastPanel = panel.gameObject;
        }

        private GameObject toastPanel;

        public void Toast(string key, params object[] args)
        {
            toast.SetKey(key, args);
            toastPanel.SetActive(true);
            toastTimer = 2.4f;
        }

        public bool IsToastVisible => toastPanel != null && toastPanel.activeSelf;
        public string ToastText => toast != null ? toast.Label.text : string.Empty;

        // ---------------------------------------------------------------- confirmar
        private void BuildConfirm()
        {
            confirmRoot = UiFactory.Backdrop(overlayRoot, "Confirm").gameObject;
            var box = UiFactory.Panel(confirmRoot.transform, "Box", "Panel_Light");
            UiFactory.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 420f), Vector2.zero);
            confirmText = UiFactory.Label(box.transform, "Message", "menu.quit_confirm", 46f, TextAlignmentOptions.Center, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Region(confirmText.Label.rectTransform, new Vector2(0f, 0.38f), Vector2.one, new Vector4(60f, 0f, 60f, 70f));
            confirmYesButton = UiFactory.MakeButton(box.transform, "Yes", "ui.yes", ButtonStyle.Green, new Vector2(300f, 110f), ConfirmAccepted);
            UiFactory.Place((RectTransform)confirmYesButton.transform, new Vector2(0.5f, 0f), new Vector2(300f, 110f), new Vector2(-180f, 56f));
            confirmNoButton = UiFactory.MakeButton(box.transform, "No", "ui.no", ButtonStyle.Red, new Vector2(300f, 110f), CloseConfirm);
            UiFactory.Place((RectTransform)confirmNoButton.transform, new Vector2(0.5f, 0f), new Vector2(300f, 110f), new Vector2(180f, 56f));
            UiFactory.LinkHorizontal(confirmYesButton, confirmNoButton);
            confirmRoot.AddComponent<ConfirmCancel>().Owner = this;
            confirmRoot.SetActive(false);
        }

        public void Confirm(string messageKey, Action onYes)
        {
            confirmYes = onYes;
            confirmText.SetKey(messageKey);
            confirmRoot.SetActive(true);
            confirmRoot.transform.SetAsLastSibling();
            // El foco empieza en "No": un Aceptar sin querer no borra ni cierra nada.
            StartCoroutine(SelectConfirm());
        }

        private IEnumerator SelectConfirm()
        {
            yield return null;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(confirmNoButton.gameObject);
        }

        private void ConfirmAccepted()
        {
            var action = confirmYes;
            CloseConfirm();
            action?.Invoke();
        }

        public void CloseConfirm()
        {
            confirmYes = null;
            confirmRoot.SetActive(false);
            if (lastSelected != null && lastSelected.activeInHierarchy && eventSystem != null)
                eventSystem.SetSelectedGameObject(lastSelected);
        }

        private class ConfirmCancel : MonoBehaviour, ICancelHandler
        {
            public MenuApp Owner;

            public void OnCancel(BaseEventData eventData)
            {
                Owner.CloseConfirm();
                eventData.Use();
            }
        }
    }
}
