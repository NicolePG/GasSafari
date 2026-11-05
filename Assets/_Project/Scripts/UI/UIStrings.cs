using System;
using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.UI
{
    // Tabla de textos por clave (docs/07 §7). Nunca se escriben textos en el código.
    [CreateAssetMenu(menuName = "Gas Safari/UI Strings", fileName = "Strings_es")]
    public class UIStrings : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            [TextArea] public string text;
        }

        public Entry[] entries;

        [NonSerialized] private Dictionary<string, string> lookup;

        public string Get(string key)
        {
            if (lookup == null)
                Build();
            return lookup.TryGetValue(key, out var text) ? text : key;
        }

        // Para claves con números: "hud.mangos" = "Mangos {0}/{1}".
        public string Format(string key, params object[] args) => string.Format(Get(key), args);

        private void Build()
        {
            lookup = new Dictionary<string, string>(entries.Length);
            foreach (var entry in entries)
                lookup[entry.key] = entry.text;
        }
    }
}
