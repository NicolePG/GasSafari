namespace GasSafari.PowerUps
{
    // P06 Cebolla: nube apestosa alrededor de la balsa; la fauna huye.
    public class OnionPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            var cloud = TimedEffect.Attach<StinkCloud>(context.Raft.gameObject, Data.duration);
            cloud.Radius = Data.radius;
        }
    }
}
