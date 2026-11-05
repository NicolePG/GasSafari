namespace GasSafari.PowerUps
{
    // P04 Coco escudo: la balsa absorbe el siguiente daño a una cámara.
    public class CoconutPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            context.Chambers.GiveShield();
        }
    }
}
