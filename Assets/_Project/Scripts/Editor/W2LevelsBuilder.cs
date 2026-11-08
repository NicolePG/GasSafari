using GasSafari.Core;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Greybox + arte de los niveles del Mundo 2 (docs/04): mar abierto sin paredes (boyas y corriente que devuelve).
    // Cada nivel es una función de este script: se rehace entero al repetirlo. Distancias en metros desde la salida.
    public static class W2LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";

        // ---------------------------------------------------------------- W2-1 Cajas a la Deriva
        [MenuItem("Gas Safari/Setup/Greybox W2-1 Cajas a la Deriva (M5-09)")]
        public static void BuildW2_1()
        {
            var level = GreyboxLevel.Create("W2-1", "L_W2_1_CajasDeriva");
            level.OpenWater();
            level.StartPath(Vector3.zero, 0f).Straight(170f, 18f);                    // S1 campo de cajas flotantes
            float barrels = level.Length;
            level.Arc(60f, 35f, 18f).Straight(110f, 18f);                             // S2 barriles con mecha
            float nets = level.Length;
            level.Straight(130f, 14f);                                                // S3 pasillo entre redes
            float mountain = level.Length;
            level.Arc(25f, -45f, 16f).Straight(30f, 16f).Arc(25f, 90f, 16f).Straight(30f, 16f).Arc(25f, -45f, 16f); // S4 rodeo
            float join = level.Length;
            level.Straight(60f, 18f);
            level.Branch(mountain, join, 10f);                                        // atajo tras la montaña de cajas
            level.BuildChannel(0.4f);                                                 // "corriente" del mar (provisional)

            level.Section(1, "Cajas", 0f);
            level.Section(2, "Barriles", barrels);
            level.Section(3, "Redes", nets);
            level.Section(4, "Montana", mountain);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(barrels);
            level.AddCheckpoint(nets);
            level.AddCheckpoint(mountain - 10f);

            var random = new System.Random(21);
            // S1: cajas flotantes (H07) repartidas; una tiene el mango dentro (romper con Rugido o golpe fuerte).
            for (int i = 0; i < 16; i++)
            {
                float d = 20f + i * 9f;
                float lateral = (float)(random.NextDouble() * 14.0 - 7.0);
                var crate = level.Place(HZ + "H07_Crate.prefab", d, lateral, (float)(random.NextDouble() * 90.0));
                crate.name = "Caja_" + i;
                if (i == 9)
                    level.Mango(0, d, lateral, 0.6f);
            }
            level.PowerUp("P01", 60f, 0f);
            level.PowerUp("P04", 130f, -3f);

            // S2: barriles con mecha (H06); mango entre dos barriles.
            for (int i = 0; i < 8; i++)
            {
                float d = barrels + 20f + i * 16f;
                level.Place(HZ + "H06_ExplosiveBarrel.prefab", d, i % 2 == 0 ? 4f : -4f).name = "Barril_" + i;
            }
            level.Mango(1, barrels + 76f, 0f, 1f);
            level.PowerUp("P01", barrels + 100f, 0f);

            // S3: pasillo entre redes de pesca (H32) que ocupan la mitad del carril, alternadas.
            for (int i = 0; i < 5; i++)
            {
                float d = nets + 18f + i * 22f;
                level.Place(HZ + "H32_FishingNet.prefab", d, i % 2 == 0 ? 3.5f : -3.5f).name = "Red_" + i;
            }
            level.PowerUp("P02", nets + 6f, 0f);
            level.PowerUp("P01", nets + 120f, 0f);

            // S4: montaña de cajas H07 que tapa el atajo; si se derrumba (Rugido, Terremoto o golpe fuerte), atajo y mango.
            var a = level.PointAt(mountain);
            var b = level.PointAt(join);
            var chord = (b - a).normalized;
            var right = Vector3.Cross(Vector3.up, chord);
            var mid = (a + b) * 0.5f;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 5 - row; col++)
                {
                    var crate = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H07_Crate.prefab"), level.Scene);
                    crate.name = "Montana_" + row + "_" + col;
                    crate.transform.SetPositionAndRotation(mid + right * ((col - (4 - row) * 0.5f) * 1.9f) + Vector3.up * (row * 1.8f),
                        Quaternion.LookRotation(chord));
                }
            }
            var mango = level.Mango(2, 0f, 0f, 1f);
            mango.transform.position = mid + chord * 8f + Vector3.up;

            LevelDresser.Dress(level, LevelDresser.W2, 21, level.PointAt(nets, 70f), -90f);
            level.Finish();
            Debug.Log($"M5-09 W2-1 listo: {level.Length:F0} m, atajo {Vector3.Distance(a, b):F0} m");
        }

        // ---------------------------------------------------------------- W2-2 La Tormenta
        [MenuItem("Gas Safari/Setup/Greybox W2-2 La Tormenta (M5-10)")]
        public static void BuildW2_2()
        {
            var level = GreyboxLevel.Create("W2-2", "L_W2_2_Tormenta");
            level.OpenWater();
            level.StartPath(Vector3.zero, 0f).Straight(180f, 18f);                   // S1 lluvia y olas grandes
            float gusts = level.Length;
            level.Arc(70f, -30f, 18f).Straight(130f, 18f);                           // S2 ráfagas alternas
            float rocks = level.Length;
            level.Straight(150f, 18f);                                               // S3 rayos y rocas ocultas
            float beacon = level.Length;
            level.Arc(70f, 30f, 18f).Straight(140f, 18f);                            // S4 faro y ola gigante
            level.BuildChannel(0.4f);

            level.Section(1, "Lluvia", 0f);
            level.Section(2, "Rafagas", gusts);
            level.Section(3, "Rayos", rocks);
            level.Section(4, "Faro", beacon);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(gusts);
            level.AddCheckpoint(rocks);
            level.AddCheckpoint(beacon);

            // S1: olas grandes (H04 con más amplitud) que bajan contra la balsa.
            var storm = Wave("W2_StormWave", "Ola de tormenta", 1.2f, 25f, 4f);
            for (int i = 0; i < 3; i++)
            {
                var wave = level.Place(HZ + "H04_Wave.prefab", 40f + i * 50f, 0f, 180f);
                wave.name = "Ola_" + i;
                SetWave(wave, storm, 18f, 30f, 5f + i, i * 1.5f);
            }
            level.PowerUp("P01", 70f, 2f);
            level.PowerUp("P03", 150f, -3f);

            // S2: ráfagas alternas izquierda/derecha (H08), desfasadas.
            for (int i = 0; i < 4; i++)
            {
                float d = gusts + 20f + i * 32f;
                var wind = level.Place(HZ + "H08_Wind.prefab", d, 0f, i % 2 == 0 ? 0f : 180f);
                wind.name = "Rafaga_" + i;
                var so = new SerializedObject(wind.GetComponent<GasSafari.Hazards.GasWindZone>());
                so.FindProperty("phase").floatValue = i * 2.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            level.PowerUp("P01", gusts + 90f, 0f);

            // S3: rocas que solo se ven con el rayo (el collider siempre está); remolino pequeño con el mango en el ojo.
            var random = new System.Random(22);
            var hidden = new System.Collections.Generic.List<Renderer>();
            var rockModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Environment/W2/W2_Rock_B.fbx");
            for (int i = 0; i < 10; i++)
            {
                float d = rocks + 12f + i * 12f;
                float lateral = (i % 2 == 0 ? 1f : -1f) * (2f + (float)random.NextDouble() * 5f);
                var rock = (GameObject)PrefabUtility.InstantiatePrefab(rockModel, level.Scene);
                rock.name = "RocaOculta_" + i;
                rock.layer = GameLayers.Hazard;
                rock.transform.position = level.PointAt(d, lateral, -0.4f);
                rock.transform.localScale = Vector3.one * 1.4f;
                var collider = rock.AddComponent<SphereCollider>();
                collider.radius = 1.1f;
                hidden.AddRange(rock.GetComponentsInChildren<Renderer>());
                if (i == 6)
                {
                    var mango = level.Mango(1, 0f, 0f, 1f);
                    mango.transform.position = level.PointAt(d + 2.5f, lateral, 1f);    // detrás de la roca
                }
            }
            var pool = level.Place(HZ + "H09_Whirlpool.prefab", rocks + 75f, -6f);
            pool.name = "RemolinoPequeno";
            pool.transform.localScale = Vector3.one * 0.6f;
            level.Mango(0, rocks + 75f, -6f, 0.8f);
            level.Hint(Bots.BotHintType.Whirlpool, rocks + 75f, -6f, 5f);

            // S4: la ola gigante empuja hacia la playa del faro (surfearla en sincronía); mango en su cresta.
            var giant = Wave("W2_GiantWave", "Ola gigante", 2.2f, 40f, 5f);
            var big = level.Place(HZ + "H04_Wave.prefab", beacon + 10f, 0f, 0f);
            big.name = "OlaGigante";
            SetWave(big, giant, 20f, 150f, 14f, 0f);
            level.Mango(2, beacon + 90f, 0f, 2.4f);
            level.PowerUp("P01", beacon + 40f, 0f);
            level.PowerUp("P03", beacon + 120f, 3f);
            var beach = level.Block("PlayaFaro", level.PointAt(level.Length + 6f, 0f, -0.9f), new Vector3(40f, 1f, 14f), new Color(0.85f, 0.78f, 0.55f),
                Quaternion.LookRotation(level.DirectionAt(level.Length)).eulerAngles.y);
            beach.name = "PlayaFaro";

            // Tormenta: día gris, niebla densa, lluvia y rayos.
            var theme = LevelDresser.With(LevelDresser.W2, LevelDresser.W2.Decor, 0.55f, new Color(0.36f, 0.42f, 0.45f));
            theme.SkyTint = new Color(0.3f, 0.34f, 0.38f);
            theme.SkyExposure = 0.6f;
            theme.FogStart = 25f;
            theme.FogEnd = 160f;
            theme.WaterShallow = new Color(0.25f, 0.42f, 0.45f, 0.85f);
            theme.WaterDeep = new Color(0.1f, 0.18f, 0.22f, 0.95f);
            LevelDresser.Dress(level, theme, 22, level.PointAt(level.Length + 20f, 8f), 0f);
            Storm(level, hidden.ToArray());
            level.Finish();
            Debug.Log($"M5-10 W2-2 listo: {level.Length:F0} m");
        }

        private static void Storm(GreyboxLevel level, Renderer[] hidden)
        {
            var sun = System.Array.Find(level.Scene.GetRootGameObjects(), g => g.name == "Directional Light")?.GetComponent<Light>();
            var go = new GameObject("Tormenta");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.AddComponent<GasSafari.VFX.StormLightning>().Configure(sun, hidden, 6f, 0.45f);

            var rain = new GameObject("Lluvia");
            rain.transform.SetParent(go.transform, false);
            rain.AddComponent<GasSafari.VFX.FollowCamera>();
            var ps = rain.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.gravityModifier = 2.2f;
            main.maxParticles = 1500;
            main.startColor = new Color(0.8f, 0.85f, 0.9f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 900f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f);
            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f;
            string path = "Assets/_Project/Art/Materials/World/M_Rain.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            renderer.sharedMaterial = material;
        }

        private static GasSafari.Hazards.HazardConfig Wave(string file, string displayName, float amplitude, float push, float speed)
        {
            string path = "Assets/_Project/Data/Hazards/" + file + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<GasSafari.Hazards.HazardConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GasSafari.Hazards.HazardConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            config.id = "H04";
            config.displayName = displayName;
            config.warningTime = 0.5f;
            config.parameters = new[]
            {
                new GasSafari.Hazards.HazardConfig.Parameter { key = "amplitude", value = amplitude },
                new GasSafari.Hazards.HazardConfig.Parameter { key = "push", value = push },
                new GasSafari.Hazards.HazardConfig.Parameter { key = "crestSpeed", value = speed },
            };
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void SetWave(GameObject go, GasSafari.Hazards.HazardConfig config, float width, float length, float interval, float phase)
        {
            var wave = go.GetComponent<GasSafari.Hazards.WaveHazard>();
            wave.Configure(config);
            var so = new SerializedObject(wave);
            so.FindProperty("width").floatValue = width;
            so.FindProperty("length").floatValue = length;
            so.FindProperty("interval").floatValue = interval;
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
