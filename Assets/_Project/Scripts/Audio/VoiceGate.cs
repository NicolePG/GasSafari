namespace GasSafari.Audio
{
    // Regla de voces de un personaje (docs/08 §2): 3 s de enfriamiento entre voces y nunca dos a la vez.
    // La caída ('force') es la excepción: suena siempre y corta la voz que estuviera sonando.
    public sealed class VoiceGate
    {
        public const float COOLDOWN = 3f;

        private float nextAllowed;

        public bool TryStart(float now, bool speaking, bool force)
        {
            if (!force && (speaking || now < nextAllowed))
                return false;
            nextAllowed = now + COOLDOWN;
            return true;
        }

        public void Reset() => nextAllowed = 0f;
    }
}
