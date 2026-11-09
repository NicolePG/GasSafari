using GasSafari.Bots;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Greybox de los niveles del Mundo 1 (docs/04). Cada nivel es una función de este script: se rehace entero al repetirlo.
    // Distancias en metros a lo largo del cauce desde la salida.
    public static class W1LevelsBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private static readonly Color StatueColor = new Color(0.85f, 0.85f, 0.9f);
        private static readonly Color BushColor = new Color(0.2f, 0.55f, 0.25f);
        private static readonly Color FenceColor = new Color(0.4f, 0.4f, 0.5f);

        // ---------------------------------------------------------------- W1-1 Fuentes del Zoo
        [MenuItem("Gas Safari/Setup/Greybox W1-1 Fuentes del Zoo (M5-03)")]
        public static void BuildW1_1()
        {
            var level = GreyboxLevel.Create("W1-1", "N01_W1-1_FuentesZoo");

            // Trayecto: canal ancho, rotonda de 270° a la izquierda con salida a la derecha (así el cauce no se cruza), zigzag,
            // aproximación a la fuente central y canal de salida.
            level.StartPath(Vector3.zero, 0f).Straight(300f, 12f);              // S1
            float rotonda = level.Length;
            level.Straight(30f, 10f).Arc(22f, -270f, 10f);                      // S2: giro cerrado alrededor de la estatua
            float rotondaEnd = level.Length;
            level.Arc(11f, 90f, 10f);                                           // salida hacia el sur, junto a S1
            float zigzag = level.Length;
            level.Straight(200f, 12f);                                         // S3: zigzag entre fuentes
            float fuente = level.Length;
            level.Straight(90f, 8f);                                           // S4: aproximación a la fuente central
            level.Straight(120f, 12f);                                         // canal de salida
            level.BuildChannel(0f); // docs/04 no pide corriente en W1-1

            level.Section(1, "CanalBancas", 0f);
            level.Section(2, "Rotonda", rotonda);
            level.Section(3, "Zigzag", zigzag);
            level.Section(4, "FuenteCentral", fuente);

            // S1: canal ancho entre bancas (H01): bancas de 4 m que sobresalen de cada orilla, alternadas.
            float[] benches = { 45f, 90f, 135f, 180f, 225f, 270f };
            for (int i = 0; i < benches.Length; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                var bench = level.Place(HZ + "H01_Wall.prefab", benches[i], side * 5.1f, 0f, 0f, "Obstacles");
                bench.name = "H01_Bench_" + i;
                bench.transform.localScale = new Vector3(0.4f, 0.5f, 1f);
            }
            level.PowerUp("P01", 120f, 0f);

            // S2: rotonda. El centro del arco queda a 22 m a la izquierda del punto donde empieza.
            var center = level.PointAt(rotonda + 30f, -22f);
            level.Cylinder("Estatua", center, 14f, 6f, StatueColor);
            level.Mango(0, rotonda + 30f + 52f, -3.5f, 1f); // ruta: pegado a la estatua, a mitad de la rotonda

            level.AddCheckpoint(0f);
            level.AddCheckpoint(rotonda);

            // S3: zigzag entre fuentes que empujan hacia el centro; un chorro que eleva con el mango de riesgo.
            for (int i = 0; i < 8; i++)
            {
                float d = zigzag + 25f + i * 22f;
                float side = i % 2 == 0 ? 1f : -1f;       // +1 = derecha
                var jet = level.Place(HZ + "Jet_Lateral.prefab", d, side * 4.6f, side > 0f ? 180f : 0f, 0f, "Obstacles");
                jet.name = "Fountain_" + i;
            }
            level.PowerUp("P01", zigzag + 10f, 0f);
            var lift = level.Place(HZ + "H27_Jet.prefab", zigzag + 165f, 0f, 0f, 0f, "Obstacles");
            lift.name = "Jet_Lift";
            level.Mango(1, zigzag + 165f, 0f, 1.2f); // riesgo: sobre un chorro, hay que dejarse elevar

            level.AddCheckpoint(fuente);

            // S4: la fuente central lanza la balsa por encima de la reja hasta el canal de salida.
            level.PowerUp("P01", fuente + 10f, 0f);
            level.Hint(BotHintType.Ramp, fuente + 44f, 0f, 4f);
            var fountain = level.Place(HZ + "H27_Fountain.prefab", fuente + 50f, 0f, 0f, 0f, "Obstacles");
            fountain.name = "H27_FuenteCentral";
            // Reja a 8 m de la fuente: queda dentro del arco de vuelo llegue la balsa lenta o rápida (a 12 m la balsa caía encima).
            float fence = fuente + 58f;
            // Resaca entre la fuente y la reja: si el salto falla, devuelve la balsa al chorro (que rebota de nuevo).
            var undertow = level.Place(HZ + "Jet_Lateral.prefab", fence - 1.5f, 0f, 90f, 0f, "Obstacles"); // yaw 90: empuja hacia atrás
            undertow.name = "Fountain_Undertow";
            level.Block("Reja", level.PointAt(fence), new Vector3(level.WidthAt(fence) + 3f, 3f, 2f), FenceColor, Quaternion.LookRotation(level.DirectionAt(fence)).eulerAngles.y);

            // Mango escondido tras un arbusto en el canal de salida.
            level.Block("Arbusto", level.PointAt(fence + 28f, 4.6f), new Vector3(3f, 2.2f, 3f), BushColor);
            level.Mango(2, fence + 33f, 4.6f, 1f);

            // Momento estrella: la fuente central lanza la balsa sobre la reja (cámara lenta + toma de 2 s).
            var star = new GameObject("StarMoment");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(star, level.Scene);
            var shot = ShotBuilder.Create(star.transform, "FuenteCentral_Toma", "Assets/_Project/Data/Levels/W1-1_StarShot.playable", 2f,
                new Vector3(-9f, 4f, 6f), out var vcam);
            star.AddComponent<GasSafari.Level.StarMoment>().Configure(fountain.GetComponent<GasSafari.Hazards.Trampoline>(), shot, vcam);

            // Arte (Dirección visual de W1): la noria, hito del mundo, enmarca la fuente central y se ve desde la salida.
            LevelDresser.Dress(level, LevelDresser.W1, 11, new Vector3(-42f, 0f, 46f), 90f);
            DressW1_1(level);

            level.Finish();
            Debug.Log($"M5-03 W1-1 listo (greybox + arte): {level.Length:F0} m");
        }

        // ---------------------------------------------------------------- W1-2 Alcantarillas
        // El nivel empieza a 14 m y la espiral final baja hasta el agua base (y = 0): el agua de cada tramo es una cinta
        // que sigue el cauce (WaterSurface). Sin corriente salvo en la espiral (provisional: 3 m/s², "a toda velocidad").
        private const float W1_2_HEIGHT = 14f;
        private static readonly Color NicheColor = new Color(0.15f, 0.17f, 0.22f);
        private static readonly Color FallColor = new Color(0.55f, 0.75f, 0.9f);

        [MenuItem("Gas Safari/Setup/Greybox W1-2 Alcantarillas (M5-04)")]
        public static void BuildW1_2()
        {
            var level = GreyboxLevel.Create("W1-2", "N02_W1-2_Alcantarillas");
            level.StartPath(new Vector3(0f, W1_2_HEIGHT, 0f), 0f).Straight(60f, 8f);   // S1 túnel
            float gates = level.Length;
            level.Straight(90f, 8f);                                                     // S2 compuertas
            float fork = level.Length;
            level.Arc(20f, -45f, 8f).Straight(30f, 8f).Arc(20f, 90f, 8f).Straight(30f, 8f).Arc(20f, -45f, 8f); // S3 izquierda segura
            float join = level.Length;
            level.Straight(20f, 9f);
            float spiral = level.Length;
            level.Arc(14f, 540f, 9f, -12f);                                              // S4 espiral: 1.5 vueltas, 8 m por vuelta
            float spiralEnd = level.Length;
            level.Straight(40f, 10f, -2f).Straight(30f, 10f);                            // salida al agua base
            level.Branch(fork, join, 8f);                                                // ruta derecha: recta y más corta
            level.FlowZone(spiral, spiralEnd, 3f);                                       // "a toda velocidad" (provisional)
            level.BuildChannel(0f); // docs/04 no pide corriente en W1-2 (la variable llega en W1-3)

            level.Section(1, "Tunel", 0f);
            level.Section(2, "Compuertas", gates);
            level.Section(3, "Bifurcacion", fork);
            level.Section(4, "Espiral", spiral);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(gates);
            level.AddCheckpoint(join + 2f);
            level.AddCheckpoint(spiral);

            // S1: nicho oscuro tras una cascada de desagüe (mango escondido), en la pared izquierda.
            float niche = 40f;
            level.WallGap(niche - 3f, niche + 3f);
            var dir = level.DirectionAt(niche);
            float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
            level.Block("Nicho_Fondo", level.PointAt(niche, -10.5f), new Vector3(1f, 3f, 7f), NicheColor, yaw);
            level.Block("Nicho_Lado1", level.PointAt(niche - 3.5f, -7.5f), new Vector3(6f, 3f, 1f), NicheColor, yaw);
            level.Block("Nicho_Lado2", level.PointAt(niche + 3.5f, -7.5f), new Vector3(6f, 3f, 1f), NicheColor, yaw);
            var nicheWater = level.Block("Nicho_Agua", level.PointAt(niche, -7.5f) + Vector3.up * 0.3f, new Vector3(6f, 0.2f, 6f), Color.white, yaw, false);
            nicheWater.layer = GameSafariWater;
            nicheWater.GetComponent<BoxCollider>().isTrigger = true;
            var fall = level.Block("Cascada", level.PointAt(niche, -4.6f), new Vector3(0.3f, 3.5f, 6f), FallColor, yaw);
            Object.DestroyImmediate(fall.GetComponent<Collider>());
            level.Mango(2, niche, -7.5f, 1f);

            // S2: 3 compuertas en secuencia (ciclos 3, 2.5 y 2 s); mango entre la 2.ª y la 3.ª.
            float[] cycles = { 3f, 2.5f, 2f };
            for (int i = 0; i < 3; i++)
                Gate(level, gates + 20f + i * 25f, cycles[i], "Compuerta_" + (i + 1));
            level.Mango(0, gates + 57.5f, 0f, 1f);
            level.PowerUp("P01", 30f, 0f);
            level.PowerUp("P01", gates + 8f, 1.5f);

            // S3: ruta derecha (ramal recto) con compuerta rápida (2 s, el mínimo de H03), P03 y mango.
            var a = level.PointAt(fork);
            var b = level.PointAt(join);
            var chordDir = (b - a).normalized;
            var mid = (a + b) * 0.5f;
            var fast = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H03_TimedGate.prefab"), level.Scene);
            fast.name = "Compuerta_Rapida";
            fast.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(chordDir));
            SetGate(fast, 2f, 0.7f);
            PlacePowerUpAt(level, "P03", a + chordDir * 15f);
            MangoAt(level, 1, b - chordDir * 15f);
            level.PowerUp("P01", join + 10f, 0f);

            // S4: luces de neón en las paredes de la espiral (momento estrella).
            Neon(level, spiral, spiralEnd);

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W1, new[] { "W1_Barrel_A", "W1_Barrel_B", "W1_Barrel_C", "W1_Crate_A", "W1_Crate_B", "W1_Crate_C", "W1_Sign_A", "W1_Sign_B" },
                0.3f, new Color(0.07f, 0.1f, 0.16f)), 12, new Vector3(-60f, 0f, 150f), 90f);
            level.Finish();
            Debug.Log($"M5-04 W1-2 listo: {level.Length:F0} m, ramal derecho {Vector3.Distance(a, b):F0} m");
        }

        private static readonly int GameSafariWater = LayerMask.NameToLayer("Water");

        private static void Gate(GreyboxLevel level, float distance, float cycle, string name)
        {
            var gate = level.Place(HZ + "H03_TimedGate.prefab", distance);
            gate.name = name;
            SetGate(gate, cycle, distance * 0.013f);
        }

        private static void SetGate(GameObject gate, float cycle, float phase)
        {
            var so = new SerializedObject(gate.GetComponent<GasSafari.Hazards.TimedGate>());
            so.FindProperty("cycleOverride").floatValue = cycle;
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlacePowerUpAt(GreyboxLevel level, string id, Vector3 position)
        {
            var p = level.PowerUp(id, 0f);
            p.transform.position = position + Vector3.up;
        }

        private static void MangoAt(GreyboxLevel level, int slot, Vector3 position)
        {
            var m = level.Mango(slot, 0f, 0f, 1f);
            m.transform.position = position + Vector3.up;
        }

        private static void Neon(GreyboxLevel level, float from, float to)
        {
            var pink = AssetDatabase.LoadAssetAtPath<Material>(ENV + "Materials/E_FF4FA3.mat");
            var cyan = AssetDatabase.LoadAssetAtPath<Material>(ENV + "Materials/E_3AE1FF.mat");
            int n = 0;
            for (float d = from; d < to; d += 3f, n++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(strip.GetComponent<Collider>());
                    strip.name = "Neon";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(strip, level.Scene);
                    strip.transform.SetPositionAndRotation(level.PointAt(d, side * (level.WidthAt(d) * 0.5f + 0.2f), 1.6f),
                        Quaternion.LookRotation(level.DirectionAt(d)));
                    strip.transform.localScale = new Vector3(0.15f, 0.15f, 2f);
                    strip.GetComponent<MeshRenderer>().sharedMaterial = (n + (side > 0 ? 1 : 0)) % 2 == 0 ? pink : cyan;
                }
            }
        }

        // ---------------------------------------------------------------- W1-3 Río de la Ciudad
        // Río ancho con corriente variable (base 0.5 m/s², fuerte junto al muelle), oleaje H04, lanchas y ferry H28.
        private static readonly Color BuoyColor = new Color(1f, 0.5f, 0.15f);
        private static readonly Color PilingColor = new Color(0.35f, 0.27f, 0.2f);
        private static readonly Color PierColor = new Color(0.45f, 0.45f, 0.52f);

        [MenuItem("Gas Safari/Setup/Greybox W1-3 Rio de la Ciudad (M5-05)")]
        public static void BuildW1_3()
        {
            var level = GreyboxLevel.Create("W1-3", "N03_W1-3_RioCiudad");
            level.StartPath(Vector3.zero, 0f).Straight(140f, 16f);      // S1 slalom
            float boats = level.Length;
            level.Straight(160f, 16f);                                 // S2 lanchas
            float dock = level.Length;
            level.Arc(40f, 60f, 14f).Straight(130f, 14f);               // S3 muelle (curva a la derecha: la orilla exterior es la izquierda)
            float bridges = level.Length;
            level.Straight(200f, 14f).Straight(60f, 14f);              // S4 puentes y ferry, salida
            level.FlowZone(dock, bridges, 2f);                         // corriente fuerte junto al muelle (provisional)
            level.BuildChannel(0.5f);                                  // "corriente variable" (provisional)

            level.Section(1, "Slalom", 0f);
            level.Section(2, "Lanchas", boats);
            level.Section(3, "Muelle", dock);
            level.Section(4, "Puentes", bridges);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(boats);
            level.AddCheckpoint(dock);
            level.AddCheckpoint(bridges);

            // S1: slalom de boyas alternadas y una ola H04 que se repite al final del tramo.
            for (int i = 0; i < 10; i++)
            {
                var buoy = level.Cylinder("Boya_" + i, level.PointAt(18f + i * 12f, i % 2 == 0 ? 3.5f : -3.5f), 0.8f, 1.6f, BuoyColor);
                buoy.layer = GasSafari.Core.GameLayers.Hazard;
            }
            level.Mango(0, 78f, 0f, 1f);                               // centro del slalom
            level.PowerUp("P01", 40f, -5f);
            var wave = level.Place(HZ + "H04_Wave.prefab", boats - 12f, 0f, 180f, 0f);
            wave.name = "Oleaje";
            SetWave(wave, 16f, 6f);

            // S2: dos lanchas que cruzan y dejan una estela que baja por el río; mango sobre la primera estela.
            Boat(level, "H28_Speedboat.prefab", boats + 25f, 0f, "Lancha_1");
            Boat(level, "H28_Speedboat.prefab", boats + 70f, 2.5f, "Lancha_2");
            level.Mango(1, boats + 33f, 0f, 1.7f);                     // solo se alcanza subido a la estela
            Boat(level, "H28_Speedboat.prefab", boats + 120f, 1.2f, "Lancha_3");
            level.PowerUp("P01", boats + 50f, 4f);

            // S3: muelle con pilotes en la orilla exterior (izquierda) y chorros de corriente que empujan hacia ellos.
            for (float d = dock + 6f; d < bridges - 4f; d += 6f)
            {
                var pile = level.Cylinder("Pilote", level.PointAt(d, -5.6f), 0.35f, 2.6f, PilingColor);
                pile.layer = GasSafari.Core.GameLayers.Hazard;
            }
            level.Block("Muelle", level.PointAt((dock + bridges) * 0.5f, -9.5f), new Vector3(5f, 1.6f, 4f), PierColor,
                Quaternion.LookRotation(level.DirectionAt((dock + bridges) * 0.5f)).eulerAngles.y);
            for (float d = dock + 20f; d < bridges - 10f; d += 25f)
            {
                var jet = level.Place(HZ + "Jet_Lateral.prefab", d, 3.5f, 180f);
                jet.name = "Corriente_Muelle";
                foreach (var r in jet.GetComponentsInChildren<Renderer>())
                    r.enabled = false;                               // es corriente, no una fuente
            }
            level.PowerUp("P03", dock + 60f, 2f);

            // S4: tres puentes (pilares en las orillas) y el cruce del ferry; mango escondido bajo el último puente.
            float[] spans = { bridges + 30f, bridges + 100f, bridges + 170f };
            for (int i = 0; i < spans.Length; i++)
                Bridge(level, spans[i], "Puente_" + (i + 1));
            Boat(level, "H28_Ferry.prefab", bridges + 65f, 0f, "Ferry");
            level.Mango(2, spans[2], 5.6f, 1f);                        // entre los pilares del último puente
            level.PowerUp("P01", bridges + 130f, 0f);

            LevelDresser.Dress(level, LevelDresser.W1, 13, level.PointAt(bridges + 65f, 45f), -90f);
            level.Finish();
            Debug.Log($"M5-05 W1-3 listo: {level.Length:F0} m");
        }

        private static void Boat(GreyboxLevel level, string prefab, float distance, float phase, string name)
        {
            float width = level.WidthAt(distance);
            var boat = level.Place(HZ + prefab, distance, -(width * 0.5f + 4f), 90f);
            boat.name = name;
            var so = new SerializedObject(boat.GetComponent<GasSafari.Hazards.CrossingBoat>());
            so.FindProperty("travel").vector3Value = new Vector3(0f, 0f, width + 8f);
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
            var wake = boat.GetComponentInChildren<GasSafari.Hazards.WaveHazard>();
            if (wake != null)
                SetWave(wake.gameObject, width, 0f);
        }

        private static void SetWave(GameObject go, float width, float interval)
        {
            var so = new SerializedObject(go.GetComponent<GasSafari.Hazards.WaveHazard>());
            so.FindProperty("width").floatValue = width;
            if (interval > 0f)
                so.FindProperty("interval").floatValue = interval;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Bridge(GreyboxLevel level, float distance, string name)
        {
            float width = level.WidthAt(distance);
            float yaw = Quaternion.LookRotation(level.DirectionAt(distance)).eulerAngles.y;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W1_Bridge.fbx"), level.Scene);
            model.name = name;
            model.transform.SetPositionAndRotation(level.PointAt(distance) + Vector3.down * 0.5f, Quaternion.Euler(0f, yaw + 90f, 0f));
            GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);
            for (int side = -1; side <= 1; side += 2)
            {
                var pier = level.Block(name + "_Pilar", level.PointAt(distance, side * (width * 0.5f - 0.6f)), new Vector3(1.2f, 4f, 1.2f), PierColor, yaw);
                pier.layer = GasSafari.Core.GameLayers.Environment;
            }
        }

        // ---------------------------------------------------------------- W1-4 Mercado Flotante
        private static readonly Color StallColor = new Color(0.75f, 0.35f, 0.3f);
        private static readonly Color CardboardColor = new Color(0.72f, 0.55f, 0.32f);

        [MenuItem("Gas Safari/Setup/Greybox W1-4 Mercado Flotante (M5-06)")]
        public static void BuildW1_4()
        {
            var level = GreyboxLevel.Create("W1-4", "N04_W1-4_MercadoFlotante");
            level.StartPath(Vector3.zero, 0f).Straight(220f, 10f);                       // S1 pasillo de puestos
            float fork = level.Length;
            level.Arc(18f, -40f, 10f).Straight(25f, 10f).Arc(18f, 80f, 10f).Straight(25f, 10f).Arc(18f, -40f, 10f); // S2 vuelta segura
            float join = level.Length;
            level.Straight(10f, 14f);
            float awnings = level.Length;
            level.Straight(150f, 14f);                                                   // S3 toldos
            float tips = level.Length;
            level.Straight(200f, 10f).Straight(40f, 10f);                                // S4 puestos que se vuelcan, salida
            level.Branch(fork, join, 8f);                                                // atajo derecho, tapado por cajas
            level.BuildChannel(0f);

            level.Section(1, "Pasillo", 0f);
            level.Section(2, "Cajas", fork);
            level.Section(3, "Toldos", awnings);
            level.Section(4, "Vuelcos", tips);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(fork - 30f);
            level.AddCheckpoint(awnings);
            level.AddCheckpoint(tips);

            // S1: puestos en las dos orillas y comida flotando (decorado).
            var market = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W1_MarketStall.fbx");
            var basket = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W1_FruitBasket.fbx");
            var random = new System.Random(14);
            for (float d = 10f; d < fork - 10f; d += 12f)
            {
                for (int side = -1; side <= 1; side += 2)
                    Prop(level, market, level.PointAt(d + side * 3f, side * 7.5f, -0.2f), side);
                Prop(level, basket, level.PointAt(d + 6f, (float)(random.NextDouble() * 6.0 - 3.0), -0.35f), 1, true);
            }
            level.PowerUp("P01", 60f, 0f);
            level.PowerUp("P03", 140f, 2f);

            // S2: locoto antes de la bifurcación; cajas de cartón (Burnable) a mitad del atajo; mango detrás.
            level.PowerUp("P02", fork - 25f, -2f);
            level.PowerUp("P02", fork - 12f, 2f);
            var a = level.PointAt(fork);
            var b = level.PointAt(join);
            var chord = (b - a).normalized;
            var boxes = level.Block("CajasCarton", (a + b) * 0.5f, new Vector3(9f, 2.6f, 2f), CardboardColor, Quaternion.LookRotation(chord).eulerAngles.y);
            boxes.layer = GasSafari.Core.GameLayers.Hazard;
            boxes.AddComponent<GasSafari.Hazards.BurnableWall>().Configure(HazardData("W1_CardboardBoxes", "Cajas de cartón", 0.5f, ("burnTime", 0.5f)));
            boxes.AddComponent<GasSafari.Hazards.HazardTags>().Configure(GasSafari.Hazards.HazardTag.Burnable);
            MangoAt(level, 1, (a + b) * 0.5f + chord * 6f);

            // S3: cadena de 3 toldos trampolín, cada uno delante de un puesto que tapa el centro (laterales estrechos).
            GasSafari.Hazards.Trampoline first = null;
            for (int i = 0; i < 3; i++)
            {
                float d = awnings + 20f + i * 14f;
                var awning = level.Place(HZ + "H27_Awning.prefab", d);
                awning.name = "Toldo_" + (i + 1);
                var trampoline = awning.GetComponent<GasSafari.Hazards.Trampoline>();
                var so = new SerializedObject(trampoline);
                so.FindProperty("forwardSpeed").floatValue = 6f; // provisional: el salto cae en el toldo siguiente (14 m)
                so.ApplyModifiedPropertiesWithoutUndo();
                if (first == null)
                    first = trampoline;
                // Techo a dos aguas (caja girada 45°): si la balsa cae encima, resbala al agua en vez de quedarse varada.
                var stall = level.Block("Puesto_" + (i + 1), level.PointAt(d + 5f, 0f, -0.7f), new Vector3(3f, 3f, 3f), StallColor);
                stall.transform.rotation = Quaternion.LookRotation(level.DirectionAt(d)) * Quaternion.Euler(0f, 0f, 45f);
                // Entorno, no peligro: los bots no lo esquivan por los laterales y entran al toldo (que los salta por encima).
                stall.layer = GasSafari.Core.GameLayers.Environment;
            }
            level.Mango(0, awnings + 34f, 0f, 4.5f);                                     // sobre el 2.º toldo (rebote)
            level.Mango(2, awnings + 52f, 5.5f, 4f);                                     // puesto colgante: toldo doble o Julien
            level.PowerUp("P03", awnings + 90f, 0f);
            level.PowerUp("P01", awnings + 120f, -2f);

            // Momento estrella: la cadena de toldos (cámara lenta + toma al primer rebote).
            var star = new GameObject("StarMoment");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(star, level.Scene);
            var shot = ShotBuilder.Create(star.transform, "Toldos_Toma", "Assets/_Project/Data/Levels/W1-4_StarShot.playable", 3f,
                new Vector3(8f, 5f, -4f), out var vcam);
            star.AddComponent<GasSafari.Level.StarMoment>().Configure(first, shot, vcam);

            // S4: puestos que vuelcan su caja H07 al agua cuando pasa la balsa.
            level.PowerUp("P04", tips + 6f, 0f);
            var tipConfig = HazardData("W1_TipOverStall", "Puesto que se vuelca", 0.5f);
            for (int i = 0; i < 6; i++)
            {
                float d = tips + 25f + i * 28f;
                int side = i % 2 == 0 ? 1 : -1;
                TipOver(level, tipConfig, market, d, side);
            }

            LevelDresser.Dress(level, LevelDresser.W1, 15, level.PointAt(awnings + 40f, -45f), 90f);
            level.Finish();
            Debug.Log($"M5-06 W1-4 listo: {level.Length:F0} m, atajo {Vector3.Distance(a, b):F0} m");
        }

        // Puestos y cestas dentro del cauce: la balsa choca con ellos, así que llevan collider simple (docs/11, "nada se traspasa").
        // Los puestos son sólidos fijos; las cestas flotan livianas y se apartan al empujarlas.
        private static void Prop(GreyboxLevel level, GameObject model, Vector3 position, int side, bool floater = false)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, level.Scene);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f));
            if (floater)
            {
                PropColliders.MakeLightFloater(go, 0.6f);
                return;
            }
            PropColliders.AddSolidBox(go);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        private static void TipOver(GreyboxLevel level, GasSafari.Hazards.HazardConfig config, GameObject stallModel, float d, int side)
        {
            float width = level.WidthAt(d);
            var right = Vector3.Cross(Vector3.up, level.DirectionAt(d));
            Prop(level, stallModel, level.PointAt(d, side * (width * 0.5f + 2.5f), 0.3f), side);

            var crate = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H07_Crate.prefab"), level.Scene);
            crate.name = "Caja_Puesto";
            crate.transform.position = level.PointAt(d, side * (width * 0.5f + 0.8f), 2.6f);
            var body = crate.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.mass = 8f;
            crate.AddComponent<GasSafari.Hazards.FloatingBody>();

            var zone = new GameObject("Vuelco");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(zone, level.Scene);
            zone.layer = GasSafari.Core.GameLayers.Hazard;
            zone.transform.SetPositionAndRotation(level.PointAt(d - 9f, 0f, 1f), Quaternion.LookRotation(-right * side));
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, 3f, 3f);
            var tip = zone.AddComponent<GasSafari.Hazards.TipOverObstacle>();
            tip.Configure(config);
            tip.Configure(body, 3.5f);
        }

        private static GasSafari.Hazards.HazardConfig HazardData(string file, string displayName, float warning, params (string key, float value)[] parameters)
        {
            string path = "Assets/_Project/Data/Hazards/" + file + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<GasSafari.Hazards.HazardConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GasSafari.Hazards.HazardConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            config.id = file;
            config.displayName = displayName;
            config.warningTime = warning;
            config.parameters = System.Array.ConvertAll(parameters, x => new GasSafari.Hazards.HazardConfig.Parameter { key = x.key, value = x.value });
            EditorUtility.SetDirty(config);
            return config;
        }

        // ---------------------------------------------------------------- W1-5 Persecución en el Puerto (jefe)
        private static readonly Color ContainerColor = new Color(0.8f, 0.3f, 0.2f);

        [MenuItem("Gas Safari/Setup/Greybox W1-5 Persecucion en el Puerto (M5-07)")]
        public static void BuildW1_5()
        {
            var level = GreyboxLevel.Create("W1-5", "N05_W1-5_PuertoJefe");
            level.StartPath(Vector3.zero, 0f).Straight(200f, 12f);          // S1 la grúa aparece: 3 ganchos
            float slide = level.Length;
            level.Straight(180f, 12f);                                      // S2 contenedores que se deslizan
            float sink = level.Length;
            level.Arc(30f, -50f, 12f).Straight(90f, 12f);                  // S3 plataforma que se hunde
            float sprint = level.Length;
            level.Straight(200f, 12f);                                      // S4 sprint final (gancho cada 2 s)
            float ramp = level.Length;
            level.Straight(16f, 10f, 3f).Straight(12f, 10f);                // rampa del barco de carga y cubierta
            level.BuildChannel(0f);

            level.Section(1, "Grua", 0f);
            level.Section(2, "Contenedores", slide);
            level.Section(3, "Plataforma", sink);
            level.Section(4, "Sprint", sprint);
            level.AddCheckpoint(0f);
            level.AddCheckpoint(slide);
            level.AddCheckpoint(sink);
            level.AddCheckpoint(sprint);

            // La grúa (H24) espera en la orilla y sigue a la balsa por todo el puerto.
            var crane = level.Place(HZ + "H24_Crane.prefab", 25f, -14f, 90f).GetComponent<GasSafari.Hazards.CraneBoss>();
            Zone(level, "Grua_3Golpes", 20f, crane, 3, 0f, false);
            Zone(level, "Grua_Para", slide - 5f, crane, 0, 0f, true);
            Zone(level, "Grua_Sprint", sprint + 5f, crane, 0, 2f, false);       // docs/04: cada 2 s
            Zone(level, "Grua_Fin", ramp + 20f, crane, 0, 0f, true);
            level.PowerUp("P04", 8f, 0f);
            level.PowerUp("P01", 100f, 3f);

            // S2: contenedores que cruzan el cauce (H05) y uno abierto con el mango.
            for (int i = 0; i < 4; i++)
            {
                float d = slide + 30f + i * 35f;
                var block = level.Place(HZ + "H05_MovingBlocker.prefab", d, -(level.WidthAt(d) * 0.5f + 1f), 90f);
                block.name = "Contenedor_" + (i + 1);
                var so = new SerializedObject(block.GetComponent<GasSafari.Hazards.MovingBlocker>());
                so.FindProperty("travel").vector3Value = new Vector3(0f, 0f, level.WidthAt(d) - 2f);
                so.FindProperty("phase").floatValue = i * 1.3f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Container(level, "ContenedorAbierto", slide + 150f, 4.5f, false);
            level.Mango(0, slide + 150f, 4.5f, 1f);
            level.PowerUp("P01", slide + 90f, 0f);

            // S3: plataforma H35 que tapa el cauce (sumergida apenas): empieza a hundirse al tocarla; mango en su borde.
            float platform = sink + 60f;
            var deck = level.Place(HZ + "H35_SinkingPlatform.prefab", platform, 0f, 0f, -0.5f);
            deck.name = "Plataforma";
            deck.transform.localScale = new Vector3(level.WidthAt(platform) / 8f, 1f, 2f);
            var touch = new GameObject("Toque");
            touch.transform.SetParent(deck.transform, false);
            touch.layer = GasSafari.Core.GameLayers.Hazard;
            var touchBox = touch.AddComponent<BoxCollider>();
            touchBox.isTrigger = true;
            touchBox.center = new Vector3(0f, 1.2f, 0f);
            touchBox.size = new Vector3(8f, 2f, 8f);
            level.Mango(1, platform + 6f, 4f, 0.8f);                        // riesgo: quedarse sobre la plataforma

            // S4: sprint; contenedor junto a la rampa que el gancho destroza detrás de la balsa (momento estrella).
            level.PowerUp("P04", sprint + 4f, 0f);
            level.PowerUp("P01", sprint + 110f, -3f);
            var target = Container(level, "ContenedorFinal", ramp - 6f, -(level.WidthAt(ramp) * 0.5f - 1.5f), true);
            var finale = new GameObject("StarMoment");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(finale, level.Scene);
            finale.layer = GasSafari.Core.GameLayers.Trigger;
            finale.transform.SetPositionAndRotation(level.PointAt(ramp + 2f, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(ramp)));
            var box = finale.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(12f, 4f, 3f);
            var shot = ShotBuilder.Create(finale.transform, "Rampa_Toma", "Assets/_Project/Data/Levels/W1-5_StarShot.playable", 2.5f,
                new Vector3(-10f, 6f, -8f), out var vcam);
            finale.AddComponent<GasSafari.Level.CraneFinale>().Configure(crane, target.transform, shot, vcam);
            level.Mango(2, ramp + 16f, 0f, 2.6f);                           // punta de la rampa: hace falta sync para llegar alto
            var ship = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W1_CargoShip.fbx");
            if (ship != null)
            {
                var hull = (GameObject)PrefabUtility.InstantiatePrefab(ship, level.Scene);
                hull.transform.SetPositionAndRotation(level.PointAt(ramp + 22f, 0f, -0.5f), Quaternion.LookRotation(level.DirectionAt(ramp)));
                GameObjectUtility.SetStaticEditorFlags(hull, StaticEditorFlags.BatchingStatic);
            }

            LevelDresser.Dress(level, LevelDresser.With(LevelDresser.W1, new[] { "W1_Container_A", "W1_Container_B", "W1_Container_C", "W1_Barrel_A",
                "W1_Barrel_B", "W1_Crate_A", "W1_Crate_B", "W1_Crate_C", "W1_Dock" }, 0.45f, LevelDresser.W1.Fog), 16, level.PointAt(ramp, -50f), 90f);
            level.Finish();
            Debug.Log($"M5-07 W1-5 listo: {level.Length:F0} m");
        }

        private static void Zone(GreyboxLevel level, string name, float d, GasSafari.Hazards.CraneBoss crane, int strikes, float interval, bool stop)
        {
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, level.Scene);
            go.layer = GasSafari.Core.GameLayers.Trigger;
            go.transform.SetPositionAndRotation(level.PointAt(d, 0f, 1f), Quaternion.LookRotation(level.DirectionAt(d)));
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(level.WidthAt(d) + 2f, 4f, 2f);
            go.AddComponent<GasSafari.Level.CraneZone>().Configure(crane, strikes, interval, stop);
        }

        // Contenedor del puerto (modelo W1_Container); 'breakable' = se rompe con el gancho (H07, IBreakable).
        private static GameObject Container(GreyboxLevel level, string name, float d, float lateral, bool breakable)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W1_Container_A.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, level.Scene);
            go.name = name;
            go.layer = GasSafari.Core.GameLayers.Hazard;
            go.transform.SetPositionAndRotation(level.PointAt(d, lateral, -0.4f), Quaternion.LookRotation(level.DirectionAt(d)));
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
            if (breakable)
                go.AddComponent<GasSafari.Hazards.BreakableCrate>().Configure(AssetDatabase.LoadAssetAtPath<GasSafari.Hazards.HazardConfig>("Assets/_Project/Data/Hazards/H07_Crate.asset"));
            return go;
        }

        // Modelos sobre las piezas del greybox de W1-1 (el collider del greybox se queda; su caja deja de verse).
        private static void DressW1_1(GreyboxLevel level)
        {
            Replace(level, "Estatua", "W1_Statue", 1f);
            Replace(level, "Arbusto", "W1_Topiary_A", 1.4f);
            var fence = Find(level, "Reja");
            if (fence != null)
            {
                fence.GetComponent<MeshRenderer>().enabled = false;
                float length = fence.transform.localScale.x;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ENV + "W1_Fence.fbx");
                int pieces = Mathf.CeilToInt(length / 3f);
                for (int i = 0; i < pieces; i++)
                {
                    var piece = (GameObject)PrefabUtility.InstantiatePrefab(model, fence.transform.parent);
                    piece.transform.rotation = fence.transform.rotation;
                    piece.transform.position = fence.transform.position + fence.transform.right * (-length * 0.5f + 1.5f + i * 3f)
                                               + Vector3.down * (fence.transform.localScale.y * 0.5f - 0.5f);
                    GameObjectUtility.SetStaticEditorFlags(piece, StaticEditorFlags.BatchingStatic);
                }
            }
        }

        private const string ENV = "Assets/_Project/Art/Environment/W1/";

        private static GameObject Find(GreyboxLevel level, string name)
        {
            foreach (var root in level.Scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t.gameObject;
            return null;
        }

        private static void Replace(GreyboxLevel level, string name, string model, float scale)
        {
            var block = Find(level, name);
            if (block == null)
                return;
            var renderer = block.GetComponent<MeshRenderer>();
            renderer.enabled = false;
            var bounds = renderer.bounds;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ENV + model + ".fbx"), block.transform.parent);
            go.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            go.transform.localScale = Vector3.one * scale;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }
    }
}
