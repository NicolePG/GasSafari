using System.Collections.Generic;
using GasSafari.Bots;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Modes;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Recetas de los tramos de Infinito: cada una repite en 40-55 m una sección de un nivel de M5 con sus mismos peligros,
    // laterales y ajustes (Source). Dificultad provisional (docs/06: 1-5): W1 1-2, W2-W3 2-3, W3-W4 4, W5-W6 5.
    public static partial class EndlessChunkBuilder
    {
        private const string HZ = "Assets/_Project/Prefabs/Hazards/";
        private const string PU = "Assets/_Project/Prefabs/PowerUps/";
        private const string W5_ROCKS = "Assets/_Project/Art/Environment/W5/";

        private static readonly Color[] BankColors =
        {
            new Color(0.62f, 0.62f, 0.6f),   // W1 ciudad: vereda
            new Color(0.93f, 0.84f, 0.6f),   // W2 mar: arena
            new Color(0.32f, 0.55f, 0.25f),  // W3 selva
            new Color(0.36f, 0.32f, 0.4f),   // W4 cueva
            new Color(0.75f, 0.45f, 0.28f),  // W5 cañón
            new Color(0.62f, 0.3f, 0.72f)    // W6 fiesta
        };

        private static IEnumerable<Recipe> Recipes()
        {
            // Salida: recta tranquila con frijoles (como la salida de T-0).
            yield return new Recipe { Id = "salida", Source = "T-0", World = 1, Difficulty = 0, Path = (p, s) => p.Straight(30f),
                Content = c => c.PowerUp("P01", 18f, 0f) };

            // ---- Mundo 1
            yield return new Recipe { Id = "w1_fuentes", Source = "W1-1", World = 1, Difficulty = 1, Path = (p, s) => p.Straight(40f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    for (int i = 0; i < 3; i++)
                        c.LateralJet(10f + i * 12f, i % 2 == 0 ? 4.6f : -4.6f);
                } };
            yield return new Recipe { Id = "w1_puestos", Source = "W1-4", World = 1, Difficulty = 1,
                Path = (p, s) => p.Straight(10f).Arc(30f, 30f * s).Straight(15f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Place(HZ + "H07_Crate.prefab", 14f, -3f, 20f);
                    c.Place(HZ + "H07_Crate.prefab", 24f, 3f, 65f);
                    c.PowerUp("P03", 34f, 0f);
                } };
            yield return new Recipe { Id = "w1_compuertas", Source = "W1-2", World = 1, Difficulty = 2, Path = (p, s) => p.Straight(44f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Gate(16f, 3f);
                    c.Gate(34f, 2.5f);
                } };

            // ---- Mundo 2
            yield return new Recipe { Id = "w2_cajas", Source = "W2-1", World = 2, Difficulty = 2, Path = (p, s) => p.Straight(50f),
                Content = c =>
                {
                    var random = new System.Random(21);
                    c.PowerUp("P01", 4f, 0f);
                    for (int i = 0; i < 5; i++)
                        c.Place(HZ + "H07_Crate.prefab", 12f + i * 8f, (float)(random.NextDouble() * 9.0 - 4.5), (float)(random.NextDouble() * 90.0));
                    c.PowerUp("P04", 30f, -3f);
                } };
            yield return new Recipe { Id = "w2_barriles", Source = "W2-1", World = 2, Difficulty = 3,
                Path = (p, s) => p.Straight(10f).Arc(40f, 30f * s).Straight(15f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    for (int i = 0; i < 3; i++)
                        c.Place(HZ + "H06_ExplosiveBarrel.prefab", 14f + i * 13f, i % 2 == 0 ? 4f : -4f);
                } };
            yield return new Recipe { Id = "w2_remolino", Source = "W2-4", World = 2, Difficulty = 3, Path = (p, s) => p.Straight(50f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Place(HZ + "H09_Whirlpool.prefab", 24f, -3.5f);
                    c.Hint(BotHintType.Whirlpool, 24f, -3.5f, 4f);
                    c.LateralJet(40f, 3f);
                } };

            // ---- Mundo 3
            yield return new Recipe { Id = "w3_bancos", Source = "W3-1", World = 3, Difficulty = 2, Path = (p, s) => p.Straight(45f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Place(HZ + "H11_SandBar.prefab", 15f, 3f);
                    c.Place(HZ + "H11_SandBar.prefab", 30f, -3f);
                    c.Place(HZ + "H29_Crab.prefab", 39f, 2f, 0f, 1.1f);
                } };
            yield return new Recipe { Id = "w3_troncos", Source = "W3-2", World = 3, Difficulty = 3, Path = (p, s) => p.Straight(50f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.SpinningLog(16f, 3.3f, 0f);
                    c.SpinningLog(36f, -3.3f, 45f);
                } };
            yield return new Recipe { Id = "w3_fosas", Source = "W3-3", World = 3, Difficulty = 3,
                Path = (p, s) => p.Straight(10f).Arc(35f, 30f * s).Straight(16f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    for (int i = 0; i < 3; i++)
                        c.Place(HZ + "H14_Pit.prefab", 12f + i * 13f, i % 2 == 0 ? 4.8f : -4.8f);
                } };
            yield return new Recipe { Id = "w3_cocodrilos", Source = "W3-4", World = 3, Difficulty = 4, Path = (p, s) => p.Straight(55f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.PowerUp("P06", 10f, 0f);
                    c.Place(HZ + "H15_Crocodile.prefab", 22f, 4.5f, 90f);
                    c.Place(HZ + "H15_Crocodile.prefab", 44f, -4.5f, 90f);
                } };

            // ---- Mundo 4
            yield return new Recipe { Id = "w4_cristales", Source = "W4-2", World = 4, Difficulty = 4,
                Path = (p, s) => p.Straight(10f).Arc(40f, 30f * s).Straight(15f),
                Content = c =>
                {
                    c.PowerUp("P04", 4f, 0f);
                    for (int i = 0; i < 4; i++)
                        c.Place(HZ + "H10_CrystalSpikes.prefab", 12f + i * 9f, i % 2 == 0 ? 4.6f : -4.6f, i * 41f);
                    c.PowerUp("P01", 44f, 0f);
                } };
            yield return new Recipe { Id = "w4_geiseres", Source = "W4-3", World = 4, Difficulty = 4, Path = (p, s) => p.Straight(50f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    for (int i = 0; i < 3; i++)
                        c.Geyser(15f + i * 14f, i % 2 == 0 ? 4f : -4f, i * 0.7f);
                } };
            yield return new Recipe { Id = "w4_murcielagos", Source = "W4-4", World = 4, Difficulty = 4, Path = (p, s) => p.Straight(50f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Place(HZ + "H26_Bats.prefab", 18f, 4.6f);
                    c.Place(HZ + "H26_Bats.prefab", 38f, -4.6f);
                    c.PowerUp("P01", 46f, 0f);
                } };

            // ---- Mundo 5
            yield return new Recipe { Id = "w5_rapidos", Source = "W5-1", World = 5, Difficulty = 5, Path = (p, s) => p.Straight(55f),
                Content = c =>
                {
                    c.PowerUp("P04", 4f, 0f);
                    c.Rapids(10f, 48f, 1f);
                    c.Rock("W5_CanyonRock_A", 22f, 4.4f, 15f, 0.85f);
                    c.Rock("W5_CanyonRock_B", 37f, -4.4f, 95f, 0.85f);
                    c.PowerUp("P01", 52f, 0f);
                } };
            yield return new Recipe { Id = "w5_curva", Source = "W5-5", World = 5, Difficulty = 5, Flow = 1.2f,
                Path = (p, s) => p.Straight(8f).Arc(30f, 45f * s).Straight(12f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Rock("W5_CanyonRock_A", 14f, -3.8f, 0f, 0.8f);
                    c.Rock("W5_CanyonRock_C", 30f, 3.8f, 53f, 0.8f);
                } };

            // ---- Mundo 6
            yield return new Recipe { Id = "w6_fosas", Source = "W6-1", World = 6, Difficulty = 5, Path = (p, s) => p.Straight(55f),
                Content = c =>
                {
                    c.PowerUp("P01", 4f, 0f);
                    c.Place(HZ + "H14_Pit.prefab", 12f, 4.8f);
                    c.LateralJet(23f, 5.2f);
                    c.PowerUp("P04", 28f, 0f);
                    c.Place(HZ + "H14_Pit.prefab", 34f, -4.8f);
                    c.LateralJet(45f, -5.2f);
                } };
            yield return new Recipe { Id = "w6_cocos", Source = "W6-4", World = 6, Difficulty = 5, Path = (p, s) => p.Straight(50f),
                Content = c =>
                {
                    c.PowerUp("P04", 4f, 0f);
                    c.CoconutRain(25f, 1.5f);
                    c.PowerUp("P01", 46f, 0f);
                } };
        }

        // ---------------------------------------------------------------- piezas de un tramo
        private sealed class Chunk
        {
            private readonly Recipe recipe;
            private readonly ChunkPath path;
            private readonly int side;
            private readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();

            public GameObject Root { get; }
            public EndlessChunk Component { get; }
            public List<float> Accel { get; } = new List<float>();

            public Chunk(Recipe chunkRecipe, ChunkPath chunkPath, int chunkSide)
            {
                recipe = chunkRecipe;
                path = chunkPath;
                side = chunkSide;
                Root = new GameObject("Chunk_" + recipe.Id);
                Component = Root.AddComponent<EndlessChunk>();
                for (int i = 0; i < path.Points.Count; i++)
                    Accel.Add(recipe.Flow);
            }

            private Transform Group(string name)
            {
                if (groups.TryGetValue(name, out var group))
                    return group;
                var go = new GameObject(name);
                go.transform.SetParent(Root.transform, false);
                groups[name] = go.transform;
                return go.transform;
            }

            // Paredes con rebote (H01) a los dos lados y una orilla del color del mundo detrás (solo se ve).
            public void BuildWalls()
            {
                var walls = Group("Walls");
                walls.gameObject.layer = GameLayers.Environment;
                var bounce = walls.gameObject.AddComponent<BounceWall>();
                var so = new SerializedObject(bounce);
                so.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<HazardConfig>(WALL_CONFIG);
                so.ApplyModifiedPropertiesWithoutUndo();
                var wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WALL_MATERIAL);
                var bankMaterial = BankMaterial(recipe.World);
                var banks = Group("Orillas");
                var points = path.Points;
                for (int s = -1; s <= 1; s += 2)
                {
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var dir = (points[i + 1] - points[i]).normalized;
                        var right = Vector3.Cross(Vector3.up, dir);
                        float wallOffset = WIDTH * 0.5f + WALL_THICKNESS * 0.5f;
                        float bankOffset = WIDTH * 0.5f + WALL_THICKNESS + BANK_WIDTH * 0.5f;
                        Segment(walls, "Wall", points[i] + right * s * wallOffset, points[i + 1] + right * s * wallOffset,
                            WALL_THICKNESS, WALL_HEIGHT, -0.5f, wallMaterial, true);
                        Segment(banks, "Orilla", points[i] + right * s * bankOffset, points[i + 1] + right * s * bankOffset,
                            BANK_WIDTH, 0.8f, -0.4f, bankMaterial, false);
                    }
                }
            }

            private static void Segment(Transform parent, string name, Vector3 a, Vector3 b, float thickness, float height, float bottom,
                Material material, bool solid)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = name;
                cube.layer = GameLayers.Environment;
                cube.transform.SetParent(parent, false);
                var delta = b - a;
                cube.transform.localPosition = (a + b) * 0.5f + Vector3.up * (bottom + height * 0.5f);
                cube.transform.localRotation = Quaternion.LookRotation(delta.normalized);
                cube.transform.localScale = new Vector3(thickness, height, delta.magnitude + 0.3f);
                cube.GetComponent<MeshRenderer>().sharedMaterial = material;
                if (!solid)
                    Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            private static Material BankMaterial(int world)
            {
                string matPath = MATS + "M_Endless_Bank_W" + world + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("GasSafari/SG_Toon"));
                    AssetDatabase.CreateAsset(material, matPath);
                }
                material.SetColor("_BaseColor", BankColors[Mathf.Clamp(world, 1, 6) - 1]);
                EditorUtility.SetDirty(material);
                return material;
            }

            // Laterales positivos = derecha en la variante normal; la espejada los invierte.
            public GameObject Place(string prefabPath, float distance, float lateral = 0f, float yaw = 0f, float height = 0f, string group = "Obstacles")
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                    throw new System.InvalidOperationException("Falta el prefab " + prefabPath);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Root.scene);
                instance.transform.SetParent(Group(group), false);
                instance.transform.localPosition = path.PointAt(distance, lateral * side, height);
                instance.transform.localRotation = Quaternion.LookRotation(path.DirectionAt(distance)) * Quaternion.Euler(0f, yaw, 0f);
                return instance;
            }

            public GameObject PowerUp(string id, float distance, float lateral, float height = 1f)
            {
                string[] found = System.IO.Directory.GetFiles(PU.TrimEnd('/'), id + "_*.prefab");
                return Place(found[0].Replace('\\', '/'), distance, lateral, 0f, height, "PowerUps");
            }

            // Chorro lateral (W1-1): empuja hacia el centro desde el lado donde está.
            public void LateralJet(float distance, float lateral)
            {
                bool right = lateral * side > 0f;
                var jet = Place(HZ + "Jet_Lateral.prefab", distance, lateral, 0f);
                jet.transform.localRotation = Quaternion.LookRotation(path.DirectionAt(distance)) * Quaternion.Euler(0f, right ? 180f : 0f, 0f);
            }

            // Compuerta H03 con su ciclo (W1-2: 3 / 2.5 / 2 s).
            public void Gate(float distance, float cycle)
            {
                var gate = Place(HZ + "H03_TimedGate.prefab", distance);
                var so = new SerializedObject(gate.GetComponent<TimedGate>());
                so.FindProperty("cycleOverride").floatValue = cycle;
                so.FindProperty("phase").floatValue = distance * 0.013f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Tronco giratorio de W3-2: 5 m de largo, eje a un costado.
            public void SpinningLog(float distance, float lateral, float yaw)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HZ + "H13_SpinningLog.prefab");
                var box = prefab.GetComponent<BoxCollider>();
                float scale = box != null ? 5f / Mathf.Max(0.5f, Mathf.Max(box.size.x, box.size.z)) : 1f;
                var log = Place(HZ + "H13_SpinningLog.prefab", distance, lateral, yaw);
                log.transform.localScale = Vector3.one * scale;
            }

            public void Geyser(float distance, float lateral, float phase)
            {
                var geyser = Place(HZ + "H19_Geyser.prefab", distance, lateral);
                geyser.GetComponent<Geyser>().SetPhase(phase);
                EditorUtility.SetDirty(geyser.GetComponent<Geyser>());
            }

            // Rápidos H21 (W5-1) entre dos distancias: zona de empuje y corriente del cauce más fuerte.
            public void Rapids(float from, float to, float intensity)
            {
                float mid = (from + to) * 0.5f;
                var zone = Place(HZ + "H21_Rapids.prefab", mid);
                zone.transform.localScale = new Vector3((WIDTH + 2f) / 14f, 1f, (to - from) / 10f);
                zone.GetComponent<RapidsZone>().Configure(intensity);
                EditorUtility.SetDirty(zone.GetComponent<RapidsZone>());
            }

            public void Rock(string fbx, float distance, float lateral, float yaw, float scale)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(W5_ROCKS + fbx + ".fbx");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, Root.scene);
                go.transform.SetParent(Group("Props"), false);
                go.transform.localPosition = path.PointAt(distance, lateral * side, -0.3f);
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localScale = Vector3.one * scale;
                PropColliders.AddSolidBox(go);
            }

            // Lluvia de cocos de W6-4 en el medio del tramo.
            public void CoconutRain(float distance, float interval)
            {
                var go = Place(HZ + "H30_Coconut.prefab", distance);
                var fall = go.GetComponent<CoconutFall>();
                fall.ConfigureRain(interval, go.transform.Find("Sombra"), 0.8f, 0f);
                EditorUtility.SetDirty(fall);
            }

            public void Hint(BotHintType type, float distance, float lateral, float radius)
            {
                var go = new GameObject("BotHint_" + type);
                go.transform.SetParent(Group("BotHints"), false);
                go.transform.localPosition = path.PointAt(distance, lateral * side);
                go.transform.localRotation = Quaternion.LookRotation(path.DirectionAt(distance));
                go.AddComponent<BotHint>().Configure(type, radius);
            }
        }
    }
}
