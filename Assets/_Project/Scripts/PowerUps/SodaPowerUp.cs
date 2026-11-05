namespace GasSafari.PowerUps
{
    // P05 Gaseosa: pedos de burbuja, la balsa flota sobre el agua mientras dura.
    public class SodaPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            context.Effects.Hover(Data.magnitude, Data.duration);
        }
    }
}
