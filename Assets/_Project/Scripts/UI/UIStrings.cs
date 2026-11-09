using System;
using System.Collections.Generic;
using GasSafari.Core;
using UnityEngine;

namespace GasSafari.UI
{
    // Tabla de textos por clave y por idioma (docs/07 §7, M10-01). Se llena con "Gas Safari/Setup/Importar textos (CSV)"
    // desde Tools/Export/Localization/strings.csv. Nunca se escriben textos en el código.
    [CreateAssetMenu(menuName = "Gas Safari/UI Strings", fileName = "Strings")]
    public class UIStrings : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            // Columna "es" del CSV (el campo se llama text por compatibilidad con los datos ya guardados).
            [TextArea] public string text;
            // Columna "en" del CSV.
            [TextArea] public string en;
        }

        public Entry[] entries;

        [NonSerialized] private Dictionary<string, Entry> lookup;

        // El texto en el idioma activo. Si falta en inglés cae en español; si falta la clave, devuelve la clave.
        public string Get(string key) => Get(key, Localization.Language);

        public string Get(string key, string language)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (lookup == null)
                Build();
            if (!lookup.TryGetValue(key, out var entry))
                return key;
            if (language == Localization.ENGLISH && !string.IsNullOrEmpty(entry.en))
                return entry.en;
            return entry.text;
        }

        public bool Has(string key)
        {
            if (lookup == null)
                Build();
            return lookup.ContainsKey(key);
        }

        // Para claves con números: "hud.mangos" = "Mangos {0}/{1}".
        public string Format(string key, params object[] args) => string.Format(Get(key), args);

        private void Build()
        {
            int count = entries != null ? entries.Length : 0;
            lookup = new Dictionary<string, Entry>(count);
            for (int i = 0; i < count; i++)
                lookup[entries[i].key] = entries[i];
        }

        private void OnValidate()
        {
            lookup = null;
        }
    }
}
