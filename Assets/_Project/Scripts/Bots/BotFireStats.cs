namespace GasSafari.Bots
{
    // Contadores de las decisiones de pedo de un bot (diagnóstico de velocidad en pruebas de nivel).
    public struct BotFireStats
    {
        public int Decisions;
        public int Aligned;
        public int CanFire;
        public int Straight;
        public int WantsSpeed;
        public int Blasts;
        public int Puffs;
        public float GasSum;

        public void Count(bool aligned, bool canFire, bool straight, float deficit, float gasFraction)
        {
            Decisions++;
            if (aligned) Aligned++;
            if (canFire) CanFire++;
            if (straight) Straight++;
            if (deficit > 0f) WantsSpeed++;
            GasSum += gasFraction;
        }

        public override string ToString()
        {
            float n = Decisions > 0 ? Decisions : 1;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "decisiones={0} alineado={1:P0} puedeDisparar={2:P0} recta={3:P0} quiereVel={4:P0} ráfaga={5:P0} toques={6} gasMedio={7:P0}",
                Decisions, Aligned / n, CanFire / n, Straight / n, WantsSpeed / n, Blasts / n, Puffs, GasSum / n);
        }
    }
}
