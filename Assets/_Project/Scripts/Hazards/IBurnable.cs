using UnityEngine;

namespace GasSafari.Hazards
{
    // Obstáculo Burnable que el locoto quema (lianas, telarañas).
    public interface IBurnable
    {
        void Burn(Vector3 source);
    }
}
