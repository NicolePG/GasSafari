using GasSafari.Characters;

namespace GasSafari.PowerUps
{
    // P01 Frijoles: recarga el gas del animal más cercano.
    public class BeansPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            if (context.Taker != null)
                context.Taker.GetComponent<GasTank>().SetFraction(Data.magnitude);
        }
    }
}
