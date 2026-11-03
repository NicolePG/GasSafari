namespace GasSafari.Abilities
{
    // Recarga de una habilidad, sin dependencias de Unity (T-ABIL-01).
    public class AbilityCooldown
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;
        // 0 = recién usada, 1 = lista (para el icono radial del HUD).
        public float Progress => Duration > 0f ? 1f - Remaining / Duration : 1f;

        public void Start(float duration)
        {
            Duration = duration;
            Remaining = duration;
        }

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f)
                Remaining -= deltaTime;
        }

        public void Reset()
        {
            Remaining = 0f;
        }
    }
}
