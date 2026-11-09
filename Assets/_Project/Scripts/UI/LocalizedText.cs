using GasSafari.Core;
using TMPro;
using UnityEngine;

namespace GasSafari.UI
{
    // Texto de interfaz que se lee por clave y se vuelve a pedir cuando cambia el idioma o el tamaño de texto.
    // Con autoajuste, un texto largo (inglés o 150 %) se encoge hasta caber antes que cortarse.
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        // Mínimo legible: nunca se encoge por debajo de esta fracción del tamaño base.
        public const float MIN_SHRINK = 0.45f;

        [SerializeField] private string key;
        [SerializeField] private float baseSize = 36f;
        [Tooltip("Si es falso, el tamaño no cambia con la opción de tamaño de textos")]
        [SerializeField] private bool followTextScale = true;

        private TMP_Text label;
        private object[] args;
        private string literal;

        // Tamaño de textos de Accesibilidad (1, 1.25 o 1.5). Lo fija el servicio de opciones.
        public static float TextScale { get; private set; } = 1f;

        public string Key => key;
        public TMP_Text Label => label != null ? label : label = GetComponent<TMP_Text>();

        public static event System.Action TextScaleChanged;

        public static void SetTextScale(float scale)
        {
            scale = Mathf.Clamp(scale, 1f, 1.5f);
            if (Mathf.Approximately(scale, TextScale))
                return;
            TextScale = scale;
            TextScaleChanged?.Invoke();
        }

        private static UIStrings table;

        // La tabla única de textos (Resources/Strings.asset). Las pruebas pueden reemplazarla.
        public static UIStrings Table
        {
            get => table != null ? table : table = Resources.Load<UIStrings>("Strings");
            set => table = value;
        }

        // Convierte un texto ya existente (de una escena construida) en un texto por clave. Conserva su tamaño como base.
        public static LocalizedText Bind(TMP_Text label, string textKey, params object[] formatArgs)
        {
            if (label == null)
                return null;
            var localized = label.GetComponent<LocalizedText>();
            if (localized == null)
            {
                localized = label.gameObject.AddComponent<LocalizedText>();
                localized.baseSize = label.fontSize;
            }
            localized.key = textKey;
            localized.args = formatArgs;
            localized.literal = null;
            localized.Refresh();
            return localized;
        }

        public void Setup(string textKey, float size, params object[] formatArgs)
        {
            key = textKey;
            baseSize = size;
            args = formatArgs;
            literal = null;
            Refresh();
        }

        public void SetKey(string textKey, params object[] formatArgs)
        {
            key = textKey;
            args = formatArgs;
            literal = null;
            Refresh();
        }

        // Texto que no sale de la tabla (un número, un nombre propio): se muestra tal cual, con el mismo autoajuste.
        public void SetLiteral(string text)
        {
            key = null;
            args = null;
            literal = text;
            Refresh();
        }

        public void SetSize(float size)
        {
            baseSize = size;
            Refresh();
        }

        private void OnEnable()
        {
            Localization.Changed += Refresh;
            TextScaleChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Localization.Changed -= Refresh;
            TextScaleChanged -= Refresh;
        }

        public void Refresh()
        {
            var text = Label;
            if (text == null)
                return;

            float scale = followTextScale ? TextScale : 1f;
            text.enableAutoSizing = true;
            text.fontSizeMax = baseSize * scale;
            text.fontSizeMin = Mathf.Max(10f, baseSize * MIN_SHRINK);
            text.fontSize = baseSize * scale;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;

            string value;
            if (literal != null)
                value = literal;
            else if (string.IsNullOrEmpty(key))
                value = string.Empty;
            else
            {
                value = Table != null ? Table.Get(key) : key;
                if (args != null && args.Length > 0)
                {
                    try { value = string.Format(value, args); }
                    catch (System.FormatException) { }
                }
            }
            text.text = value;
        }
    }
}
