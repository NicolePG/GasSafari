using UnityEngine;

namespace GasSafari.VFX
{
    // Efecto reutilizable. Vuelve al pool cuando el Particle System termina (Stop Action = Callback).
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledVfx : MonoBehaviour
    {
        private ParticleSystem system;
        private VfxPool owner;
        private Transform follow;
        private bool released;

        public VfxId Id { get; private set; }
        public bool IsPlaying => system.IsAlive(true);

        internal void Init(VfxPool pool, VfxId id)
        {
            owner = pool;
            Id = id;
            system = GetComponent<ParticleSystem>();
        }

        internal void Play(Color color, Transform target)
        {
            released = false;
            follow = target;
            var main = system.main;
            main.startColor = color;
            system.Play(true);
        }

        // Para efectos continuos: deja de emitir y vuelve al pool al apagarse.
        public void Stop()
        {
            follow = null;
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void LateUpdate()
        {
            if (follow != null)
                transform.SetPositionAndRotation(follow.position, follow.rotation);
        }

        private void OnParticleSystemStopped()
        {
            if (released)
                return;
            released = true;
            follow = null;
            owner.Release(this);
        }
    }
}
