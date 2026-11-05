using GasSafari.Audio;
using GasSafari.VFX;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Crea los sonidos y partículas provisionales de pedos (M1-17). Se reemplazan con los definitivos en M2.
    public static class ProvisionalFartFeedbackBuilder
    {
        private const string SFX = "Assets/_Project/Audio/SFX/";
        private const string SOUNDS = "Assets/_Project/Data/Sounds/";
        private const string VFX_FOLDER = "Assets/_Project/Prefabs/VFX/";
        private const string URP_PARTICLE_MAT = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        private const float FART_VOLUME = 0.7f;

        internal struct Spec
        {
            public float Duration, LifeMin, LifeMax, SpeedMin, SpeedMax, SizeMin, SizeMax, Angle, Gravity, Rate;
            public short BurstMin, BurstMax;
            public int Max;
            public bool Loop;
        }

        [MenuItem("Gas Safari/Setup/Crear pedos provisionales (audio y VFX)")]
        public static void Build()
        {
            var library = LoadOrCreate<SoundLibrary>(SOUNDS + "SoundLibrary.asset");
            library.entries = new[]
            {
                Entry(SoundId.FartPuff, "fart_provpuff_01", "fart_provpuff_02", "fart_provpuff_03"),
                Entry(SoundId.FartBlast, "fart_provblast_loop_01"),
                Entry(SoundId.FartCharge, "fart_provcharge_loop_01"),
                Entry(SoundId.FartMega, "fart_provmega_01"),
                Entry(SoundId.FartCough, "fart_provcough_01", "fart_provcough_02")
            };
            EditorUtility.SetDirty(library);

            var vfx = LoadOrCreate<VfxLibrary>(VFX_FOLDER + "VfxLibrary.asset");
            vfx.entries = new[]
            {
                VfxEntry(VfxId.FartPuff, "vfx_fart_puff", 8, new Spec
                {
                    Duration = 0.5f, LifeMin = 0.4f, LifeMax = 0.7f, SpeedMin = 1f, SpeedMax = 2.5f,
                    SizeMin = 0.25f, SizeMax = 0.5f, Angle = 25f, Gravity = -0.05f, BurstMin = 8, BurstMax = 12, Max = 12
                }),
                VfxEntry(VfxId.FartBlast, "vfx_fart_blast", 4, new Spec
                {
                    Duration = 1f, Loop = true, LifeMin = 0.3f, LifeMax = 0.5f, SpeedMin = 3f, SpeedMax = 5f,
                    SizeMin = 0.2f, SizeMax = 0.4f, Angle = 12f, Rate = 40f, Max = 30
                }),
                VfxEntry(VfxId.FartMega, "vfx_fart_mega", 4, new Spec
                {
                    Duration = 0.8f, LifeMin = 0.6f, LifeMax = 1f, SpeedMin = 2f, SpeedMax = 5f,
                    SizeMin = 0.5f, SizeMax = 1f, Angle = 45f, BurstMin = 25, BurstMax = 30, Max = 30
                }),
                VfxEntry(VfxId.Cough, "vfx_cough", 4, new Spec
                {
                    Duration = 0.5f, LifeMin = 0.5f, LifeMax = 0.8f, SpeedMin = 0.3f, SpeedMax = 0.8f,
                    SizeMin = 0.15f, SizeMax = 0.3f, Angle = 30f, Gravity = -0.1f, BurstMin = 4, BurstMax = 6, Max = 6
                })
            };
            EditorUtility.SetDirty(vfx);
            AssetDatabase.SaveAssets();
        }

        private static SoundLibrary.Entry Entry(SoundId id, params string[] clipNames)
        {
            var def = LoadOrCreate<SoundDef>(SOUNDS + id + ".asset");
            def.clips = new AudioClip[clipNames.Length];
            for (int i = 0; i < clipNames.Length; i++)
                def.clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(SFX + clipNames[i] + ".wav");
            def.isFart = true;
            def.spatial = true;
            // Margen para el +3 dB del pedo que completa un sync (docs/08 §2).
            def.volume = FART_VOLUME;
            EditorUtility.SetDirty(def);
            return new SoundLibrary.Entry { id = id, sound = def };
        }

        internal static VfxLibrary.Entry VfxEntry(VfxId id, string name, int prewarm, Spec spec)
        {
            return new VfxLibrary.Entry { id = id, prefab = BuildParticles(name, spec), prewarm = prewarm };
        }

        private static PooledVfx BuildParticles(string name, Spec spec)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = spec.Duration;
            main.loop = spec.Loop;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.LifeMin, spec.LifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.SpeedMin, spec.SpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.SizeMin, spec.SizeMax);
            main.gravityModifier = spec.Gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = spec.Max;
            main.stopAction = ParticleSystemStopAction.Callback;

            var emission = ps.emission;
            emission.rateOverTime = spec.Rate;
            if (spec.BurstMax > 0)
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, spec.BurstMin, spec.BurstMax) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = spec.Angle;
            shape.radius = 0.05f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.3f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial();
            go.AddComponent<PooledVfx>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, VFX_FOLDER + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PooledVfx>();
        }

        private static Material ParticleMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(URP_PARTICLE_MAT);
            if (mat != null)
                return mat;
            const string path = "Assets/_Project/Art/VFX/M_Particle_Provisional.mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
