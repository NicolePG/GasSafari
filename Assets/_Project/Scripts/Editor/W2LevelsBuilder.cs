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
            W2StarShots.Apply(level.Scene, "W2-1");
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
            W2StarShots.Apply(level.Scene, "W2-2");
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

        internal static GasSafari.Hazards.HazardConfig Wave(string file, string displayName, float amplitude, float push, float speed)
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

        internal static void SetWave(GameObject go, GasSafari.Hazards.HazardConfig config, float width, float length, float interval, float phase)
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

        // ---------------------------------------------------------------- W2-3 Cementerio de Barcos
        private const string ENV2 = "Assets/_Project/Art/Environment/W2/";
        private static readonly Color HullColor = new Color(0.3f, 0.26f, 0.22f);

        [MenuItem("Gas Safari/Setup/Greybox W2-3 Cementerio de Barcos (M5-11)")]
        public static void BuildW2_3()
        {
            var level = GreyboxLevel.Create("W2-3", "L_W2_3_CementerioBarcos");
            level.OpenWater();
            level.StartPath(Vector3.zero, 0f).Straight(170f, 12f);                  // S1 pasillos entre cascos
            float masts = level.Length;
            float[] turns = new float[3];
            turns[0] = level.Length; level.Arc(14f, 180f, 10f).Straight(30f, 10f);   // S2 mástiles con ancla en curvas de 180°
            turns[1] = level.Length; level.Arc(14f, -180f, 10f).Straight(30f, 10f);
            turns[2] = level.Length; level.Arc(14f, 180f, 10f).Straight(40f, 10f);
            float hold = level.Length;
            level.Straight(100f, 10f);                                               // S3 bodega inundada
            float exit = level.Length;
            level.Straight(150f, 12f);                                               // S4 salida entre dos barcos que se juntan
            level.BuildChannel(0.4f);

            level.Section(1, "Cascos", 0f);
            level.Section(2, "Mastiles", masts);
            level.Section(3, "Bodega", hold);
            level.Section(4, "Salida", exit);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(masts);
            level.AddCheckpoint(hold);
            level.AddCheckpoint(exit);

            // S1: cascos hundidos a los dos lados, escalonados (hacen de paredes del pasillo).
            for (int i = 0; i < 9; i++)
            {
                float d = 15f + i * 18f;
                int side = i % 2 == 0 ? 1 : -1;
                Hull(level, "Casco_" + i, d, side, side > 0 ? 0f : 180f);
            }
            level.PowerUp("P01", 60f, 0f);
            level.PowerUp("P04", 120f, 0f);

            // S2: un mástil con ancla en el centro de cada curva de 180°; mango en la cofa del primero.
            float[] sides = { 1f, -1f, 1f };
            for (int i = 0; i < 3; i++)
            {
                var center = level.PointAt(turns[i], sides[i] * 14f);
                Mast(level, "Mastil_" + (i + 1), center, 1f);
            }
            var nest = level.Mango(0, 0f, 0f, 1f);
            nest.transform.position = level.PointAt(turns[0], 14f) + Vector3.up * 6f + level.DirectionAt(turns[0]) * 0f;
            level.PowerUp("P01", turns[1] + 20f, 0f);

            // S3: bodega inundada: paredes de casco, techo de vigas bajas (penumbra) y luz tenue; mango dentro.
            for (float d = hold + 4f; d < exit - 2f; d += 5f)
            {
                var beam = level.Block("Viga", level.PointAt(d, 0f, 3.6f), new Vector3(13f, 0.5f, 1.4f), HullColor,
                    Quaternion.LookRotation(level.DirectionAt(d)).eulerAngles.y);
                Object.DestroyImmediate(beam.GetComponent<Collider>());
            }
            for (int side = -1; side <= 1; side += 2)
                level.Block("ParedBodega", level.PointAt((hold + exit) * 0.5f, side * 6.3f), new Vector3(1f, 3.5f, exit - hold), HullColor,
                    Quaternion.LookRotation(level.DirectionAt(hold)).eulerAngles.y);
            for (float d = hold + 15f; d < exit; d += 30f)
            {
                var lamp = new GameObject("FarolBodega");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp, level.Scene);
                lamp.transform.position = level.PointAt(d, 0f, 3f);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.75f, 0.4f);
                light.range = 10f;
                light.intensity = 1.6f;
                light.shadows = LightShadows.None;
            }
            level.Mango(1, hold + 55f, -3.5f, 1f);
            level.PowerUp("P01", hold + 30f, 2f);

            // S4: el gran mástil (ancla para la Honda de la Selva) y dos barcos que se juntan lentamente sobre la salida.
            Mast(level, "GranMastil", level.PointAt(exit + 40f, 9f), 1.6f);
            for (int side = -1; side <= 1; side += 2)
            {
                float d = exit + 80f;
                var ship = level.Place(HZ + "H05_MovingBlocker.prefab", d, side * 11f, side > 0 ? -90f : 90f);
                ship.name = "BarcoQueSeCierra_" + (side > 0 ? "D" : "I");
                ship.transform.localScale = new Vector3(2f, 1.5f, 3f);
                var so = new SerializedObject(ship.GetComponent<GasSafari.Hazards.MovingBlocker>());
                so.FindProperty("travel").vector3Value = new Vector3(0f, 0f, 4.5f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // Mango escondido tras el timón de un barco (orilla izquierda de la salida).
            var wheel = level.Block("Timon", level.PointAt(exit + 110f, -8.5f), new Vector3(2.5f, 2.5f, 1f), HullColor,
                Quaternion.LookRotation(level.DirectionAt(exit + 110f)).eulerAngles.y);
            wheel.layer = GameLayers.Environment;
            level.Mango(2, exit + 112f, -8.5f, 1f);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W2, new[] { "W2_Rock_A", "W2_Rock_C", "W2_Barrel_A", "W2_Crate_A", "W2_Crate_B", "W2_LifeRing", "W2_Bottle", "W2_Rowboat" },
                0.9f, new Color(0.55f, 0.68f, 0.72f)), 23, level.PointAt(exit + 60f, 70f), -90f);
            W2StarShots.Apply(level.Scene, "W2-3");
            level.Finish();
            Debug.Log($"M5-11 W2-3 listo: {level.Length:F0} m");
        }

        // Casco a lo largo del cauce, con su cara interior a 6.5 m del centro (side = +1 derecha, -1 izquierda).
        private static void Hull(GreyboxLevel level, string name, float d, int side, float yaw)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ENV2 + "W2_Shipwreck.fbx");
            var hull = (GameObject)PrefabUtility.InstantiatePrefab(model, level.Scene);
            hull.name = name;
            hull.transform.SetPositionAndRotation(level.PointAt(d, side * 6.5f, -0.6f), Quaternion.LookRotation(level.DirectionAt(d)) * Quaternion.Euler(0f, yaw, 0f));
            var first = hull.GetComponentsInChildren<Renderer>();
            var span = first[0].bounds;
            foreach (var r in first)
                span.Encapsulate(r.bounds);
            var right = Vector3.Cross(Vector3.up, level.DirectionAt(d));
            float inner = Vector3.Dot(span.center - hull.transform.position, right * side) - Mathf.Abs(Vector3.Dot(span.extents, new Vector3(Mathf.Abs(right.x), 0f, Mathf.Abs(right.z))));
            hull.transform.position += right * side * (-inner);
            hull.layer = GameLayers.Environment;
            var renderers = hull.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            var box = hull.AddComponent<BoxCollider>();
            box.center = hull.transform.InverseTransformPoint(bounds.center);
            var local = hull.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
        }

        private static void Mast(GreyboxLevel level, string name, Vector3 position, float scale)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ENV2 + "W2_Mast.fbx");
            var mast = (GameObject)PrefabUtility.InstantiatePrefab(model, level.Scene);
            mast.name = name;
            mast.transform.position = position + Vector3.down * 0.5f;
            mast.transform.localScale = Vector3.one * scale;
            var anchor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "Anchor_Post.prefab"), level.Scene);
            anchor.name = name + "_Ancla";
            anchor.transform.position = position + Vector3.down * 0.4f;
        }

        // ---------------------------------------------------------------- W2-4 El Gran Remolino
        private const float WHIRL_PREFAB_RADIUS = 8f;

        [MenuItem("Gas Safari/Setup/Greybox W2-4 El Gran Remolino (M5-12)")]
        public static void BuildW2_4()
        {
            var level = GreyboxLevel.Create("W2-4", "L_W2_4_GranRemolino");
            level.OpenWater();
            level.StartPath(Vector3.zero, 0f).Straight(150f, 20f);                  // S1 remolino de práctica
            float slalom = level.Length;
            level.Straight(160f, 22f);                                               // S2 slalom entre dos remolinos
            float chain = level.Length;
            level.Arc(80f, 30f, 20f).Straight(130f, 20f);                           // S3 cadena de 3 con corrientes cruzadas
            float great = level.Length;
            level.Straight(150f, 34f).Straight(40f, 20f);                           // S4 el gran remolino (radio 15 m)
            level.BuildChannel(0.4f);

            level.Section(1, "Practica", 0f);
            level.Section(2, "Slalom", slalom);
            level.Section(3, "Cadena", chain);
            level.Section(4, "GranRemolino", great);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(slalom);
            level.AddCheckpoint(chain);
            level.AddCheckpoint(great);

            Pool(level, "Practica", 70f, 6f, 6f);
            level.Mango(0, 70f, 0.4f, 0.8f);                                         // en el borde del primer remolino
            level.PowerUp("P01", 30f, 0f);

            Pool(level, "Slalom_1", slalom + 45f, -8f, 6f);
            Pool(level, "Slalom_2", slalom + 95f, 8f, 6f);
            level.Mango(1, slalom + 70f, 0f, 1f);                                    // entre los dos
            level.PowerUp("P01", slalom + 140f, 0f);

            float[] chainAt = { 30f, 80f, 130f };
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Pool(level, "Cadena_" + (i + 1), chain + chainAt[i], side * 7f, 6f);
                // Corriente cruzada que empuja hacia el remolino siguiente.
                var jet = level.Place(HZ + "Jet_Lateral.prefab", chain + chainAt[i] + 25f, side * 3f, side > 0 ? 180f : 0f);
                jet.name = "CorrienteCruzada_" + (i + 1);
                foreach (var r in jet.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
            }
            level.PowerUp("P03", chain + 105f, 0f);

            Pool(level, "GranRemolino", great + 70f, 18f, 15f);                            // el carril lo bordea por fuera
            level.Mango(2, great + 70f, 18f, 0.8f);                                  // en el centro: entrar y salir con Panzazo o sync x4
            level.PowerUp("P01", great + 20f, -4f);

            LevelDresser.Dress(level, LevelDresser.W2, 24, level.PointAt(great + 70f, 80f), -90f);
            W2StarShots.Apply(level.Scene, "W2-4");
            level.Finish();
            Debug.Log($"M5-12 W2-4 listo: {level.Length:F0} m");
        }

        private static void Pool(GreyboxLevel level, string name, float d, float lateral, float radius)
        {
            var pool = level.Place(HZ + "H09_Whirlpool.prefab", d, lateral);
            pool.name = "Remolino_" + name;
            pool.transform.localScale = Vector3.one * (radius / WHIRL_PREFAB_RADIUS);
            level.Hint(Bots.BotHintType.Whirlpool, d, lateral, radius);
        }

        // ---------------------------------------------------------------- W2-5 Arrecife de Coral (jefe)
        private static readonly Color CaveColor = new Color(0.45f, 0.38f, 0.42f);

        [MenuItem("Gas Safari/Setup/Greybox W2-5 Arrecife de Coral (M5-13)")]
        public static void BuildW2_5()
        {
            var level = GreyboxLevel.Create("W2-5", "L_W2_5_ArrecifeJefe");
            level.OpenWater();
            level.StartPath(Vector3.zero, 0f).Straight(180f, 12f);                  // S1 pasillos de coral con pinchos
            float phase1 = level.Length;
            level.Straight(150f, 16f);                                               // S2 fase 1: 2 tentáculos, cada 3 s
            float phase2 = level.Length;
            level.Arc(60f, 40f, 16f).Straight(120f, 16f);                           // S3 fase 2: 4 tentáculos, cada 2 s
            float phase3 = level.Length;
            level.Straight(160f, 12f);                                               // S4 fase 3: pasillo final, cada 1.5 s
            float exit = level.Length;
            level.Straight(45f, 14f);
            level.BuildChannel(0.4f);

            level.Section(1, "Coral", 0f);
            level.Section(2, "Fase1", phase1);
            level.Section(3, "Fase2", phase2);
            level.Section(4, "Fase3", phase3);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(phase1);
            level.AddCheckpoint(phase2);
            level.AddCheckpoint(phase3);

            // S1: pinchos de coral que marcan el pasillo, y una cueva de coral con un mango.
            for (float d = 12f; d < phase1 - 4f; d += 9f)
            {
                int i = Mathf.RoundToInt(d / 9f);
                float jitter = i % 3 == 0 ? 1.2f : 0f;
                level.Place(HZ + "H10_Spikes.prefab", d, 5.2f - jitter, i * 37f).name = "Pinchos";
                level.Place(HZ + "H10_Spikes.prefab", d + 4.5f, -5.2f + jitter, i * 53f).name = "Pinchos";
            }
            float cave = 95f;
            var yaw = Quaternion.LookRotation(level.DirectionAt(cave)).eulerAngles.y;
            level.Block("Cueva_Techo", level.PointAt(cave, -9f, 2.6f), new Vector3(6f, 0.8f, 7f), CaveColor, yaw);
            level.Block("Cueva_Fondo", level.PointAt(cave, -12.2f), new Vector3(0.8f, 3.5f, 7f), CaveColor, yaw);
            level.Block("Cueva_Lado", level.PointAt(cave + 3.6f, -9f), new Vector3(6f, 3.5f, 0.8f), CaveColor, yaw);
            level.Mango(0, cave, -9.5f, 1f);
            level.PowerUp("P01", 40f, 0f);
            level.PowerUp("P04", 150f, 0f);

            // Jefe: tres grupos de tentáculos, uno por fase; las zonas los encienden y apagan al pasar.
            var boss1 = Tentacles(level, "Tentaculos_F1", phase1, new[] { (40f, 4f), (95f, -4f) });
            var boss2 = Tentacles(level, "Tentaculos_F2", phase2, new[] { (20f, 4f), (50f, -4f), (80f, 4f), (110f, -4f) });
            var boss3 = Tentacles(level, "Tentaculos_F3", phase3, new[] { (30f, 3.5f), (60f, -3.5f), (90f, 3.5f), (120f, -3.5f) });
            Phase(level, "Fase1", phase1 + 2f, boss1, 0);
            Phase(level, "Fase1_Fin", phase2 + 2f, boss1, -1);
            Phase(level, "Fase2", phase2 + 4f, boss2, 1);
            Phase(level, "Fase2_Fin", phase3 + 2f, boss2, -1);
            Phase(level, "Fase3", phase3 + 4f, boss3, 2);
            Phase(level, "Fase3_Fin", exit + 5f, boss3, -1);
            level.PowerUp("P04", phase1 + 70f, 0f);
            level.Mango(1, phase2 + 65f, 0f, 1f);                                     // entre los tentáculos de la fase 2
            level.PowerUp("P01", phase2 + 100f, 0f);
            level.PowerUp("P04", phase3 + 10f, 0f);
            level.PowerUp("P01", phase3 + 140f, 0f);

            // Salida: coral rompible (Rugido o golpe fuerte) con el mango detrás.
            var coral = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H07_Crate.prefab"), level.Scene);
            coral.name = "CoralRompible";
            coral.transform.position = level.PointAt(exit + 15f, 5f);
            coral.transform.localScale = new Vector3(1.6f, 1.4f, 1.6f);
            level.Mango(2, exit + 15f, 7.5f, 1f);

            var reef = LevelDresser.With(LevelDresser.W2, new[] { "W2_Coral_A", "W2_Coral_B", "W2_Coral_C", "W2_Starfish", "W2_Kelp", "W2_Rock_B" },
                1.15f, LevelDresser.W2.Fog);
            LevelDresser.Dress(level, reef, 25, level.PointAt(phase3 + 80f, -70f), 90f);
            W2StarShots.Apply(level.Scene, "W2-5");
            level.Finish();
            Debug.Log($"M5-13 W2-5 listo: {level.Length:F0} m");
        }

        private static GasSafari.Hazards.TentacleBoss Tentacles(GreyboxLevel level, string name, float from, (float d, float lateral)[] spots)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H25_Tentacles.prefab"), level.Scene);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.position = level.PointAt(from);
            var boss = go.GetComponent<GasSafari.Hazards.TentacleBoss>();
            var all = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in go.transform)
                all.Add(child);
            var used = new Transform[spots.Length];
            for (int i = 0; i < all.Count; i++)
            {
                if (i >= spots.Length)
                {
                    Object.DestroyImmediate(all[i].gameObject);
                    continue;
                }
                all[i].position = level.PointAt(from + spots[i].d, spots[i].lateral, -1.5f);
                used[i] = all[i];
            }
            boss.Configure(used);
            return boss;
        }

        private static void Phase(GreyboxLevel level, string name, float d, GasSafari.Hazards.TentacleBoss boss, int phase)
        {
            var go = new GameObject("ZonaJefe_" + name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.layer = GameLayers.Trigger;
            go.transform.SetPositionAndRotation(level.PointAt(d, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(d)));
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(level.WidthAt(d) + 24f, 4f, 2f);
            go.AddComponent<GasSafari.Level.BossPhaseZone>().Configure(boss, phase);
        }
    }
}
