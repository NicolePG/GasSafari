using GasSafari.Characters;

namespace GasSafari.Input
{
    // Fuente de control que necesita conocer a su avatar (control doble: saber si el animal está nadando).
    public interface IAvatarBoundSource
    {
        void Bind(PlayerAvatar avatar);
    }
}
