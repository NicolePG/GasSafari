using GasSafari.Characters;

namespace GasSafari.Team
{
    // Quien disparó dentro de un grupo de sincronía que luego creció.
    public interface ISyncMember
    {
        // Sube el pedo de este miembro del multiplicador 'from' al 'to'.
        void ApplySyncTopUp(FartType type, float baseImpulse, float from, float to);
    }
}
