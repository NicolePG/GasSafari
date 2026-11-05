namespace GasSafari.Level
{
    // Cuenta regresiva N..0 con un paso fijo entre números (0 = ¡YA!).
    public sealed class LevelCountdown
    {
        private float step;
        private float timer;

        public int Value { get; private set; } = -1;

        public void Start(int from, float stepSeconds)
        {
            Value = from;
            step = stepSeconds;
            timer = stepSeconds;
        }

        public void Stop() => Value = -1;

        // true cuando el número cambió en este avance.
        public bool Advance(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f)
                return false;

            Value--;
            timer = step;
            return true;
        }
    }
}
