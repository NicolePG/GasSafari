using UnityEngine;

namespace GasSafari.Hazards
{
    // Fauna que retrocede y no ataca durante 'duration' segundos.
    public interface IScareable
    {
        void Scare(float duration, Vector3 source);
    }
}
