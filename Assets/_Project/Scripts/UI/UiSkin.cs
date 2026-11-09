using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GasSafari.UI
{
    // Arte de los menús (M6): todos los sprites del kit, iconos, retratos, poses, logo, mapa y fondos de carga, buscables por
    // nombre de archivo ("Panel_Light", "Icon_Star_Full", "Portrait_Alex", "Icon_Ach_01_PrimerPedo", "Loading_W3"...).
    // Lo llena "Gas Safari/Setup/Importar arte de menús" y vive en Resources para que lo lea cualquier escena.
    [CreateAssetMenu(menuName = "Gas Safari/UI Skin", fileName = "UiSkin")]
    public class UiSkin : ScriptableObject
    {
        public const string RESOURCE_PATH = "UiSkin";

        [SerializeField] private Sprite[] sprites = new Sprite[0];
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TextAsset credits;

        private Dictionary<string, Sprite> lookup;
        private static UiSkin instance;

        public static UiSkin Instance
        {
            get => instance != null ? instance : instance = Resources.Load<UiSkin>(RESOURCE_PATH);
            set => instance = value;
        }

        public Sprite[] Sprites => sprites;
        public TMP_FontAsset Font => font;
        // CREDITS.md (fuente de la pantalla de créditos).
        public TextAsset Credits => credits;

        public void Set(Sprite[] all, TMP_FontAsset fontAsset, TextAsset creditsText = null)
        {
            sprites = all;
            font = fontAsset;
            credits = creditsText;
            lookup = null;
        }

        // null si el sprite no existe (la interfaz sigue funcionando sin arte).
        public Sprite Get(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(sprites.Length);
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                        lookup[sprites[i].name] = sprites[i];
                }
            }
            return lookup.TryGetValue(spriteName, out var sprite) ? sprite : null;
        }

        public bool Has(string spriteName) => Get(spriteName) != null;

        private void OnValidate()
        {
            lookup = null;
        }
    }
}
