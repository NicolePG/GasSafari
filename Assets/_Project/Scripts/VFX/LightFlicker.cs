using UnityEngine;

namespace GasSafari.VFX
{
    // Parpadeo de una luz de fuego (túnel en llamas de W3-2): la intensidad oscila con ruido suave, fase propia por luz.
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] private float amount = 0.35f;
        [SerializeField] private float speed = 7f;

        private Light target;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            target = GetComponent<Light>();
            baseIntensity = target.intensity;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            target.intensity = baseIntensity * (1f + noise * 2f * amount);
        }
    }
}
