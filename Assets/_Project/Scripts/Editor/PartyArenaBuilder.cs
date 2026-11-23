using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Minijuegos de Fiesta (docs/06 §1, M7-04..06): datos en Data/Party, escenas BalsaGol, ReyBalsa y PescaMangos en
    // Scenes/Juego/Fiesta (docs/10 §3) y su lugar en Build Settings. Arenas greybox: piscina con borde de baldosa, agua
    // de la piscina del zoo (T-0, de día), cámara fija que encuadra toda la arena. Se puede repetir: rehace las escenas.
    public static partial class PartyArenaBuilder
    {
        public const string SCENES = "Assets/_Project/Scenes/Juego/Fiesta/";
        private const string DATA = "Assets/_Project/Data/Party/";
        private const string MATS = "Assets/_Project/Art/Materials/Party/";
        private const string BALL_TEXTURE = "Assets/_Project/Art/Textures/Party/T_BeachBall.png";
        private const string CONFIG_PATH = "Assets/_Project/Resources/ModesConfig.asset";
        private const string PARTY_MODE = "Assets/_Project/Data/Modes/Mode_party.asset";
        private const string BALANCE_PATH = "Assets/_Project/Data/Balance/BalanceConfig.asset";
        private const string RAFT_PREFAB = "Assets/_Project/Prefabs/Raft/Raft.prefab";
        private const string AVATAR_PREFAB = "Assets/_Project/Prefabs/Characters/Avatar_Placeholder.prefab";
        private const string MANGO_PREFAB = "Assets/_Project/Prefabs/PowerUps/P03_Mango.prefab";
        private const string MINI_PREFABS = "Assets/_Project/Prefabs/MiniRaft/MiniRaft_J";
        private const float DECK_TOP = 0.9f;
        private const float DECK_BOTTOM = -2.2f;
        private const float POOL_FLOOR = -1.8f;

        // Coco Caliente llega con M7-08.
        public static readonly string[] Implemented = { "goal", "king", "fishing", "bubble" };

        [MenuItem("Gas Safari/Setup/Crear minijuegos de Fiesta (M7)")]
        public static void BuildAll()
        {
            var games = BuildData();
            BuildGoal(games[0]);
            BuildKing(games[1]);
            BuildFishing(games[2]);
            BuildBubble(games[3]);
            AddToBuildSettings(games);
            AssetDatabase.SaveAssets();
            Debug.Log("M7 Fiesta: datos y escenas listos");
        }

        // Rehace solo una escena (las demás no cambian: rehacerlas cambia todos sus fileID en Git).
        [MenuItem("Gas Safari/Setup/Fiesta: rehacer solo Carrera de Burbujas")]
        public static void BuildBubbleOnly()
        {
            var games = BuildData();
            BuildBubble(games[3]);
            AddToBuildSettings(games);
            AssetDatabase.SaveAssets();
            Debug.Log("M7 Fiesta: Carrera de Burbujas lista");
        }

        // ---------------------------------------------------------------- datos
        public static PartyGameData[] BuildData()
        {
            var defs = new (string id, string scene, float duration, int target, int rounds, int maxRounds, float roundSeconds, bool even)[]
            {
                ("goal", "BalsaGol", 180f, 5, 0, 0, 0f, true),
                ("king", "ReyBalsa", 0f, 0, 2, 5, 60f, false),
                ("fishing", "PescaMangos", 90f, 0, 0, 0, 0f, false),
                ("bubble", "CarreraBurbujas", 0f, 0, 0, 0, 0f, false),
                ("coconut", "CocoCaliente", 0f, 0, 0, 0, 0f, false),
            };
            if (!AssetDatabase.IsValidFolder(DATA.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Party");
            var result = new PartyGameData[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var data = HudBuilder.LoadOrCreate<PartyGameData>(DATA + "Party_" + d.id + ".asset");
                data.id = d.id;
                data.sceneName = d.scene;
                data.order = i + 1;
                data.implemented = System.Array.IndexOf(Implemented, d.id) >= 0;
                data.minPlayers = 2;
                data.maxPlayers = 4;
                data.evenPlayers = d.even;
                data.durationSeconds = d.duration;
                data.targetScore = d.target;
                data.roundsToWin = d.rounds;
                data.maxRounds = d.maxRounds;
                data.roundSeconds = d.roundSeconds;
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            var config = HudBuilder.LoadOrCreate<ModesConfig>(CONFIG_PATH);
            config.partyGames = result;
            if (config.partyPoints == null || config.partyPoints.Length == 0)
                config.partyPoints = new[] { 3, 2, 1, 0 };
            EditorUtility.SetDirty(config);
            var mode = AssetDatabase.LoadAssetAtPath<GameModeData>(PARTY_MODE);
            if (mode != null)
            {
                mode.implemented = true;
                EditorUtility.SetDirty(mode);
            }
            return result;
        }

        // ---------------------------------------------------------------- Balsa-Gol
        private static void BuildGoal(PartyGameData data)
        {
            var scene = Begin();
            try
            {
                const float hx = 20f, hz = 12f, mouth = 5f, depth = 3.5f;
                var root = Common(data, scene, out var spawner, new Vector3(0f, 25f, -23f), new Vector3(0f, 0f, -1.5f));
                var game = root.AddComponent<BalsaGolGame>();
                Water(root.transform, new Vector2(hx + depth + 4f, hz + 4f));
                var deck = Mat("M_Party_Deck", new Color(0.95f, 0.86f, 0.66f));
                var arena = new GameObject("Arena").transform;
                arena.SetParent(root.transform, false);
                // Bandas largas y fondos con hueco para la portería; todo llega hasta el borde exterior (outer).
                float outer = hx + depth + 4f;
                Block(arena, "Wall_N", new Vector3(0f, 0f, hz + 2f), new Vector3(2f * outer, 0f, 4f), deck);
                Block(arena, "Wall_S", new Vector3(0f, 0f, -hz - 2f), new Vector3(2f * outer, 0f, 4f), deck);
                for (int side = -1; side <= 1; side += 2)
                {
                    float len = hz - mouth;
                    float endWidth = outer - hx;
                    Block(arena, "End_" + side + "_N", new Vector3(side * (hx + endWidth * 0.5f), 0f, mouth + len * 0.5f), new Vector3(endWidth, 0f, len), deck);
                    Block(arena, "End_" + side + "_S", new Vector3(side * (hx + endWidth * 0.5f), 0f, -mouth - len * 0.5f), new Vector3(endWidth, 0f, len), deck);
                    float backWidth = outer - hx - depth;
                    Block(arena, "GoalBack_" + side, new Vector3(side * (hx + depth + backWidth * 0.5f), 0f, 0f), new Vector3(backWidth, 0f, 2f * mouth), deck);
                    // Chaflanes a los lados de la boca: la pelota que roza el poste resbala hacia adentro.
                    for (int z = -1; z <= 1; z += 2)
                        Block(arena, "Bevel_" + side + "_" + z, new Vector3(side * hx, 0f, z * (mouth + 0.6f)), new Vector3(1.6f, 0f, 1.6f), deck, 45f);
                    // Esquinas a 45°: la pelota no se queda atascada en el rincón.
                    for (int z = -1; z <= 1; z += 2)
                        Block(arena, "Corner_" + side + "_" + z, new Vector3(side * (hx - 1f), 0f, z * (hz - 1f)), new Vector3(4.5f, 0f, 4.5f), deck, 45f);
                }
                Skirt(arena, outer, hz + 4f);

                var spawns = new Transform[2];
                var goals = new GoalZone[2];
                for (int team = 0; team < 2; team++)
                {
                    float side = team == 0 ? -1f : 1f;
                    spawns[team] = Marker(root.transform, "Spawn_Team" + team, new Vector3(side * 11f, 0.3f, 0f), Quaternion.Euler(0f, side < 0f ? 90f : -90f, 0f));
                    goals[team] = Goal(arena, team, side * (hx + depth * 0.55f), mouth, depth, BalsaGolGame.TeamColors[team]);
                }
                var ballSpawn = Marker(root.transform, "BallSpawn", new Vector3(0f, 1.2f, 0f), Quaternion.identity);
                var ball = Ball(root.transform, data);
                game.Configure(data, spawner, Balance);
                game.ConfigureArena(spawns, ballSpawn, ball, goals, new[] { Mat("M_Party_Team0", BalsaGolGame.TeamColors[0]), Mat("M_Party_Team1", BalsaGolGame.TeamColors[1]) });
                MidLine(arena, hz);
                Save(scene, data.sceneName);
            }
            finally
            {
                End(scene);
            }
        }

        private static GoalZone Goal(Transform parent, int team, float x, float mouth, float depth, Color color)
        {
            var go = new GameObject("Goal_Team" + team);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(depth * 0.7f, 5f, 2f * mouth - 0.4f);
            var zone = go.AddComponent<GoalZone>();
            zone.Configure(team);
            var material = Mat("M_Party_Team" + team, color);
            float line = x - Mathf.Sign(x) * depth * 0.55f;
            for (int z = -1; z <= 1; z += 2)
                Visual(parent, PrimitiveType.Cylinder, "Post_" + team + "_" + z, new Vector3(line, 1.4f, z * mouth), new Vector3(0.45f, 1.6f, 0.45f), material);
            Visual(parent, PrimitiveType.Cube, "Bar_" + team, new Vector3(line, 3f, 0f), new Vector3(0.4f, 0.4f, 2f * mouth + 0.4f), material);
            Visual(parent, PrimitiveType.Cube, "Net_" + team, new Vector3(x + Mathf.Sign(x) * depth * 0.3f, 1.3f, 0f), new Vector3(0.15f, 2.6f, 2f * mouth), Mat("M_Party_Net" + team, Color.Lerp(color, Color.white, 0.55f)));
            return zone;
        }

        private static void MidLine(Transform parent, float hz)
        {
            var white = Mat("M_Party_Buoy", new Color(1f, 0.97f, 0.9f));
            for (float z = -hz + 1.5f; z <= hz - 1.5f; z += 3f)
                Visual(parent, PrimitiveType.Sphere, "Buoy", new Vector3(0f, 0.05f, z), new Vector3(0.45f, 0.3f, 0.45f), white);
        }

        private static PartyBall Ball(Transform parent, PartyGameData data)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BeachBall";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 1.2f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = data.ballMass;
            var material = Mat("M_Party_BeachBall", Color.white);
            material.SetTexture("_BaseMap", BallTexture());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var ball = go.AddComponent<PartyBall>();
            ball.Setup(data.ballRadius, data.ballMass);
            return ball;
        }

        // Gajos de colores de pelota de playa (la U de la esfera recorre la vuelta).
        private static Texture2D BallTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(BALL_TEXTURE);
            if (existing != null)
                return existing;
            var colors = new[] { new Color(0.95f, 0.2f, 0.2f), Color.white, new Color(0.15f, 0.45f, 0.95f), new Color(1f, 0.85f, 0.15f), Color.white, new Color(0.2f, 0.75f, 0.35f) };
            var texture = new Texture2D(192, 64, TextureFormat.RGB24, false);
            for (int x = 0; x < texture.width; x++)
            {
                var color = colors[x * colors.Length / texture.width];
                for (int y = 0; y < texture.height; y++)
                    texture.SetPixel(x, y, y < 4 || y >= texture.height - 4 ? Color.white : color);
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BALL_TEXTURE));
            System.IO.File.WriteAllBytes(BALL_TEXTURE, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(BALL_TEXTURE);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(BALL_TEXTURE);
        }

        // ---------------------------------------------------------------- Rey de la Balsa
        private static void BuildKing(PartyGameData data)
        {
            var scene = Begin();
            try
            {
                const float radius = 10f;
                var root = Common(data, scene, out var spawner, new Vector3(0f, 14.5f, -12.5f), new Vector3(0f, 0f, -0.8f));
                var game = root.AddComponent<ReyBalsaGame>();
                Water(root.transform, new Vector2(radius + 4f, radius + 4f));
                var deck = Mat("M_Party_Deck", new Color(0.95f, 0.86f, 0.66f));
                var stripe = Mat("M_Party_DeckStripe", new Color(0.35f, 0.75f, 0.95f));
                var arena = new GameObject("Arena").transform;
                arena.SetParent(root.transform, false);
                // Anillo ancho: su borde exterior pasa las esquinas del agua cuadrada (radio + 4).
                const int segments = 40;
                const float ringDepth = 12f;
                float width = 2f * Mathf.PI * (radius + ringDepth) / segments * 1.05f;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * 360f / segments;
                    var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Block(arena, "Ring_" + i, direction * (radius + ringDepth * 0.5f), new Vector3(width, 0f, ringDepth), deck, angle);
                    // Borde de baldosas de colores (solo se ve): una franja fina sobre el filo de la piscina.
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "RimTile_" + i;
                    Object.DestroyImmediate(tile.GetComponent<Collider>());
                    tile.transform.SetParent(arena, false);
                    tile.transform.localPosition = direction * (radius + 0.45f) + Vector3.up * (DECK_TOP + 0.01f);
                    tile.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                    tile.transform.localScale = new Vector3(2f * Mathf.PI * (radius + 0.45f) / segments * 1.02f, 0.04f, 0.9f);
                    tile.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? stripe : Mat("M_Party_Buoy", new Color(1f, 0.97f, 0.9f));
                }
                Skirt(arena, radius + 4f, radius + 4f);
                var center = Marker(root.transform, "Center", new Vector3(0f, 0.3f, 0f), Quaternion.identity);
                game.Configure(data, spawner, Balance);
                game.ConfigureArena(center);
                Save(scene, data.sceneName);
            }
            finally
            {
                End(scene);
            }
        }

        // ---------------------------------------------------------------- Pesca de Mangos
        private static void BuildFishing(PartyGameData data)
        {
            var scene = Begin();
            try
            {
                const float half = 13f;
                var root = Common(data, scene, out var spawner, new Vector3(0f, 22f, -17.5f), new Vector3(0f, 0f, -1.2f));
                var game = root.AddComponent<PescaMangosGame>();
                Water(root.transform, new Vector2(half + 4f, half + 4f));
                var deck = Mat("M_Party_Deck", new Color(0.95f, 0.86f, 0.66f));
                var arena = new GameObject("Arena").transform;
                arena.SetParent(root.transform, false);
                Block(arena, "Wall_N", new Vector3(0f, 0f, half + 2f), new Vector3(2f * half + 8f, 0f, 4f), deck);
                Block(arena, "Wall_S", new Vector3(0f, 0f, -half - 2f), new Vector3(2f * half + 8f, 0f, 4f), deck);
                Block(arena, "Wall_E", new Vector3(half + 2f, 0f, 0f), new Vector3(4f, 0f, 2f * half), deck);
                Block(arena, "Wall_W", new Vector3(-half - 2f, 0f, 0f), new Vector3(4f, 0f, 2f * half), deck);
                Skirt(arena, half + 4f, half + 4f);
                var spawns = new Transform[4];
                var models = new GameObject[4];
                var corners = new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };
                for (int i = 0; i < 4; i++)
                {
                    var position = new Vector3(corners[i].x * 9f, 0.3f, corners[i].y * 9f);
                    var look = Quaternion.LookRotation(-new Vector3(position.x, 0f, position.z).normalized);
                    spawns[i] = Marker(root.transform, "Spawn_J" + (i + 1), position, look);
                    models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(MINI_PREFABS + (i + 1) + ".prefab");
                }
                game.Configure(data, spawner, Balance);
                game.ConfigureArena(spawns, models, AssetDatabase.LoadAssetAtPath<GameObject>(MANGO_PREFAB), new Vector2(half - 2.5f, half - 2.5f));
                Save(scene, data.sceneName);
            }
            finally
            {
                End(scene);
            }
        }

        // ---------------------------------------------------------------- piezas comunes
        private static BalanceConfig Balance => AssetDatabase.LoadAssetAtPath<BalanceConfig>(BALANCE_PATH);

        private static Scene Begin()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        private static void Save(Scene scene, string name)
        {
            if (!AssetDatabase.IsValidFolder(SCENES.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/_Project/Scenes/Juego", "Fiesta");
            WorldLook.ApplyEnvironment(0);
            EditorSceneManager.SaveScene(scene, SCENES + name + ".unity");
        }

        private static void End(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        // Cámara fija, luz, volumen por defecto (día), nivel base del agua, fondo de la piscina y el RaftSpawner.
        private static GameObject Common(PartyGameData data, Scene scene, out RaftSpawner spawner, Vector3 cameraPosition, Vector3 look)
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cam.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.farClipPlane = 400f;
            cam.AddComponent<AudioListener>();
            cam.transform.position = cameraPosition;
            cam.transform.rotation = Quaternion.LookRotation(look - cameraPosition);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            var volume = new GameObject("Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volume_Default.asset");

            var root = new GameObject("Party_" + data.id);
            root.AddComponent<WaterBaseLevel>().Configure(0f);
            spawner = new GameObject("RaftSpawner").AddComponent<RaftSpawner>();
            spawner.transform.SetParent(root.transform, false);
            spawner.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(RAFT_PREFAB), AssetDatabase.LoadAssetAtPath<GameObject>(AVATAR_PREFAB), null);
            return root;
        }

        private static void Water(Transform parent, Vector2 half)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.layer = GameLayers.Water;
            water.transform.SetParent(parent, false);
            water.transform.localScale = new Vector3(half.x * 0.2f, 1f, half.y * 0.2f);
            water.GetComponent<MeshRenderer>().sharedMaterial = WorldLook.Water(0);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PoolFloor";
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, POOL_FLOOR, 0f);
            floor.transform.localScale = new Vector3(half.x * 0.2f, 1f, half.y * 0.2f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_Party_PoolFloor", new Color(0.55f, 0.85f, 0.95f));
        }

        // Playa alrededor: cuatro losas sin colisión desde el rectángulo exterior del borde hasta lejos (tapan el vacío).
        private static void Skirt(Transform parent, float hx, float hz)
        {
            const float reach = 80f;
            var sand = Mat("M_Party_Sand", new Color(0.98f, 0.9f, 0.7f));
            var size = new[] { new Vector3(2f * (hx + reach), 0.2f, reach), new Vector3(2f * (hx + reach), 0.2f, reach), new Vector3(reach, 0.2f, 2f * hz), new Vector3(reach, 0.2f, 2f * hz) };
            var center = new[] { new Vector3(0f, 0f, hz + reach * 0.5f), new Vector3(0f, 0f, -hz - reach * 0.5f), new Vector3(hx + reach * 0.5f, 0f, 0f), new Vector3(-hx - reach * 0.5f, 0f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Beach_" + i;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.isStatic = true;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = center[i] + Vector3.up * (DECK_TOP - 0.2f);
                go.transform.localScale = size[i];
                go.GetComponent<MeshRenderer>().sharedMaterial = sand;
            }
        }

        // Bloque del borde (capa Environment): de DECK_BOTTOM a DECK_TOP, 'size.y' se ignora.
        private static void Block(Transform parent, string name, Vector3 center, Vector3 size, Material material, float yaw = 0f, float top = DECK_TOP)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = GameLayers.Environment;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(center.x, (top + DECK_BOTTOM) * 0.5f, center.z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(size.x, top - DECK_BOTTOM, size.z);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void Visual(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Transform Marker(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            return go.transform;
        }

        private static Material Mat(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MATS.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Materials", "Party");
            string path = MATS + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("GasSafari/SG_Toon"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddToBuildSettings(PartyGameData[] games)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var game in games)
            {
                if (!game.implemented)
                    continue;
                string path = SCENES + game.sceneName + ".unity";
                if (scenes.Exists(s => s.path == path))
                    continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
