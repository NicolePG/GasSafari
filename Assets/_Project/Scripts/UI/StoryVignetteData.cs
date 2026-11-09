using System;
using UnityEngine;

namespace GasSafari.UI
{
    // Una viñeta de historia (docs/01 "Historia"): de 3 a 5 imágenes entre mundos, saltables. Los pies de texto salen de las claves
    // story.<momento>.<n> (Tools/Export/Localization/strings.csv); si una clave no tiene texto, la imagen se muestra sin pie.
    [CreateAssetMenu(menuName = "Gas Safari/Story Vignette", fileName = "Story_Moment")]
    public class StoryVignetteData : ScriptableObject
    {
        [Serializable]
        public struct Panel
        {
            public Sprite image;
            public string captionKey;
        }

        [Tooltip("Intro, W1_End ... W5_End, W6_Secret")] public string id;
        public Panel[] panels;
        [Tooltip("Segundos que dura cada imagen en la línea de tiempo")] public float secondsPerPanel = 5f;
        public UIStrings strings;
        [Tooltip("Lienzo con la línea de tiempo (PlayableDirector) y un panel por imagen")] public GameObject prefab;

        public float TotalSeconds => panels == null ? 0f : panels.Length * secondsPerPanel;

        // Texto del pie de la imagen i, o "" si no hay (clave sin traducir o sin texto).
        public string CaptionFor(int index)
        {
            if (strings == null || panels == null || index < 0 || index >= panels.Length)
                return string.Empty;
            string key = panels[index].captionKey;
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            string text = strings.Get(key);
            return text == key ? string.Empty : text;
        }
    }
}
