using UnityEngine;
using UnityEngine.Pool;

namespace GasSafari.PowerUps
{
    // Pool de un tipo de power-up para los que se generan en juego (Infinito, minijuegos).
    public sealed class PowerUpPool
    {
        private readonly ObjectPool<PowerUpBase> pool;

        public PowerUpPool(PowerUpBase prefab, Transform parent)
        {
            pool = new ObjectPool<PowerUpBase>(
                () => Object.Instantiate(prefab, parent),
                null,
                powerUp => powerUp.gameObject.SetActive(false),
                powerUp => Object.Destroy(powerUp.gameObject),
                false);
        }

        public int CountInactive => pool.CountInactive;

        public PowerUpBase Get(Vector3 position)
        {
            var powerUp = pool.Get();
            powerUp.Recycle(position);
            return powerUp;
        }

        public void Release(PowerUpBase powerUp) => pool.Release(powerUp);
    }
}
