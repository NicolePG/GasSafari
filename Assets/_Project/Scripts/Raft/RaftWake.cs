using GasSafari.Level;
using UnityEngine;

namespace GasSafari.Raft
{
    // Estela de la balsa (docs/09 §2): espuma plana que queda sobre el agua detrás de la popa y se ensancha al desvanecerse.
    // La cantidad sigue la velocidad: con la balsa quieta no hay estela. Se agrega sola en RaftSpawner.
    [RequireComponent(typeof(RaftController))]
    public class RaftWake : MonoBehaviour
    {
        private const string MATERIAL_PATH = "Wake/M_Wake";
        private const float MIN_SPEED = 0.8f;
        private const float RATE_PER_SPEED = 2.4f;
        private const float MAX_RATE = 24f;

        private static Texture2D softTexture;

        private RaftController raft;
        private ParticleSystem foam;
        private Transform emitter;
        private float radius = 1.5f;

        private void Awake()
        {
            raft = GetComponent<RaftController>();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);
                radius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.5f, 1f, 4f);
            }
            Build();
        }

        private void Build()
        {
            var material = Resources.Load<Material>(MATERIAL_PATH);
            if (material == null)
                return;
            material = new Material(material) { mainTexture = SoftTexture() };

            var go = new GameObject("Wake");
            emitter = go.transform;
            emitter.SetParent(transform, false);
            foam = go.AddComponent<ParticleSystem>();
            foam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = foam.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 1.1f, radius * 1.7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.maxParticles = 200;

            var emission = foam.emission;
            emission.rateOverTime = 0f;

            var shape = foam.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.55f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var color = foam.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            var size = foam.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 1.7f)));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            foam.Play();
        }

        private void LateUpdate()
        {
            if (foam == null)
                return;
            var velocity = raft.Body.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            var direction = speed > 0.1f ? velocity / speed : transform.forward;
            Vector3 position = transform.position;
            float y = WaterSurface.TryGetHeight(position, out float height) ? height + 0.05f : position.y;
            emitter.SetPositionAndRotation(new Vector3(position.x, y, position.z) - direction * radius * 0.8f, Quaternion.identity);

            var emission = foam.emission;
            emission.rateOverTime = speed > MIN_SPEED ? Mathf.Min(MAX_RATE, speed * RATE_PER_SPEED) : 0f;
        }

        private static Texture2D SoftTexture()
        {
            if (softTexture != null)
                return softTexture;
            const int size = 64;
            softTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "WakeSoft" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) / 0.45f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            }
            softTexture.SetPixels32(pixels);
            softTexture.Apply(false, true);
            return softTexture;
        }
    }
}
