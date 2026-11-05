using UnityEngine;

namespace GasSafari.Hazards
{
    // H28 Barco que cruza (docs/05): bloqueador grande (Heavy) que cruza el cauce y deja una estela de oleaje.
    // Es un MovingBlocker con paradas largas en las orillas (la parada es el aviso) y, al pasar por el centro del
    // cauce, lanza una cresta de su WaveHazard 'wake' que baja por el cauce: pasar antes de que cruce o esperar la ola y surfearla.
    public class CrossingBoat : MovingBlocker
    {
        [SerializeField] private WaveHazard wake;

        public void SetWake(WaveHazard wave) => wake = wave;

        protected override void OnProgress(float previous, float current)
        {
            if (wake == null || Direction == 0)
                return;
            bool crossedCenter = (previous < 0.5f && current >= 0.5f) || (previous > 0.5f && current <= 0.5f);
            if (crossedCenter)
                wake.SpawnCrest();
        }
    }
}
