using UnityEngine;

namespace GasSafari.Hazards
{
    // Obstáculo Fragile o Heavy que se puede destruir (Rugido, Terremoto, explosiones).
    public interface IBreakable
    {
        void Break(Vector3 source);
    }
}
