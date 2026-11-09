using System.Collections.Generic;
using System.Linq;
using GasSafari.Core;
using GasSafari.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Pase de arte de un nivel ya hecho en greybox (skill level-builder, "Dirección visual"): luz, cielo y niebla del
    // mundo, agua SG_ToonWater con su color, Volume del mundo, orillas decoradas cada 3-5 m con variantes rotadas y
    // escaladas, farolas emisivas con algunas luces, hito del mundo con partes móviles y static batching.
    public static class LevelDresser
    {
        public sealed class Theme
        {
            public int World;
            public Color LightColor;
            public float LightIntensity;
            public Vector3 LightEuler;
            public Color AmbientSky, AmbientEquator, AmbientGround;
            public Color Fog;
            public float FogStart, FogEnd;
            public Color SkyTint, SkyGround;
            public float SkyExposure;
            public Color WaterShallow, WaterDeep, WaterFoam;
            public Color WallColor;
            public Color GroundColor;
            public string[] Decor;
            public string[] Lamps;
            public Color LampLight;
            public string Landmark;
            // Mar abierto: sin aceras; los props de la orilla flotan en el agua marcando el carril.
            public bool OpenSea;
        }

        private const string ENV = "Assets/_Project/Art/Environment/";
        private const string MATS = "Assets/_Project/Art/Materials/World/";
        private const float STEP_MIN = 3f;
        private const float STEP_MAX = 5f;
        private const float BANK_OFFSET = 2.6f;   // m desde el borde del agua (detrás de la pared)
        private const float MIN_CLEARANCE = 1.8f; // nunca dentro de otro tramo del cauce
        private const int LAMP_EVERY = 3;
        private const int LIGHT_EVERY = 2;
        private const float GROUND_TOP = 0.3f;    // acera: sobre el agua (y = 0)
        private const float GROUND_WIDTH = 9f;        // de cada 2 farolas, 1 lleva luz real (las demás, solo emisivo + Bloom)

        // W1 (docs/04 Mundos): ciudad de noche, azul #1B2A4A, neón #FF4FA3 / #3AE1FF, farolas ámbar.
        public static readonly Theme W1 = new Theme
        {
            World = 1,
            LightColor = new Color(0.55f, 0.65f, 1f),
            LightIntensity = 0.45f,
            LightEuler = new Vector3(40f, -35f, 0f),
            AmbientSky = new Color(0.16f, 0.22f, 0.42f),
            AmbientEquator = new Color(0.11f, 0.16f, 0.29f),
            AmbientGround = new Color(0.05f, 0.06f, 0.1f),
            Fog = new Color(0.106f, 0.165f, 0.29f),
            FogStart = 45f,
            FogEnd = 230f,
            SkyTint = new Color(0.12f, 0.18f, 0.42f),
            SkyGround = new Color(0.05f, 0.07f, 0.13f),
            SkyExposure = 0.35f,
            WaterShallow = new Color(0.18f, 0.32f, 0.55f, 0.8f),
            WaterDeep = new Color(0.07f, 0.11f, 0.23f, 0.95f),
            WaterFoam = new Color(0.62f, 0.9f, 1f, 1f),
            WallColor = new Color(0.36f, 0.4f, 0.54f),
            GroundColor = new Color(0.27f, 0.29f, 0.38f),
            Decor = new[]
            {
                "W1_Bench_A", "W1_Bench_B", "W1_Planter", "W1_Topiary_A", "W1_Topiary_B", "W1_Sign_A", "W1_Sign_B", "W1_Sign_C",
                "W1_Barrel_A", "W1_Barrel_B", "W1_Barrel_C", "W1_Crate_A", "W1_Crate_B", "W1_Crate_C", "W1_FruitBasket", "W1_Flagpole",
            },
            Lamps = new[] { "W1_Lamp_A", "W1_Lamp_B" },
            LampLight = new Color(1f, 0.7f, 0.22f),
            Landmark = "W1_FerrisWheel",
        };

        // Variante del mundo con otra lista de props (p. ej. alcantarillas: sin bancas ni macetas) y otra luz.
        public static Theme With(Theme theme, string[] decor, float lightIntensity, Color fog)
        {
            var copy = (Theme)typeof(Theme).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(theme, null);
            copy.Decor = decor;
            copy.LightIntensity = lightIntensity;
            copy.Fog = fog;
            return copy;
        }

        // W2 (docs/04 Mundos): mar y tormenta, turquesa #2EC4B6, gris #5C6B73, espuma; día soleado.
        public static readonly Theme W2 = new Theme
        {
            World = 2,
            LightColor = new Color(1f, 0.96f, 0.86f),
            LightIntensity = 1.25f,
            LightEuler = new Vector3(50f, -30f, 0f),
            AmbientSky = new Color(0.62f, 0.8f, 0.95f),
            AmbientEquator = new Color(0.5f, 0.7f, 0.75f),
            AmbientGround = new Color(0.25f, 0.35f, 0.38f),
            Fog = new Color(0.68f, 0.86f, 0.92f),
            FogStart = 70f,
            FogEnd = 320f,
            SkyTint = new Color(0.45f, 0.7f, 0.95f),
            SkyGround = new Color(0.35f, 0.5f, 0.55f),
            SkyExposure = 1.2f,
            WaterShallow = new Color(0.18f, 0.77f, 0.71f, 0.8f),
            WaterDeep = new Color(0.05f, 0.33f, 0.45f, 0.95f),
            WaterFoam = new Color(1f, 1f, 1f, 1f),
            WallColor = new Color(0.36f, 0.42f, 0.45f),
            GroundColor = new Color(0.36f, 0.42f, 0.45f),
            Decor = new[] { "W2_Buoy_A", "W2_Buoy_B", "W2_Buoy_C", "W2_Rock_A", "W2_Rock_B", "W2_Rock_C", "W2_LifeRing", "W2_Barrel_B", "W2_Kelp" },
            Lamps = new[] { "W2_Buoy_A", "W2_Buoy_C" },
            LampLight = new Color(1f, 0.9f, 0.6f),
            Landmark = "W2_Lighthouse",
            OpenSea = true,
        };

        // W3 (docs/04 Mundos): playa y selva al atardecer, verde #2D6A4F, naranja #F77F00, amarillo; antorchas tiki.
        public static readonly Theme W3 = new Theme
        {
            World = 3,
            LightColor = new Color(1f, 0.78f, 0.5f),
            LightIntensity = 1.05f,
            LightEuler = new Vector3(35f, -50f, 0f),
            AmbientSky = new Color(0.55f, 0.72f, 0.6f),
            AmbientEquator = new Color(0.36f, 0.5f, 0.38f),
            AmbientGround = new Color(0.16f, 0.22f, 0.15f),
            Fog = new Color(0.62f, 0.78f, 0.6f),
            FogStart = 45f,
            FogEnd = 260f,
            SkyTint = new Color(0.95f, 0.62f, 0.35f),
            SkyGround = new Color(0.2f, 0.3f, 0.2f),
            SkyExposure = 1.0f,
            WaterShallow = new Color(0.36f, 0.72f, 0.45f, 0.8f),
            WaterDeep = new Color(0.1f, 0.33f, 0.25f, 0.95f),
            WaterFoam = new Color(0.95f, 1f, 0.85f, 1f),
            WallColor = new Color(0.26f, 0.42f, 0.28f),
            GroundColor = new Color(0.45f, 0.36f, 0.22f),
            Decor = new[]
            {
                "W3_Palm_A", "W3_Palm_B", "W3_Palm_C", "W3_Baobab_A", "W3_Bamboo", "W3_Fern", "W3_TropicalFlower", "W3_Rock_A", "W3_Rock_B",
                "W3_Rock_C", "W3_FruitCrate_A", "W3_FruitCrate_B", "W3_Roots", "W3_Sandcastle_B",
            },
            Lamps = new[] { "W3_TikiTorch" },
            LampLight = new Color(1f, 0.55f, 0.12f),
            Landmark = "W3_Baobab_B",
        };

        // W4 (docs/04 Mundos): cuevas y volcán, morado #3C1642, cian #4CC9F0, rojo magma #D00000; faroles de cristal.
        public static readonly Theme W4 = new Theme
        {
            World = 4,
            LightColor = new Color(0.55f, 0.7f, 1f),
            LightIntensity = 0.5f,
            LightEuler = new Vector3(60f, 20f, 0f),
            AmbientSky = new Color(0.24f, 0.12f, 0.3f),
            AmbientEquator = new Color(0.17f, 0.09f, 0.22f),
            AmbientGround = new Color(0.12f, 0.03f, 0.04f),
            Fog = new Color(0.14f, 0.07f, 0.19f),
            FogStart = 25f,
            FogEnd = 150f,
            SkyTint = new Color(0.16f, 0.08f, 0.22f),
            SkyGround = new Color(0.08f, 0.03f, 0.1f),
            SkyExposure = 0.3f,
            WaterShallow = new Color(0.3f, 0.79f, 0.94f, 0.78f),
            WaterDeep = new Color(0.14f, 0.1f, 0.35f, 0.95f),
            WaterFoam = new Color(0.8f, 0.95f, 1f, 1f),
            WallColor = new Color(0.24f, 0.16f, 0.3f),
            GroundColor = new Color(0.17f, 0.1f, 0.2f),
            Decor = new[]
            {
                "W4_CaveRock_A", "W4_CaveRock_B", "W4_CaveRock_C", "W4_Crystal_A", "W4_Crystal_B", "W4_Crystal_C", "W4_GlowMushrooms",
                "W4_Stalagmite_A", "W4_Stalagmite_B", "W4_LavaRock",
            },
            Lamps = new[] { "W4_Lantern" },
            LampLight = new Color(0.3f, 0.8f, 0.95f),
            Landmark = "W4_GiantCrystal",
        };

        // W5 (docs/04 Mundos): cañón al atardecer, dorado #FFB703, rojo #9C3D1C y naranja #FB8500; faroles de papel cálidos.
        public static readonly Theme W5 = new Theme
        {
            World = 5,
            LightColor = new Color(1f, 0.72f, 0.4f),
            LightIntensity = 1.1f,
            LightEuler = new Vector3(24f, -55f, 0f),
            AmbientSky = new Color(0.6f, 0.38f, 0.3f),
            AmbientEquator = new Color(0.5f, 0.3f, 0.2f),
            AmbientGround = new Color(0.28f, 0.14f, 0.09f),
            Fog = new Color(0.82f, 0.48f, 0.27f),
            FogStart = 70f,
            FogEnd = 340f,
            SkyTint = new Color(0.95f, 0.55f, 0.3f),
            SkyGround = new Color(0.38f, 0.2f, 0.11f),
            SkyExposure = 1.0f,
            WaterShallow = new Color(0.98f, 0.64f, 0.18f, 0.8f),
            WaterDeep = new Color(0.5f, 0.2f, 0.1f, 0.95f),
            WaterFoam = new Color(1f, 0.92f, 0.75f, 1f),
            WallColor = new Color(0.61f, 0.24f, 0.11f),
            GroundColor = new Color(0.78f, 0.45f, 0.2f),
            Decor = new[]
            {
                "W5_CanyonRock_A", "W5_CanyonRock_B", "W5_CanyonRock_C", "W5_Cactus_A", "W5_Cactus_B", "W5_DesertFlowers", "W5_Tumbleweed", "W5_Sign",
            },
            Lamps = new[] { "W5_Lanterns" },
            LampLight = new Color(1f, 0.72f, 0.2f),
            Landmark = "W5_NaturalArch",
        };

        // W6 (docs/04 Mundos): fiesta nocturna en el Reino de Julien, magenta #F72585 y lima #B5E48C; antorchas tiki magenta y Bloom fuerte.
        public static readonly Theme W6 = new Theme
        {
            World = 6,
            LightColor = new Color(0.85f, 0.5f, 0.95f),
            LightIntensity = 0.55f,
            LightEuler = new Vector3(50f, 30f, 0f),
            AmbientSky = new Color(0.3f, 0.11f, 0.36f),
            AmbientEquator = new Color(0.21f, 0.07f, 0.27f),
            AmbientGround = new Color(0.06f, 0.02f, 0.08f),
            Fog = new Color(0.17f, 0.05f, 0.22f),
            FogStart = 30f,
            FogEnd = 190f,
            SkyTint = new Color(0.22f, 0.06f, 0.28f),
            SkyGround = new Color(0.05f, 0.02f, 0.07f),
            SkyExposure = 0.3f,
            WaterShallow = new Color(0.71f, 0.89f, 0.55f, 0.8f),
            WaterDeep = new Color(0.45f, 0.07f, 0.3f, 0.95f),
            WaterFoam = new Color(0.95f, 1f, 0.8f, 1f),
            WallColor = new Color(0.46f, 0.1f, 0.32f),
            GroundColor = new Color(0.2f, 0.06f, 0.26f),
            Decor = new[]
            {
                "W6_Balloons_A", "W6_Balloons_B", "W6_Balloons_C", "W6_Garland_A", "W6_Garland_B", "W6_Garland_C", "W6_GiftBox_A", "W6_GiftBox_B",
                "W6_GiftBox_C", "W6_Palm_A", "W6_Palm_B", "W6_Palm_C", "W6_Rock_A", "W6_Rock_B", "W6_Rock_C", "W6_SpeakerStack", "W6_Buffet",
            },
            Lamps = new[] { "W6_TikiTorch" },
            LampLight = new Color(0.97f, 0.15f, 0.52f),
            Landmark = "W6_Stage",
        };

        // landmark: posición y giro del hito (se ve desde la salida y enmarca el momento estrella).
        public static void Dress(GreyboxLevel level, Theme theme, int seed, Vector3 landmark, float landmarkYaw)
        {
            EnsureFolder(MATS);
            var root = new GameObject("Art");
            SceneManager.MoveGameObjectToScene(root, level.Scene);
            Lighting(level.Scene, theme);
            Water(level.Scene, theme);
            Walls(level.Scene, theme);
            if (!theme.OpenSea)
                Ground(level, theme, root.transform);
            Banks(level, theme, seed, root.transform);
            Landmark(theme, landmark, landmarkYaw, root.transform);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Spinner>() == null && t.GetComponentInParent<Spinner>() == null)
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }

        // ---------------------------------------------------------------- ambiente
        private static void Lighting(Scene scene, Theme theme)
        {
            var light = Find(scene, "Directional Light")?.GetComponent<Light>();
            if (light != null)
            {
                light.color = theme.LightColor;
                light.intensity = theme.LightIntensity;
                light.transform.rotation = Quaternion.Euler(theme.LightEuler);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.AmbientSky;
            RenderSettings.ambientEquatorColor = theme.AmbientEquator;
            RenderSettings.ambientGroundColor = theme.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.Fog;
            RenderSettings.fogStartDistance = theme.FogStart;
            RenderSettings.fogEndDistance = theme.FogEnd;

            var sky = Material($"M_Sky_W{theme.World}", "Skybox/Procedural");
            sky.SetColor("_SkyTint", theme.SkyTint);
            sky.SetColor("_GroundColor", theme.SkyGround);
            sky.SetFloat("_Exposure", theme.SkyExposure);
            sky.SetFloat("_AtmosphereThickness", 0.55f);
            sky.SetFloat("_SunSize", 0f);
            RenderSettings.skybox = sky;

            var volume = Find(scene, "Volume")?.GetComponent<Volume>();
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"Assets/_Project/Settings/Volumes/Volume_W{theme.World}.asset");
            if (volume != null && profile != null)
                volume.sharedProfile = profile;
        }

        private static void Water(Scene scene, Theme theme)
        {
            var water = Find(scene, "Water")?.GetComponent<Renderer>();
            if (water == null)
                return;
            var material = Material($"M_Water_W{theme.World}", "GasSafari/SG_ToonWater");
            material.SetColor("_ShallowColor", theme.WaterShallow);
            material.SetColor("_DeepColor", theme.WaterDeep);
            material.SetColor("_FoamColor", theme.WaterFoam);
            material.SetFloat("_FlowSpeed", 0.15f);
            water.sharedMaterial = material;
        }

        private static void Walls(Scene scene, Theme theme)
        {
            var material = Material($"M_Wall_W{theme.World}", "GasSafari/SG_Toon");
            material.SetColor("_BaseColor", theme.WallColor);
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.gameObject.name == "Wall" && r.gameObject.layer == GameLayers.Environment)
                        r.sharedMaterial = material;
                }
            }
        }

        // ---------------------------------------------------------------- orillas
        private static void Banks(GreyboxLevel level, Theme theme, int seed, Transform root)
        {
            var random = new System.Random(seed);
            var parent = new GameObject("Banks").transform;
            parent.SetParent(root, false);
            var lampMaterialLight = theme.LampLight;
            Physics.SyncTransforms();
            int placed = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                int count = 0;
                float d = (float)random.NextDouble() * STEP_MAX;
                while (d < level.Length)
                {
                    float offset = level.WidthAt(d) * 0.5f + BANK_OFFSET + (float)random.NextDouble() * 1.2f;
                    var position = level.PointAt(d, side * offset);
                    bool lamp = count % LAMP_EVERY == 0;
                    if (level.ClearanceTo(position) >= MIN_CLEARANCE && Free(position))
                    {
                        string name = lamp ? theme.Lamps[(count / LAMP_EVERY) % theme.Lamps.Length]
                                           : theme.Decor[random.Next(theme.Decor.Length)];
                        var prop = Spawn(theme, name, parent);
                        if (prop != null)
                        {
                            var toChannel = Quaternion.LookRotation(Vector3.Cross(Vector3.up, level.DirectionAt(d)) * -side); // de cara al agua
                            float yaw = lamp ? 0f : (float)(random.NextDouble() * 30.0 - 15.0);
                            prop.transform.SetPositionAndRotation(position + Vector3.up * (theme.OpenSea ? -0.2f : GROUND_TOP), toChannel * Quaternion.Euler(0f, yaw, 0f));
                            float scale = 0.9f + (float)random.NextDouble() * 0.2f;
                            prop.transform.localScale = Vector3.one * scale;
                            if (lamp && !theme.OpenSea && (count / LAMP_EVERY) % LIGHT_EVERY == 0)
                                AddLampLight(prop, lampMaterialLight);
                            placed++;
                        }
                    }
                    count++;
                    d += STEP_MIN + (float)random.NextDouble() * (STEP_MAX - STEP_MIN);
                }
            }
            Debug.Log($"Arte: {placed} props en las orillas");
        }

        // Aceras a los dos lados del cauce, por tramos de 4 m; un tramo que taparía otro cauce cercano no se pone.
        private static void Ground(GreyboxLevel level, Theme theme, Transform root)
        {
            var parent = new GameObject("Ground").transform;
            parent.SetParent(root, false);
            var material = Material($"M_Ground_W{theme.World}", "GasSafari/SG_Toon");
            material.SetColor("_BaseColor", theme.GroundColor);
            const float step = 4f;
            for (float d = 0f; d < level.Length; d += step)
            {
                float mid = Mathf.Min(d + step * 0.5f, level.Length);
                var dir = level.DirectionAt(mid);
                for (int side = -1; side <= 1; side += 2)
                {
                    float offset = level.WidthAt(mid) * 0.5f + 1f + GROUND_WIDTH * 0.5f;
                    var center = level.PointAt(mid, side * offset);
                    if (level.ClearanceTo(center) < GROUND_WIDTH * 0.5f + 0.5f)
                        continue;
                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(slab.GetComponent<Collider>());
                    slab.name = "Acera";
                    slab.transform.SetParent(parent, false);
                    slab.transform.SetPositionAndRotation(new Vector3(center.x, center.y + GROUND_TOP - 0.4f, center.z), Quaternion.LookRotation(dir));
                    slab.transform.localScale = new Vector3(GROUND_WIDTH, 0.8f, step + 0.6f);
                    slab.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
            }
        }

        // Sin chocar con lo que ya hay (estatuas, rejas, arbustos): solo colliders sólidos del entorno y obstáculos.
        private static bool Free(Vector3 position)
        {
            int mask = (1 << GameLayers.Environment) | (1 << GameLayers.Hazard);
            var hits = Physics.OverlapSphere(position + Vector3.up, 1.2f, mask, QueryTriggerInteraction.Ignore);
            return hits.All(h => h.gameObject.name == "Wall");
        }

        private static void AddLampLight(GameObject lamp, Color color)
        {
            var bounds = Bounds(lamp);
            var go = new GameObject("LampLight");
            go.transform.SetParent(lamp.transform, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.4f, bounds.center.z);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 9f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- hito
        private static void Landmark(Theme theme, Vector3 position, float yaw, Transform root)
        {
            var landmark = Spawn(theme, theme.Landmark, root);
            if (landmark == null)
                return;
            landmark.transform.SetPositionAndRotation(position + Vector3.up * GROUND_TOP, Quaternion.Euler(0f, yaw, 0f));
            // Noria: la rueda gira sobre su eje (el más delgado de su malla) y lleva las cabinas.
            var wheel = landmark.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Wheel");
            if (wheel == null)
                return;
            var cabins = landmark.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Cabin") && t.parent != wheel).ToArray();
            var spinner = wheel.gameObject.AddComponent<Spinner>();
            spinner.Configure(ThinAxis(wheel), 10f, cabins);
        }

        private static Vector3 ThinAxis(Transform wheel)
        {
            var bounds = Bounds(wheel.gameObject);
            var size = bounds.size;
            var world = size.x < size.y && size.x < size.z ? Vector3.right : size.y < size.z ? Vector3.up : Vector3.forward;
            return wheel.parent != null ? wheel.parent.InverseTransformDirection(world) : world;
        }

        // ---------------------------------------------------------------- utilidades
        private static GameObject Spawn(Theme theme, string name, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ENV}W{theme.World}/{name}.fbx");
            if (asset == null)
            {
                Debug.LogWarning($"Arte: falta {name}.fbx");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = name;
            return go;
        }

        private static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);
            var b = renderers[0].bounds;
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
            return b;
        }

        private static GameObject Find(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);

        private static Material Material(string name, string shader)
        {
            string path = MATS + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            EditorUtility.SetDirty(material);
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
