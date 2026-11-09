using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GasSafari.UI
{
    // Revisa que ningún texto de una pantalla se salga de su caja ("textos al 150 % sin cortes", en español y en inglés).
    // Los textos por clave se encogen solos hasta un mínimo; si aun así no caben, aparecen aquí.
    public static class UiAudit
    {
        // El contorno del texto suma unos píxeles a sus límites: solo cuenta lo que de verdad se sale de la caja.
        public const float TOLERANCE = 8f;

        public static List<string> Overflows(Transform root)
        {
            var result = new List<string>();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text))
                    continue;
                var rect = text.rectTransform.rect;
                if (rect.width <= 1f || rect.height <= 1f)
                    continue;
                text.ForceMeshUpdate();
                var bounds = text.textBounds;
                bool tooTall = bounds.size.y > rect.height + TOLERANCE;
                bool tooWide = bounds.size.x > rect.width + TOLERANCE;
                if (tooTall || tooWide)
                    result.Add($"{Path(text.transform, root)} \"{Trim(text.text)}\" texto {bounds.size.x:0}x{bounds.size.y:0} en caja {rect.width:0}x{rect.height:0} (tamaño {text.fontSize:0.#})");
            }
            return result;
        }

        private static string Trim(string text) => text.Length > 40 ? text.Substring(0, 40) + "…" : text;

        private static string Path(Transform t, Transform root)
        {
            string path = t.name;
            for (var parent = t.parent; parent != null && parent != root; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
