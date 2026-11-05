using GasSafari.Core;
using GasSafari.Level;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // Al reaparecer en un checkpoint, devuelve los power-ups recogidos desde ese checkpoint (docs/05). P07 no vuelve.
    public class PowerUpRespawner : MonoBehaviour
    {
        [SerializeField] private CheckpointSystem checkpoints;

        private void OnEnable()
        {
            GameEvents.OnRespawn += OnRespawn;
        }

        private void OnDisable()
        {
            GameEvents.OnRespawn -= OnRespawn;
        }

        public void Configure(CheckpointSystem system)
        {
            checkpoints = system;
        }

        private void OnRespawn()
        {
            foreach (var effect in FindObjectsByType<TimedEffect>(FindObjectsSortMode.None))
                effect.End();

            var all = PowerUpBase.All;
            for (int i = 0; i < all.Count; i++)
            {
                var powerUp = all[i];
                if (powerUp.IsCollected && powerUp.CollectedAtCheckpoint >= checkpoints.CurrentIndex)
                    powerUp.Respawn();
            }
        }
    }
}
