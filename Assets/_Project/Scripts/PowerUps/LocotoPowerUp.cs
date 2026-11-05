namespace GasSafari.PowerUps
{
    // P02 Locoto: pedos de fuego del animal más cercano (x1.5 de fuerza, queman Burnable).
    public class LocotoPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            if (context.Taker == null)
                return;

            var fire = TimedEffect.Attach<FireFartEffect>(context.Taker.gameObject, Data.duration);
            fire.Configure(Data.magnitude, Data.radius);
        }
    }
}
