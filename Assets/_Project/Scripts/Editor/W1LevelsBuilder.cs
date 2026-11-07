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
            var level = GreyboxLevel.Create("W1-1", "L_W1_1_FuentesZoo");

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
            var level = GreyboxLevel.Create("W1-2", "L_W1_2_Alcantarillas");
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
