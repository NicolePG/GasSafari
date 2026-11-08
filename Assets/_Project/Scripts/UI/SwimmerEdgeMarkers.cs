using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Icono en el borde de la pantalla de cada animal caído que quedó fuera de cámara (docs/07 §3): el animal nunca
    // desaparece, siempre se sabe hacia dónde nada. Arma su propio Canvas, así funciona en todos los niveles.
    public class SwimmerEdgeMarkers : MonoBehaviour
    {
        private const float EDGE_MARGIN = 70f;
        private const float VIEWPORT_MARGIN = 0.04f;
        private const float MARKER_SIZE = 84f;
        private const float ARROW_SIZE = 34f;
        private const int SORTING_ORDER = 40;

        private sealed class Marker
        {
            public AvatarFall Fall;
            public RectTransform Root;
            public RectTransform Arrow;
        }

        private static Sprite circle;
        private static Sprite triangle;

        private readonly List<Marker> markers = new List<Marker>();
        private readonly List<Marker> spare = new List<Marker>();
        private RectTransform area;
        private Camera cam;

        // Pruebas: cuántos iconos están visibles ahora.
        public int VisibleCount { get; private set; }

        private void Awake()
        {
            var canvasObject = new GameObject("SwimmerMarkers", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            area = canvasObject.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerFell += Track;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerFell -= Track;
        }

        private void Track(PlayerAvatar avatar)
        {
            var fall = avatar.GetComponent<AvatarFall>();
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].Fall == fall)
                    return;
            }

            Marker marker;
            if (spare.Count > 0)
            {
                marker = spare[spare.Count - 1];
                spare.RemoveAt(spare.Count - 1);
            }
            else
            {
                marker = Create();
            }
            marker.Fall = fall;
            Style(marker, avatar);
            marker.Root.gameObject.SetActive(false);
            markers.Add(marker);
        }

        private void LateUpdate()
        {
            if (cam == null)
                cam = Camera.main;

            int visible = 0;
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var marker = markers[i];
                if (marker.Fall == null || !marker.Fall.IsOverboard)
                {
                    marker.Root.gameObject.SetActive(false);
                    markers.RemoveAt(i);
                    spare.Add(marker);
                    continue;
                }

                bool show = cam != null && Place(marker, marker.Fall.transform.position + Vector3.up * 0.5f);
                marker.Root.gameObject.SetActive(show);
                if (show)
                    visible++;
            }
            VisibleCount = visible;
        }

        // false si el animal está a la vista (no hace falta icono).
        private bool Place(Marker marker, Vector3 worldPoint)
        {
            var view = cam.WorldToViewportPoint(worldPoint);
            bool onScreen = view.z > 0f && view.x > VIEWPORT_MARGIN && view.x < 1f - VIEWPORT_MARGIN
                && view.y > VIEWPORT_MARGIN && view.y < 1f - VIEWPORT_MARGIN;
            if (onScreen)
                return false;

            // Detrás de la cámara se invierte la dirección para que el icono apunte hacia donde hay que mirar.
            var dir = new Vector2(view.x - 0.5f, view.y - 0.5f);
            if (view.z < 0f)
                dir = -dir;
            if (dir.sqrMagnitude < 1e-6f)
                dir = Vector2.down;
            dir.Normalize();

            var half = area.rect.size * 0.5f - Vector2.one * EDGE_MARGIN;
            float scale = Mathf.Min(half.x / Mathf.Max(Mathf.Abs(dir.x), 1e-4f), half.y / Mathf.Max(Mathf.Abs(dir.y), 1e-4f));
            marker.Root.anchoredPosition = dir * scale;
            marker.Arrow.anchoredPosition = dir * (MARKER_SIZE * 0.5f + ARROW_SIZE * 0.3f);
            marker.Arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            return true;
        }

        private Marker Create()
        {
            var root = NewImage("Marker", area, GetCircle(), Color.white, MARKER_SIZE);
            var arrow = NewImage("Arrow", root, GetTriangle(), Color.white, ARROW_SIZE);
            NewImage("Icon", root, null, Color.white, MARKER_SIZE * 0.72f);
            return new Marker { Root = root, Arrow = arrow };
        }

        private static void Style(Marker marker, PlayerAvatar avatar)
        {
            var color = avatar.PlayerColor;
            marker.Root.GetComponent<Image>().color = color;
            marker.Arrow.GetComponent<Image>().color = color;

            var icon = marker.Root.Find("Icon").GetComponent<Image>();
            var sprite = avatar.Character != null ? avatar.Character.icon : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.preserveAspect = true;
        }

        private static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Sprite GetCircle()
        {
            if (circle == null)
                circle = BuildSprite(64, (x, y) => (new Vector2(x, y) - new Vector2(31.5f, 31.5f)).magnitude <= 31f);
            return circle;
        }

        private static Sprite GetTriangle()
        {
            // Punta hacia arriba; ocupa la mitad inferior más ancha hacia la base.
            if (triangle == null)
                triangle = BuildSprite(64, (x, y) => Mathf.Abs(x - 31.5f) <= (63f - y) * 0.5f);
            return triangle;
        }

        private static Sprite BuildSprite(int size, System.Func<int, int, bool> inside)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = inside(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
