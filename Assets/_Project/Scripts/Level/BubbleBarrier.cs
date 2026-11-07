using System;
using UnityEngine;

namespace GasSafari.Level
{
    // Barrera de burbujas del T-0 (docs/07 §8): cierra el paso hasta que se cumple la lección; al abrirse revienta.
    // Reiniciar el checkpoint la deja como estaba (abierta si la lección ya se cumplió).
    public class BubbleBarrier : MonoBehaviour
    {
        [SerializeField] private Collider blocker;
        [SerializeField] private Renderer[] visuals = Array.Empty<Renderer>();
        [Tooltip("Partículas opcionales al reventar")] [SerializeField] private ParticleSystem popEffect;

        public bool IsOpen { get; private set; }

        public event Action Opened;

        public void Configure(Collider blockerCollider, Renderer[] renderers)
        {
            blocker = blockerCollider;
            visuals = renderers;
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            SetClosed(false);
            if (popEffect != null)
                popEffect.Play();
            Opened?.Invoke();
        }

        public void Close()
        {
            IsOpen = false;
            SetClosed(true);
        }

        private void SetClosed(bool closed)
        {
            if (blocker != null)
                blocker.enabled = closed;
            for (int i = 0; i < visuals.Length; i++)
                visuals[i].enabled = closed;
        }
    }
}
