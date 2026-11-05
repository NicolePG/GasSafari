using UnityEngine;

namespace GasSafari.PowerUps
{
    // Efecto temporal que un power-up deja en la balsa o el animal: se quita solo al terminar.
    // Volver a aplicarlo renueva la duración.
    public abstract class TimedEffect : MonoBehaviour
    {
        public float Remaining { get; private set; }

        public static T Attach<T>(GameObject target, float duration) where T : TimedEffect
        {
            if (!target.TryGetComponent<T>(out var effect))
                effect = target.AddComponent<T>();
            effect.Remaining = duration;
            effect.OnStarted();
            return effect;
        }

        protected virtual void OnStarted() { }
        protected virtual void OnEnded() { }
        protected virtual void OnTick(float deltaTime) { }

        private void Update()
        {
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                End();
                return;
            }
            OnTick(Time.deltaTime);
        }

        public void End()
        {
            OnEnded();
            Destroy(this);
        }
    }
}
