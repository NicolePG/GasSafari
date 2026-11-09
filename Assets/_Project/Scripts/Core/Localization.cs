using System;
using UnityEngine;

namespace GasSafari.Core
{
    // Servicio de idioma (M10-01). Guarda el código activo ("es" o "en") y avisa a las pantallas abiertas para que
    // vuelvan a pedir sus textos. El idioma elegido vive en el guardado (settings.language).
    public static class Localization
    {
        public const string SPANISH = "es";
        public const string ENGLISH = "en";

        public static readonly string[] Languages = { SPANISH, ENGLISH };

        public static string Language { get; private set; } = SPANISH;

        // Se dispara solo cuando el idioma cambia de verdad.
        public static event Action Changed;

        public static bool IsEnglish => Language == ENGLISH;

        // Códigos desconocidos caen en español.
        public static string Normalize(string code) => code == ENGLISH ? ENGLISH : SPANISH;

        public static void Set(string code)
        {
            code = Normalize(code);
            if (code == Language)
                return;
            Language = code;
            Changed?.Invoke();
        }

        // Clave del nombre del idioma en la tabla ("lang.es" / "lang.en"), siempre en su propio idioma.
        public static string NameKey(string code) => "lang." + Normalize(code);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Language = SPANISH;
            Changed = null;
        }
    }
}
