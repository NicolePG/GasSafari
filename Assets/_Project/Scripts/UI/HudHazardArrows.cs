using GasSafari.Core;
using GasSafari.Hazards;
using UnityEngine;

namespace GasSafari.UI
{
    // Bordes de pantalla (docs/07 §3): flecha hacia un peligro fuera de cámara mientras dura su aviso.
    public class HudHazardArrows : MonoBehaviour
    {
        private const float EDGE_MARGIN = 60f;
        private const float EXTRA_SECONDS = 0.5f;

        [SerializeField] private RectTransform[] arrows;
        [SerializeField] private RectTransform area;

        private HazardBase[] targets;
        private float[] remaining;
        private Camera cam;

        private void Awake()
        {
            targets = new HazardBase[arrows.Length];
            remaining = new float[arrows.Length];
            for (int i = 0; i < arrows.Length; i++)
                arrows[i].gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnHazardWarning += OnWarning;
        }

        private void OnDisable()
        {
            GameEvents.OnHazardWarning -= OnWarning;
        }

        private void OnWarning(HazardBase hazard)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                if (remaining[i] > 0f)
                    continue;
                targets[i] = hazard;
                remaining[i] = hazard.Config.warningTime + EXTRA_SECONDS;
                return;
            }
        }

        private void LateUpdate()
        {
            if (cam == null)
                cam = Camera.main;
            for (int i = 0; i < arrows.Length; i++)
            {
                if (remaining[i] <= 0f)
                    continue;
                remaining[i] -= Time.unscaledDeltaTime;
                bool visible = remaining[i] > 0f && Place(arrows[i], targets[i].transform.position);
                arrows[i].gameObject.SetActive(visible);
            }
        }

        // false si el peligro está a la vista (no hace falta flecha).
        private bool Place(RectTransform arrow, Vector3 worldPoint)
        {
            if (cam == null)
                return false;
            var view = cam.WorldToViewportPoint(worldPoint);
            bool onScreen = view.z > 0f && view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f;
            if (onScreen)
                return false;

            // Detrás de la cámara se invierte para que la flecha apunte hacia donde hay que girar.
            var dir = new Vector2(view.x - 0.5f, view.y - 0.5f);
            if (view.z < 0f)
                dir = -dir;
            if (dir.sqrMagnitude < 1e-6f)
                dir = Vector2.up;
            dir.Normalize();

            var size = area.rect.size * 0.5f - Vector2.one * EDGE_MARGIN;
            float scale = Mathf.Min(size.x / Mathf.Max(Mathf.Abs(dir.x), 1e-4f), size.y / Mathf.Max(Mathf.Abs(dir.y), 1e-4f));
            arrow.anchoredPosition = dir * scale;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            return true;
        }
    }
}
