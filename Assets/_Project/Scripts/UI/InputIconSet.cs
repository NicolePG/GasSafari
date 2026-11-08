using System;
using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.UI
{
    // Iconos de botón de Xbox, PlayStation y teclado (Kenney "Input Prompts", CC0, ver CREDITS.md).
    // Lo genera `Gas Safari/Setup/Importar iconos de botones (Kenney)` a partir de Art/UI/InputIcons/ y vive en Resources.
    public class InputIconSet : ScriptableObject
    {
        public const string RESOURCE_PATH = "InputIconSet";

        [Serializable]
        public struct Entry
        {
            public string id;
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Sprite> lookup;
        private static InputIconSet cached;

        public int Count => entries.Count;

        public static InputIconSet Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<InputIconSet>(RESOURCE_PATH);
                return cached;
            }
        }

        public void SetEntries(List<Entry> newEntries)
        {
            entries = newEntries;
            lookup = null;
        }

        // null si el icono no existe: quien lo pide usa el texto de InputGlyphs.
        public Sprite Get(string id)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>();
                foreach (var entry in entries)
                    lookup[entry.id] = entry.sprite;
            }
            return lookup.TryGetValue(id, out var sprite) ? sprite : null;
        }
    }
}
