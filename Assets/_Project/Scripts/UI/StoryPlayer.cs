using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace GasSafari.UI
{
    // Reproduce una viñeta de historia con Timeline (docs/01): una pista de activación por imagen, una tras otra. Corre en tiempo
    // sin escalar. Cualquier botón la salta (tras una pausa corta para no saltarse sola con el botón que la abrió).
    public class StoryPlayer : MonoBehaviour
    {
        public const float SKIP_GRACE = 0.4f;
        private const float FADE_IN = 0.5f;

        [SerializeField] private PlayableDirector director;
        [SerializeField] private CanvasGroup[] panels;
        [SerializeField] private TMP_Text[] captions;
        [SerializeField] private GameObject skipHint;

        private float startedAt;
        private bool finished;

        public bool IsPlaying { get; private set; }
        public bool Skipped { get; private set; }
        public PlayableDirector Director => director;
        // Imagen que se ve ahora (-1 si ninguna).
        public int PanelIndex { get; private set; } = -1;
        public int PanelCount => panels == null ? 0 : panels.Length;

        public event Action Finished;

        // Abre la viñeta: instancia su lienzo, pone los pies de texto y la reproduce. 'onFinished' se llama al terminar o saltar.
        public static StoryPlayer Play(StoryVignetteData data, Action onFinished = null)
        {
            if (data == null || data.prefab == null)
            {
                onFinished?.Invoke();
                return null;
            }
            var go = Instantiate(data.prefab);
            go.name = data.prefab.name;
            var player = go.GetComponent<StoryPlayer>();
            if (onFinished != null)
                player.Finished += onFinished;
            player.Begin(data);
            return player;
        }

        private void Begin(StoryVignetteData data)
        {
            for (int i = 0; captions != null && i < captions.Length; i++)
            {
                string text = data.CaptionFor(i);
                captions[i].text = text;
                // Sin texto, la franja del pie no se dibuja.
                captions[i].transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
            // La ayuda para saltar sale de la tabla de textos (clave story.skip); sin ella queda el texto del lienzo.
            if (skipHint != null && data.strings != null && skipHint.TryGetComponent<TMP_Text>(out var hintLabel))
            {
                string hint = data.strings.Get("story.skip");
                if (hint != "story.skip")
                    hintLabel.text = hint;
            }
            startedAt = Time.unscaledTime;
            IsPlaying = true;
            director.stopped += OnStopped;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.Play();
        }

        private void OnStopped(PlayableDirector _)
        {
            Finish();
        }

        public void Skip()
        {
            if (finished)
                return;
            Skipped = true;
            director.Stop();
            Finish();
        }

        private void Finish()
        {
            if (finished)
                return;
            finished = true;
            IsPlaying = false;
            Finished?.Invoke();
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            PanelIndex = -1;
            for (int i = 0; panels != null && i < panels.Length; i++)
            {
                if (!panels[i].gameObject.activeInHierarchy)
                    continue;
                PanelIndex = i;
                // Aparece con un fundido de entrada.
                panels[i].alpha = Mathf.MoveTowards(panels[i].alpha, 1f, Time.unscaledDeltaTime / FADE_IN);
            }

            if (Time.unscaledTime - startedAt >= SKIP_GRACE && AnyButtonPressed())
                Skip();
        }

        private static bool AnyButtonPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame))
                return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }
    }
}
