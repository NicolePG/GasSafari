namespace GasSafari.Hazards
{
    // Jefe con fases (W2-5): BossPhaseZone le cambia la fase al pasar la balsa; -1 lo detiene.
    public interface IPhasedBoss
    {
        void SetPhase(int phase);
    }
}
