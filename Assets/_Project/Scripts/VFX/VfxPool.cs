using System;
using UnityEngine;
using UnityEngine.Pool;

namespace GasSafari.VFX
{
    // Pool de partículas por ID (docs/10 §12). Se precalienta al arrancar: cero Instantiate en juego.
    public class VfxPool : MonoBehaviour
    {
        [SerializeField] private VfxLibrary library;

        private ObjectPool<PooledVfx>[] pools;
        private int[] prewarmTargets;

        public int CountInactive(VfxId id) => pools[(int)id].CountInactive;
        public int CountAll(VfxId id) => pools[(int)id].CountAll;

        private void Awake()
        {
            int count = Enum.GetValues(typeof(VfxId)).Length;
            pools = new ObjectPool<PooledVfx>[count];
            prewarmTargets = new int[count];
            foreach (var entry in library.entries)
            {
                var prefab = entry.prefab;
                var id = entry.id;
                var pool = new ObjectPool<PooledVfx>(
                    () => Create(prefab, id),
                    fx => fx.gameObject.SetActive(true),
                    fx => fx.gameObject.SetActive(false),
                    fx => Destroy(fx.gameObject),
                    false);
                pools[(int)id] = pool;
                prewarmTargets[(int)id] = entry.prewarm;
                Prewarm(pool, entry.prewarm);
            }
        }

        public PooledVfx Play(VfxId id, Vector3 position, Quaternion rotation, Color color, Transform follow = null)
        {
            var pool = pools[(int)id];
            // Unity vacía los ObjectPool al entrar a Play sin recargar el dominio (lo hace el runner de
            // pruebas por MCP) después de nuestro Awake: se vuelve a precalentar si hace falta.
            if (pool.CountAll == 0)
                Prewarm(pool, prewarmTargets[(int)id]);
            var fx = pool.Get();
            fx.transform.SetPositionAndRotation(position, rotation);
            fx.Play(color, follow);
            return fx;
        }

        internal void Release(PooledVfx fx)
        {
            pools[(int)fx.Id].Release(fx);
        }

        private PooledVfx Create(PooledVfx prefab, VfxId id)
        {
            var fx = Instantiate(prefab, transform);
            fx.Init(this, id);
            fx.gameObject.SetActive(false);
            return fx;
        }

        private static void Prewarm(ObjectPool<PooledVfx> pool, int count)
        {
            var temp = new PooledVfx[count];
            for (int i = 0; i < count; i++)
                temp[i] = pool.Get();
            for (int i = 0; i < count; i++)
                pool.Release(temp[i]);
        }
    }
}
