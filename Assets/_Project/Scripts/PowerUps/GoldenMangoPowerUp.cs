namespace GasSafari.PowerUps
{
    // P07 Mango dorado: coleccionable del nivel y puntos de medidor de equipo. Queda recogido al reaparecer.
    // El conteo por nivel lo lleva quien escuche GameEvents.OnPowerUp (resultados y progresión).
    public class GoldenMangoPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            context.Meter.Add(Data.magnitude);
        }
    }
}
