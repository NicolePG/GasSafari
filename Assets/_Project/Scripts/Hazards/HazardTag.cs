using System;

namespace GasSafari.Hazards
{
    // Etiquetas de obstáculos (docs/10 §11). Un objeto puede tener varias.
    [Flags]
    public enum HazardTag
    {
        None = 0,
        Fragile = 1 << 0,
        Heavy = 1 << 1,
        Fauna = 1 << 2,
        Burnable = 1 << 3,
        Anchor = 1 << 4
    }
}
