using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    public enum ButtonStyle
    {
        Green,
        Blue,
        Red
    }

    // Crea elementos de interfaz de los menús con el kit de arte (docs/07, M6). Todo se construye por código sobre un Canvas
    // 1920x1080: los textos son LocalizedText (clave + idioma + tamaño) y todo se navega con mando o teclado.
    public static class UiFactory
    {
        public static readonly Color Ink = new Color32(0x1C, 0x1A, 0x26, 255);
        public static readonly Color Cream = new Color32(0xFF, 0xF3, 0xD6, 255);
        public static readonly Color Gold = new Color32(0xFF, 0xC8, 0x2E, 255);
        public static readonly Color Muted = new Color32(0x8A, 0x86, 0x9E, 255);
        public static readonly Color Dim = new Color(0.05f, 0.04f, 0.12f, 0.72f);

        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        public static Sprite Sprite(string name) => UiSkin.Instance != null ? UiSkin.Instance.Get(name) : null;

        // ---------------------------------------------------------------- canvas y rectángulos
        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
                root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return rect;
        }

        // Ancla en un punto (0..1) con tamaño fijo y desplazamiento en píxeles de referencia.
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            return rect;
        }

        // Ocupa una franja: anchorMin/Max normalizados con márgenes en píxeles.
        public static RectTransform Region(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector4 margin = default)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(margin.x, margin.y);
            rect.offsetMax = new Vector2(-margin.z, -margin.w);
            return rect;
        }

        // ---------------------------------------------------------------- imágenes
        public static Image Picture(Transform parent, string name, Sprite sprite, Color? color = null, bool preserveAspect = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color ?? Color.white;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            image.enabled = sprite != null || color.HasValue;
            return image;
        }

        public static Image Picture(Transform parent, string name, string spriteName, Color? color = null, bool preserveAspect = true) =>
            Picture(parent, name, Sprite(spriteName), color, preserveAspect);

        public static Image Panel(Transform parent, string name, string spriteName = "Panel_Light", Color? tint = null)
        {
            var image = Picture(parent, name, Sprite(spriteName), tint ?? Color.white, false);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.raycastTarget = true;
            return image;
        }

        // Fondo oscuro translúcido a pantalla completa.
        public static Image Backdrop(Transform parent, string name = "Dim")
        {
            var image = Picture(parent, name, (Sprite)null, Dim, false);
            Stretch(image.rectTransform);
            image.raycastTarget = true;
            return image;
        }

        // ---------------------------------------------------------------- textos
        public static LocalizedText Label(Transform parent, string name, string key, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null, FontStyles style = FontStyles.Normal,
            params object[] args)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LocalizedText));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (UiSkin.Instance != null && UiSkin.Instance.Font != null)
                text.font = UiSkin.Instance.Font;
            text.alignment = align;
            text.color = color ?? Cream;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.richText = true;
            var localized = go.GetComponent<LocalizedText>();
            if (key != null)
                localized.Setup(key, size, args);
            else
                localized.SetSize(size);
            return localized;
        }

        // Texto que no sale de la tabla (un número, un nombre propio).
        public static LocalizedText Literal(Transform parent, string name, string text, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null, FontStyles style = FontStyles.Normal)
        {
            var label = Label(parent, name, null, size, align, color, style);
            label.SetLiteral(text);
            return label;
        }

        // Texto con contorno oscuro (títulos y botones sobre fondos de colores).
        public static LocalizedText Outlined(this LocalizedText label, float width = 0.22f)
        {
            label.Label.outlineWidth = width;
            label.Label.outlineColor = Ink;
            return label;
        }

        // ---------------------------------------------------------------- botones
        public static Button MakeButton(Transform parent, string name, string labelKey, ButtonStyle style, Vector2 size,
            UnityAction onClick = null, float fontSize = 44f)
        {
            string prefix = "Button_" + style + "_";
            var image = Picture(parent, name, prefix + "Normal", Color.white, false);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            var swap = new SpriteState
            {
                highlightedSprite = Sprite(prefix + "Highlighted"),
                selectedSprite = Sprite(prefix + "Highlighted"),
                pressedSprite = Sprite(prefix + "Pressed"),
                disabledSprite = Sprite(prefix + "Disabled")
            };
            button.spriteState = swap;
            if (swap.highlightedSprite == null)
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.selectedColor = Gold;
                colors.highlightedColor = new Color(1f, 0.9f, 0.6f);
                button.colors = colors;
            }
            if (onClick != null)
                button.onClick.AddListener(onClick);
            image.gameObject.AddComponent<FocusScale>();

            if (labelKey != null)
            {
                // La base del botón ocupa los 13 px de abajo: el texto va un poco arriba.
                var label = Label(image.transform, "Label", labelKey, fontSize, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).Outlined();
                Region(label.Label.rectTransform, Vector2.zero, Vector2.one, new Vector4(22f, 22f, 22f, 12f));
            }
            return button;
        }

        // Tarjeta seleccionable (modos, personajes, artículos...): panel crema con realce al enfocar.
        public static Button MakeCard(Transform parent, string name, Vector2 size, string spriteName = "Panel_Light")
        {
            var image = Panel(parent, name, spriteName);
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.62f);
            colors.selectedColor = new Color(1f, 0.86f, 0.42f);
            colors.pressedColor = new Color(0.9f, 0.75f, 0.35f);
            colors.disabledColor = new Color(0.62f, 0.62f, 0.66f, 1f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            image.gameObject.AddComponent<FocusScale>();
            return button;
        }

        // ---------------------------------------------------------------- listas
        public static VerticalLayoutGroup VerticalList(Transform parent, string name, float spacing, TextAnchor anchor = TextAnchor.UpperCenter)
        {
            var rect = Rect(name, parent);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = anchor;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalList(Transform parent, string name, float spacing, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rect = Rect(name, parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = anchor;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static GridLayoutGroup Grid(Transform parent, string name, Vector2 cell, Vector2 spacing, int columns)
        {
            var rect = Rect(name, parent);
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            return grid;
        }

        // Vista con desplazamiento vertical. 'content' crece con sus hijos; la rueda y el mando (por selección) la mueven.
        public static ScrollRect Scroll(Transform parent, string name, out RectTransform content)
        {
            var root = Rect(name, parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return scroll;
        }

        // Navegación explícita en una columna (arriba/abajo), sin saltos raros entre pestañas.
        public static void LinkVertical(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = FindNeighbour(items, i, -1);
                nav.selectOnDown = FindNeighbour(items, i, 1);
                items[i].navigation = nav;
            }
        }

        // Navegación explícita en una fila (izquierda/derecha).
        public static void LinkHorizontal(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = FindNeighbour(items, i, -1);
                nav.selectOnRight = FindNeighbour(items, i, 1);
                items[i].navigation = nav;
            }
        }

        private static Selectable FindNeighbour(Selectable[] items, int from, int step)
        {
            for (int i = from + step; i >= 0 && i < items.Length; i += step)
            {
                if (items[i] != null && items[i].gameObject.activeInHierarchy)
                    return items[i];
            }
            return null;
        }
    }

    // El elemento enfocado (mando o teclado) crece un poco para verse sin ratón.
    public class FocusScale : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float SCALE = 1.06f;

        private bool selected;
        private bool hovered;

        public void OnSelect(BaseEventData eventData) { selected = true; Apply(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; Apply(); }
        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Apply(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Apply(); }

        private void OnEnable()
        {
            selected = false;
            hovered = false;
            Apply();
        }

        private void Apply()
        {
            transform.localScale = Vector3.one * (selected || hovered ? SCALE : 1f);
        }
    }
}
