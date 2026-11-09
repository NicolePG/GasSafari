using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Una fila de Opciones (docs/07 §5) que se maneja igual con mando, teclado o ratón:
    // izquierda/derecha cambian el valor, Aceptar lo alterna o lo activa, arriba/abajo cambian de fila.
    public class OptionRow : Selectable, IMoveHandler, ISubmitHandler, IPointerClickHandler, IDragHandler
    {
        public enum Kind
        {
            Slider,
            Toggle,
            Choice,
            Action
        }

        public const int SLIDER_STEPS = 10;
        public static readonly Vector2 DefaultSize = new Vector2(1180f, 84f);

        private Kind kind;
        private LocalizedText label;
        private LocalizedText valueText;
        private Image barFill;
        private RectTransform barRect;
        private string[] choiceKeys = new string[0];
        private string[] choiceLiterals;
        private float sliderValue;
        private bool toggleValue;
        private int choiceIndex;

        public Kind RowKind => kind;
        public LocalizedText Label => label;
        public float Slider01 => sliderValue;
        public bool Toggle => toggleValue;
        public int ChoiceIndex => choiceIndex;
        public int ChoiceCount => choiceLiterals != null ? choiceLiterals.Length : choiceKeys.Length;

        // Cambió el valor por una acción del jugador (no al fijarlo por código).
        public event Action<OptionRow> Changed;
        // Solo filas de acción (por ejemplo remapear un botón).
        public event Action<OptionRow> Activated;

        // ---------------------------------------------------------------- creación
        public static OptionRow Create(Transform parent, string name, string labelKey, Kind kind, Vector2? size = null)
        {
            var background = UiFactory.Panel(parent, name, "Panel_Inset");
            background.rectTransform.sizeDelta = size ?? DefaultSize;
            // Panel_Inset ya es azul claro: se usa de fondo de la fila tal cual.
            var row = background.gameObject.AddComponent<OptionRow>();
            row.targetGraphic = background;
            row.transition = Transition.ColorTint;
            var colors = row.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.92f);
            colors.highlightedColor = new Color(1f, 0.95f, 0.7f, 1f);
            colors.selectedColor = new Color(1f, 0.88f, 0.45f, 1f);
            colors.pressedColor = new Color(0.9f, 0.8f, 0.4f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.65f, 0.7f);
            colors.fadeDuration = 0.05f;
            row.colors = colors;
            row.kind = kind;

            row.label = UiFactory.Label(row.transform, "Label", labelKey, 36f, TMPro.TextAlignmentOptions.MidlineLeft, UiFactory.Ink, TMPro.FontStyles.Bold);
            UiFactory.Region(row.label.Label.rectTransform, Vector2.zero, new Vector2(0.52f, 1f), new Vector4(34f, 6f, 8f, 6f));

            if (kind == Kind.Slider)
            {
                var back = UiFactory.Picture(row.transform, "BarBack", "Meter_Back", Color.white, false);
                back.type = Image.Type.Sliced;
                row.barRect = back.rectTransform;
                UiFactory.Region(back.rectTransform, new Vector2(0.54f, 0.5f), new Vector2(0.86f, 0.5f), new Vector4(0f, 0f, 0f, 0f));
                back.rectTransform.sizeDelta = new Vector2(back.rectTransform.sizeDelta.x, 36f);
                var fill = UiFactory.Picture(back.transform, "BarFill", "Meter_Fill", Color.white, false);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                UiFactory.Stretch(fill.rectTransform);
                row.barFill = fill;
            }

            row.valueText = UiFactory.Literal(row.transform, "Value", string.Empty, 36f, TMPro.TextAlignmentOptions.MidlineRight, UiFactory.Ink, TMPro.FontStyles.Bold);
            var valueAnchor = kind == Kind.Slider ? new Vector2(0.87f, 0f) : new Vector2(0.52f, 0f);
            UiFactory.Region(row.valueText.Label.rectTransform, valueAnchor, Vector2.one, new Vector4(8f, 6f, 30f, 6f));
            row.Refresh();
            return row;
        }

        // ---------------------------------------------------------------- valores
        public void SetSlider(float value01)
        {
            sliderValue = Mathf.Round(Mathf.Clamp01(value01) * SLIDER_STEPS) / SLIDER_STEPS;
            Refresh();
        }

        public void SetToggle(bool value)
        {
            toggleValue = value;
            Refresh();
        }

        // Opciones por clave de la tabla de textos (se traducen solas).
        public void SetChoices(string[] keys, int selected)
        {
            choiceKeys = keys;
            choiceLiterals = null;
            choiceIndex = keys.Length == 0 ? 0 : Mathf.Clamp(selected, 0, keys.Length - 1);
            Refresh();
        }

        // Opciones que no se traducen (resoluciones, "60").
        public void SetLiteralChoices(string[] values, int selected)
        {
            choiceLiterals = values;
            choiceKeys = new string[0];
            choiceIndex = values.Length == 0 ? 0 : Mathf.Clamp(selected, 0, values.Length - 1);
            Refresh();
        }

        public void SetActionText(string literal)
        {
            valueText.SetLiteral(literal);
        }

        public void SetActionKey(string key)
        {
            valueText.SetKey(key);
        }

        private void Refresh()
        {
            if (valueText == null)
                return;
            switch (kind)
            {
                case Kind.Slider:
                    barFill.fillAmount = sliderValue;
                    valueText.SetLiteral(Mathf.RoundToInt(sliderValue * 100f) + "%");
                    break;
                case Kind.Toggle:
                    valueText.SetKey(toggleValue ? "ui.on" : "ui.off");
                    break;
                case Kind.Choice:
                    if (choiceLiterals != null && choiceLiterals.Length > 0)
                        valueText.SetLiteral("< " + choiceLiterals[choiceIndex] + " >");
                    else if (choiceKeys.Length > 0)
                        valueText.SetKey(choiceKeys[choiceIndex]);
                    break;
            }
        }

        // Para las opciones por clave se dibujan los símbolos < > alrededor del texto traducido.
        private void LateUpdate()
        {
            if (kind == Kind.Choice && choiceKeys.Length > 0 && valueText != null)
            {
                var text = valueText.Label;
                string plain = LocalizedText.Table != null ? LocalizedText.Table.Get(choiceKeys[choiceIndex]) : choiceKeys[choiceIndex];
                string wanted = "< " + plain + " >";
                if (text.text != wanted)
                    text.text = wanted;
            }
        }

        // ---------------------------------------------------------------- entrada
        public override void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (Step(-1))
                    {
                        eventData.Use();
                        return;
                    }
                    break;
                case MoveDirection.Right:
                    if (Step(1))
                    {
                        eventData.Use();
                        return;
                    }
                    break;
            }
            base.OnMove(eventData);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            switch (kind)
            {
                case Kind.Toggle:
                    Step(1);
                    break;
                case Kind.Choice:
                    Step(1, wrap: true);
                    break;
                case Kind.Action:
                    Activated?.Invoke(this);
                    break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable())
                return;
            if (kind == Kind.Slider)
            {
                SetFromPointer(eventData);
                return;
            }
            if (kind == Kind.Action)
            {
                Activated?.Invoke(this);
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local);
            var rect = ((RectTransform)transform).rect;
            float normalized = (local.x - rect.xMin) / Mathf.Max(1f, rect.width);
            // En las opciones, la mitad izquierda del valor retrocede y el resto avanza.
            bool backwards = kind == Kind.Choice && normalized > 0.52f && normalized < 0.76f;
            Step(backwards ? -1 : 1, wrap: true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (kind == Kind.Slider)
                SetFromPointer(eventData);
        }

        private void SetFromPointer(PointerEventData eventData)
        {
            if (barRect == null)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(barRect, eventData.position, eventData.pressEventCamera, out var local);
            float t = Mathf.InverseLerp(barRect.rect.xMin, barRect.rect.xMax, local.x);
            float before = sliderValue;
            SetSlider(t);
            if (!Mathf.Approximately(before, sliderValue))
                Changed?.Invoke(this);
        }

        // true si el valor cambió (o se consumió el movimiento).
        private bool Step(int direction, bool wrap = false)
        {
            switch (kind)
            {
                case Kind.Slider:
                {
                    float before = sliderValue;
                    SetSlider(sliderValue + direction / (float)SLIDER_STEPS);
                    if (!Mathf.Approximately(before, sliderValue))
                        Changed?.Invoke(this);
                    return true;
                }
                case Kind.Toggle:
                    toggleValue = !toggleValue;
                    Refresh();
                    Changed?.Invoke(this);
                    return true;
                case Kind.Choice:
                {
                    int count = ChoiceCount;
                    if (count <= 1)
                        return true;
                    int next = choiceIndex + direction;
                    if (wrap)
                        next = (next % count + count) % count;
                    next = Mathf.Clamp(next, 0, count - 1);
                    if (next != choiceIndex)
                    {
                        choiceIndex = next;
                        Refresh();
                        Changed?.Invoke(this);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
