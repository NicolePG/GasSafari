using GasSafari.Core;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // P07 Mango dorado: coleccionable del nivel y puntos de medidor de equipo. Queda recogido al reaparecer.
    // El conteo por nivel lo lleva quien escuche GameEvents.OnPowerUp (resultados y progresión).
    public class GoldenMangoPowerUp : PowerUpBase
    {
        [Tooltip("Ranura 0-2 del mango dentro del nivel (ruta, habilidad o riesgo, escondido)")]
        [SerializeField] private int slot;

        public int Slot => slot;

        protected override void Apply(PowerUpContext context)
        {
            context.Meter.Add(Data.magnitude);
            GameEvents.RaiseGoldenMango(slot);
        }
    }
}
