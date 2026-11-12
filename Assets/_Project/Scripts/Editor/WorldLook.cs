using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GasSafari.Editor
{
    // Aspecto del mundo (skill level-builder, "Dirección visual"): agua, cinta de corriente, cielo con nubes y niebla.
    // Fuente única para LevelDresser y para el pase de pulido (LevelVisualPass). El agua siempre se distingue de las
    // orillas y del suelo: más saturada y de otro tono que el terreno del mundo (ver LevelDresser.Theme.WallColor/GroundColor).
    public static class WorldLook
    {
        public const string MATS = "Assets/_Project/Art/Materials/World/";

        public sealed class Look
        {
            public Color WaterShallow, WaterDeep, WaterFoam, Ripple, Glint, WaterSky, FlowLine;
            public float RippleStrength = 0.3f, SpecStrength = 1.4f, SparkleDensity = 0.03f, Fresnel = 0.25f;
            public Color SkyTop, SkyHorizon, SkyGround, CloudLit, CloudShade, SunGlow, StarColor = Color.white;
            public float CloudCover = 0.5f, CloudOpacity = 0.9f, CloudSpeed = 0.012f, Stars;
            public Color Fog;
            public float FogStart, FogEnd;
        }

        public static string[] Names = { "T0", "W1", "W2", "W3", "W4", "W5", "W6" };

        // W1: ciudad de noche, azul profundo con reflejos de neón cian y rosa.
        private static readonly Look W1 = new Look
        {
            WaterShallow = new Color(0.07f, 0.34f, 0.82f, 0.93f), WaterDeep = new Color(0.01f, 0.06f, 0.36f, 0.97f),
            WaterFoam = new Color(0.78f, 0.96f, 1f), Ripple = new Color(0.35f, 0.72f, 1f), Glint = new Color(0.7f, 1.7f, 2.3f),
            WaterSky = new Color(0.95f, 0.35f, 0.85f), FlowLine = new Color(0.62f, 0.92f, 1f, 0.4f),
            RippleStrength = 0.35f, SpecStrength = 1.6f, SparkleDensity = 0.04f, Fresnel = 0.35f,
            SkyTop = Hex(0x070C26), SkyHorizon = Hex(0x2E2A66), SkyGround = Hex(0x15163A),
            CloudLit = new Color(0.62f, 0.5f, 0.92f), CloudShade = new Color(0.13f, 0.11f, 0.34f), SunGlow = new Color(0.45f, 0.55f, 1f, 0.12f),
            CloudCover = 0.52f, CloudOpacity = 0.8f, Stars = 0.6f, StarColor = new Color(0.85f, 0.9f, 1f),
            Fog = Hex(0x2E2A66), FogStart = 50f, FogEnd = 1000f,
        };

        // W2: mar abierto, turquesa saturado contra cielo y rocas grises.
        private static readonly Look W2 = new Look
        {
            WaterShallow = new Color(0f, 0.82f, 0.80f, 0.92f), WaterDeep = new Color(0f, 0.30f, 0.58f, 0.97f),
            WaterFoam = new Color(1f, 1f, 1f), Ripple = new Color(0.75f, 1f, 1f), Glint = new Color(2f, 2f, 1.8f),
            WaterSky = new Color(0.7f, 0.88f, 1f), FlowLine = new Color(0.9f, 1f, 1f, 0.4f),
            SkyTop = new Color(0.22f, 0.52f, 0.88f), SkyHorizon = new Color(0.74f, 0.88f, 0.95f), SkyGround = new Color(0.5f, 0.66f, 0.74f),
            CloudLit = Color.white, CloudShade = new Color(0.68f, 0.78f, 0.92f), SunGlow = new Color(1f, 0.95f, 0.7f, 0.4f),
            CloudCover = 0.5f, CloudOpacity = 0.92f,
            Fog = new Color(0.74f, 0.88f, 0.95f), FogStart = 60f, FogEnd = 900f,
        };

        // W2-2 Tormenta: el mismo mar bajo un cielo cubierto.
        private static readonly Look W2Storm = new Look
        {
            WaterShallow = new Color(0f, 0.74f, 0.74f, 0.93f), WaterDeep = new Color(0f, 0.24f, 0.46f, 0.97f),
            WaterFoam = new Color(0.95f, 1f, 1f), Ripple = new Color(0.65f, 0.95f, 1f), Glint = new Color(1.4f, 1.6f, 1.7f),
            WaterSky = new Color(0.6f, 0.7f, 0.76f), FlowLine = new Color(0.85f, 0.95f, 1f, 0.4f),
            SkyTop = Hex(0x58676F), SkyHorizon = Hex(0x93A8B0), SkyGround = Hex(0x6F8088),
            CloudLit = new Color(0.78f, 0.83f, 0.86f), CloudShade = new Color(0.36f, 0.42f, 0.46f), SunGlow = new Color(0.8f, 0.85f, 0.9f, 0.3f),
            CloudCover = 0.28f, CloudOpacity = 0.95f,
            Fog = Hex(0x93A8B0), FogStart = 25f, FogEnd = 600f,
        };

        // W3: selva al atardecer; agua turquesa azulada que no se confunde con el verde ni con la arena.
        private static readonly Look W3 = new Look
        {
            WaterShallow = new Color(0.04f, 0.78f, 0.95f, 0.92f), WaterDeep = new Color(0f, 0.34f, 0.68f, 0.97f),
            WaterFoam = new Color(0.96f, 1f, 1f), Ripple = new Color(0.62f, 1f, 1f), Glint = new Color(2f, 1.9f, 1.5f),
            WaterSky = new Color(1f, 0.85f, 0.6f), FlowLine = new Color(0.9f, 1f, 1f, 0.4f),
            SkyTop = Hex(0x4C9FE0), SkyHorizon = new Color(0.88f, 0.9f, 0.68f), SkyGround = new Color(0.55f, 0.65f, 0.45f),
            CloudLit = new Color(1f, 0.95f, 0.85f), CloudShade = new Color(0.86f, 0.78f, 0.68f), SunGlow = new Color(1f, 0.72f, 0.4f, 0.5f),
            CloudCover = 0.52f,
            Fog = new Color(0.88f, 0.9f, 0.68f), FogStart = 60f, FogEnd = 1100f,
        };

        // W4: cuevas y volcán, agua oscura con brillo cian.
        private static readonly Look W4 = new Look
        {
            WaterShallow = new Color(0.02f, 0.2f, 0.3f, 0.95f), WaterDeep = new Color(0f, 0.04f, 0.12f, 0.98f),
            WaterFoam = new Color(0.55f, 1f, 1f), Ripple = new Color(0.1f, 0.85f, 1f), Glint = new Color(0.4f, 2.4f, 2.8f),
            WaterSky = new Color(0.3f, 0.9f, 1f), FlowLine = new Color(0.25f, 0.92f, 1f, 0.28f),
            RippleStrength = 0.33f, SpecStrength = 1.8f, SparkleDensity = 0.05f, Fresnel = 0.35f,
            SkyTop = Hex(0x0D0616), SkyHorizon = Hex(0x22103A), SkyGround = Hex(0x0A0514),
            CloudLit = new Color(0.34f, 0.22f, 0.5f), CloudShade = new Color(0.1f, 0.05f, 0.18f), SunGlow = new Color(0.2f, 0.7f, 0.9f, 0.1f),
            CloudCover = 0.5f, CloudOpacity = 0.6f,
            Fog = Hex(0x22103A), FogStart = 40f, FogEnd = 700f,
        };

        // W5: río turquesa que contrasta con el cañón rojizo.
        private static readonly Look W5 = new Look
        {
            WaterShallow = new Color(0.04f, 0.86f, 0.86f, 0.92f), WaterDeep = new Color(0f, 0.40f, 0.60f, 0.97f),
            WaterFoam = new Color(1f, 1f, 0.95f), Ripple = new Color(0.55f, 1f, 0.95f), Glint = new Color(2.2f, 2f, 1.6f),
            WaterSky = new Color(1f, 0.7f, 0.5f), FlowLine = new Color(0.9f, 1f, 1f, 0.4f),
            SkyTop = Hex(0x5A4A8C), SkyHorizon = Hex(0xE79A68), SkyGround = Hex(0x9C5A3A),
            CloudLit = new Color(1f, 0.82f, 0.62f), CloudShade = new Color(0.66f, 0.4f, 0.5f), SunGlow = new Color(1f, 0.6f, 0.3f, 0.6f),
            CloudCover = 0.5f,
            Fog = Hex(0xE79A68), FogStart = 70f, FogEnd = 1100f,
        };

        // W6: fiesta nocturna, agua cian eléctrico contra el terreno magenta.
        private static readonly Look W6 = new Look
        {
            WaterShallow = new Color(0.08f, 0.88f, 0.95f, 0.93f), WaterDeep = new Color(0.04f, 0.22f, 0.70f, 0.97f),
            WaterFoam = new Color(0.9f, 1f, 0.9f), Ripple = new Color(0.5f, 1f, 1f), Glint = new Color(1.7f, 2.3f, 0.8f),
            WaterSky = new Color(1f, 0.4f, 0.9f), FlowLine = new Color(0.7f, 1f, 1f, 0.4f),
            RippleStrength = 0.35f, SparkleDensity = 0.05f, Fresnel = 0.3f,
            SkyTop = Hex(0x14072A), SkyHorizon = Hex(0x4C1B58), SkyGround = Hex(0x1E0A2C),
            CloudLit = new Color(0.95f, 0.48f, 0.9f), CloudShade = new Color(0.3f, 0.1f, 0.4f), SunGlow = new Color(0.9f, 0.3f, 0.8f, 0.15f),
            CloudCover = 0.55f, CloudOpacity = 0.8f, Stars = 0.6f,
            Fog = Hex(0x4C1B58), FogStart = 60f, FogEnd = 1000f,
        };

        public static Look For(int world, bool storm = false)
        {
            switch (world)
            {
                case 2: return storm ? W2Storm : W2;
                case 3: return W3;
                case 4: return W4;
                case 5: return W5;
                case 6: return W6;
                default: return W1; // W1 y el tutorial T-0 (el zoológico de la ciudad)
            }
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        // ---------------------------------------------------------------- materiales
        public static Material Water(int world, bool storm = false)
        {
            var look = For(world, storm);
            var m = GetMaterial($"M_Water_W{world}{(storm ? "_Storm" : "")}", "GasSafari/SG_ToonWater");
            m.SetColor("_ShallowColor", look.WaterShallow);
            m.SetColor("_DeepColor", look.WaterDeep);
            m.SetColor("_FoamColor", look.WaterFoam);
            m.SetColor("_RippleColor", look.Ripple);
            m.SetColor("_GlintColor", look.Glint);
            m.SetColor("_SkyColor", look.WaterSky);
            m.SetFloat("_RippleStrength", look.RippleStrength);
            m.SetFloat("_RippleScale", 0.8f);
            m.SetFloat("_SpecStrength", look.SpecStrength);
            m.SetFloat("_SparkleDensity", look.SparkleDensity);
            m.SetFloat("_FresnelStrength", look.Fresnel);
            m.SetFloat("_FoamWidth", 0.18f);
            m.SetFloat("_DepthMax", 2.2f);
            m.SetFloat("_FlowSpeed", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Flow(int world, bool openSea, bool storm = false)
        {
            var look = For(world, storm);
            var m = GetMaterial($"M_WaterFlow_W{world}{(openSea ? "_Sea" : "")}{(storm ? "_Storm" : "")}", "GasSafari/SG_WaterFlow");
            m.SetColor("_LineColor", look.FlowLine);
            m.SetColor("_FoamColor", new Color(look.WaterFoam.r, look.WaterFoam.g, look.WaterFoam.b, 0.95f));
            m.SetFloat("_EdgeFoam", openSea ? 0f : 1f);
            m.SetFloat("_LineLanes", 2.2f);
            m.SetFloat("_LineLength", 0.28f);
            m.SetFloat("_EdgeFade", openSea ? 6f : 0.6f);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Sky(int world, bool storm = false)
        {
            var look = For(world, storm);
            var m = GetMaterial($"M_SkyC_W{world}{(storm ? "_Storm" : "")}", "GasSafari/SG_SkyClouds");
            m.SetColor("_TopColor", look.SkyTop);
            m.SetColor("_HorizonColor", look.SkyHorizon);
            m.SetColor("_GroundColor", look.SkyGround);
            m.SetColor("_CloudColor", look.CloudLit);
            m.SetColor("_CloudShade", look.CloudShade);
            m.SetColor("_SunGlow", look.SunGlow);
            m.SetColor("_StarColor", look.StarColor);
            m.SetFloat("_CloudCover", look.CloudCover);
            m.SetFloat("_CloudOpacity", look.CloudOpacity);
            m.SetFloat("_CloudSpeed", look.CloudSpeed);
            m.SetFloat("_StarAmount", look.Stars);
            EditorUtility.SetDirty(m);
            return m;
        }

        // Material de la estela de la balsa: en Resources para que la balsa lo cargue en cualquier escena y la build incluya el shader.
        [MenuItem("Gas Safari/Visual/Crear material de estela")]
        public static void EnsureWakeMaterial()
        {
            const string folder = "Assets/_Project/Resources/Wake";
            EnsureFolder(folder);
            string path = folder + "/M_Wake.mat";
            var shader = Shader.Find("GasSafari/SG_Wake");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_DstBlend", 10f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        // El fuego de los túneles de W3 era un cuadrado opaco naranja: ahora mancha suave aditiva.
        [MenuItem("Gas Safari/Visual/Arreglar material de fuego")]
        public static void FixFireMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATS + "M_Fire.mat");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/T_SoftDot.png");
            if (material == null || texture == null)
                return;
            material.shader = Shader.Find("GasSafari/SG_Wake");
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_DstBlend", 1f);
            material.SetColor("_Tint", Color.white);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        // Cielo con nubes y niebla lineal del mundo en la escena abierta.
        public static void ApplyEnvironment(int world, bool storm = false)
        {
            var look = For(world, storm);
            RenderSettings.skybox = Sky(world, storm);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog;
            RenderSettings.fogStartDistance = look.FogStart;
            RenderSettings.fogEndDistance = look.FogEnd;
            DynamicGI.UpdateEnvironment();
        }

        private static Material GetMaterial(string name, string shaderName)
        {
            EnsureFolder(MATS);
            string path = MATS + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
