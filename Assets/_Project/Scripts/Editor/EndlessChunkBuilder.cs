using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using GasSafari.Modes;
using GasSafari.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // Infinito y Desafío Diario (docs/06 §1, M7-09/10): tramos de 30-60 m sacados de las recetas de los niveles de M5
    // (EndlessChunkRecipes), sus EndlessChunkData, Resources/EndlessConfig.asset, los datos de la escena y la escena
    // Scenes/Juego/Infinito/Infinito.unity (copia de la plantilla de nivel con EndlessTrack). Todos los tramos son planos,
    // a la altura del agua base, con el mismo ancho: así encajan en cualquier orden. Se puede repetir: rehace todo.
    public static partial class EndlessChunkBuilder
    {
        private const string PREFABS = "Assets/_Project/Prefabs/Endless/";
        private const string DATA = "Assets/_Project/Data/Endless/";
        private const string MATS = "Assets/_Project/Art/Materials/Endless/";
        private const string CONFIG_PATH = "Assets/_Project/Resources/EndlessConfig.asset";
        private const string LEVEL_PATH = "Assets/_Project/Data/Modes/Level_Endless.asset";
        public const string SCENE_PATH = "Assets/_Project/Scenes/Juego/Infinito/Infinito.unity";
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";
        private const string CHECKPOINT = "Assets/_Project/Prefabs/Level/Checkpoint.prefab";
        private const string WALL_MATERIAL = "Assets/_Project/Art/Materials/Hazards/M_H01_Wall.mat";
        private const string WALL_CONFIG = "Assets/_Project/Data/Hazards/H01_Wall.asset";
        private const string MODES = "Assets/_Project/Data/Modes/";
        public const float WIDTH = 12f;
        private const float STEP = 2f;
        private const float WALL_HEIGHT = 2.5f;
        private const float WALL_THICKNESS = 1f;
        private const float BANK_WIDTH = 6f;
        // Plano de agua de 600 m que sigue a la balsa (EndlessTrack).
        private const float WATER_SCALE = 60f;

        [MenuItem("Gas Safari/Setup/Crear tramos de Infinito (M7)")]
        public static void BuildAll()
        {
            var chunks = BuildChunks();
            var level = BuildLevelData();
            var config = BuildConfig(chunks, level);
            BuildScene(config, level);
            MarkModesImplemented();
            AssetDatabase.SaveAssets();
            Debug.Log("M7 Infinito: " + chunks.Length + " tramos, config y escena listos");
        }

        // Solo prefabs y datos de los tramos (la escena no cambia).
        [MenuItem("Gas Safari/Setup/Infinito: rehacer solo los tramos")]
        public static void BuildChunksOnly()
        {
            var chunks = BuildChunks();
            BuildConfig(chunks, AssetDatabase.LoadAssetAtPath<LevelData>(LEVEL_PATH));
            AssetDatabase.SaveAssets();
            Debug.Log("M7 Infinito: " + chunks.Length + " tramos listos");
        }

        // ---------------------------------------------------------------- tramos
        private static EndlessChunkData[] BuildChunks()
        {
            EnsureFolder(PREFABS);
            EnsureFolder(DATA);
            EnsureFolder(MATS);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var result = new List<EndlessChunkData>();
            try
            {
                foreach (var recipe in Recipes())
                {
                    var data = HudBuilder.LoadOrCreate<EndlessChunkData>(DATA + "Chunk_" + recipe.Id + ".asset");
                    var right = BuildPrefab(recipe, 1, out float length, out float turn);
                    data.id = recipe.Id;
                    data.sourceLevel = recipe.Source;
                    data.world = recipe.World;
                    data.difficulty = recipe.Difficulty;
                    data.length = Mathf.Round(length * 10f) / 10f;
                    data.turn = Mathf.Round(turn);
                    data.prefab = right;
                    data.mirroredPrefab = Mathf.Abs(turn) > 0.5f ? BuildPrefab(recipe, -1, out _, out _) : null;
                    EditorUtility.SetDirty(data);
                    result.Add(data);
                }
            }
            finally
            {
                if (previous.IsValid())
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            return result.ToArray();
        }

        private static EndlessChunk BuildPrefab(Recipe recipe, int side, out float length, out float turn)
        {
            var path = new ChunkPath();
            recipe.Path(path, side);
            var chunk = new Chunk(recipe, path, side);
            chunk.BuildWalls();
            recipe.Content?.Invoke(chunk);
            length = path.Length;
            turn = path.Heading;
            string name = "Chunk_" + recipe.Id + (Mathf.Abs(turn) > 0.5f ? (side > 0 ? "_R" : "_L") : string.Empty);
            chunk.Root.name = name;
            chunk.Component.Configure(path.Points.ToArray(), chunk.Accel.ToArray());
            var prefab = PrefabUtility.SaveAsPrefabAsset(chunk.Root, PREFABS + name + ".prefab");
            Object.DestroyImmediate(chunk.Root);
            return prefab.GetComponent<EndlessChunk>();
        }

        // ---------------------------------------------------------------- datos
        private static LevelData BuildLevelData()
        {
            var level = HudBuilder.LoadOrCreate<LevelData>(LEVEL_PATH);
            level.id = "INF";
            level.sceneName = Path.GetFileNameWithoutExtension(SCENE_PATH);
            level.world = null;
            level.orderInWorld = 0;
            level.mangoCount = 0;
            level.showTimer = true;
            level.isBoss = false;
            level.musicId = "mus_w1";
            level.mechanicText = "Infinito y Desafío Diario (docs/06 §1): río generado por EndlessTrack.";
            EditorUtility.SetDirty(level);
            return level;
        }

        private static EndlessConfig BuildConfig(EndlessChunkData[] chunks, LevelData level)
        {
            var config = HudBuilder.LoadOrCreate<EndlessConfig>(CONFIG_PATH);
            config.chunks = chunks;
            config.level = level;
            config.channelWidth = WIDTH;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void MarkModesImplemented()
        {
            foreach (var id in new[] { ProgressionRules.MODE_ENDLESS })
            {
                var mode = AssetDatabase.LoadAssetAtPath<GameModeData>(MODES + "Mode_" + id + ".asset");
                if (mode == null)
                    continue;
                mode.implemented = true;
                EditorUtility.SetDirty(mode);
            }
        }

        // ---------------------------------------------------------------- escena
        private static void BuildScene(EndlessConfig config, LevelData level)
        {
            EnsureFolder(Path.GetDirectoryName(SCENE_PATH).Replace('\\', '/') + "/");
            AssetDatabase.DeleteAsset(SCENE_PATH);
            AssetDatabase.CopyAsset(TEMPLATE, SCENE_PATH);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                GameObject Find(string n) => roots.First(g => g.name == n);
                var manager = Find("LevelManager");
                var so = new SerializedObject(manager.GetComponent<LevelManager>());
                so.FindProperty("data").objectReferenceValue = level;
                so.ApplyModifiedPropertiesWithoutUndo();

                var water = Find("Water");
                water.transform.position = Vector3.zero;
                water.transform.localScale = new Vector3(WATER_SCALE, 1f, WATER_SCALE);
                water.GetComponent<MeshRenderer>().sharedMaterial = WorldLook.Water(1);
                var baseLevel = water.GetComponent<WaterBaseLevel>();
                if (baseLevel == null)
                    baseLevel = water.AddComponent<WaterBaseLevel>();
                baseLevel.Configure(0f);

                var trackObject = new GameObject("EndlessTrack");
                SceneManager.MoveGameObjectToScene(trackObject, scene);
                var track = trackObject.AddComponent<EndlessTrack>();
                track.Configure(config, Find("Flow").GetComponent<FlowSpline>(), manager.GetComponent<CheckpointSystem>(),
                    Find("FinishGate").GetComponent<FinishGate>(), water.transform, AssetDatabase.LoadAssetAtPath<GameObject>(CHECKPOINT));
                EditorUtility.SetDirty(track);

                WorldLook.ApplyEnvironment(1);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, SCENE_PATH);
            }
            finally
            {
                if (previous.IsValid())
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != SCENE_PATH))
                scenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------------------------------------------------------------- trayecto
        // Cauce local de un tramo: empieza en el origen mirando a +Z; un punto cada STEP m.
        private sealed class ChunkPath
        {
            public readonly List<Vector3> Points = new List<Vector3> { Vector3.zero };
            private readonly List<float> cumulative = new List<float> { 0f };

            public float Heading { get; private set; }
            public float Length => cumulative[cumulative.Count - 1];

            public ChunkPath Straight(float length)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / STEP));
                var dir = Quaternion.Euler(0f, Heading, 0f) * Vector3.forward;
                for (int i = 0; i < steps; i++)
                    Add(Points[Points.Count - 1] + dir * (length / steps));
                return this;
            }

            // degrees > 0 gira a la derecha.
            public ChunkPath Arc(float radius, float degrees)
            {
                float length = radius * Mathf.Abs(degrees) * Mathf.Deg2Rad;
                int steps = Mathf.Max(2, Mathf.CeilToInt(length / STEP));
                float delta = degrees / steps;
                for (int i = 0; i < steps; i++)
                {
                    var dir = Quaternion.Euler(0f, Heading + delta * 0.5f, 0f) * Vector3.forward;
                    Heading += delta;
                    Add(Points[Points.Count - 1] + dir * (length / steps));
                }
                return this;
            }

            private void Add(Vector3 point)
            {
                cumulative.Add(Length + Vector3.Distance(Points[Points.Count - 1], point));
                Points.Add(point);
            }

            public int IndexAt(float distance)
            {
                for (int i = 1; i < cumulative.Count; i++)
                {
                    if (cumulative[i] >= distance)
                        return i - 1;
                }
                return cumulative.Count - 2;
            }

            public Vector3 DirectionAt(float distance)
            {
                int i = IndexAt(distance);
                return (Points[i + 1] - Points[i]).normalized;
            }

            public Vector3 PointAt(float distance, float lateral = 0f, float height = 0f)
            {
                int i = IndexAt(distance);
                float span = Mathf.Max(1e-4f, cumulative[i + 1] - cumulative[i]);
                var p = Vector3.Lerp(Points[i], Points[i + 1], Mathf.Clamp01((distance - cumulative[i]) / span));
                var right = Vector3.Cross(Vector3.up, DirectionAt(distance));
                return p + right * lateral + Vector3.up * height;
            }

            public float DistanceOf(int index) => cumulative[index];
        }

        // ---------------------------------------------------------------- receta
        private sealed class Recipe
        {
            public string Id;
            public string Source;
            public int World;
            public int Difficulty;
            public float Flow = 0.3f;
            // (trayecto, lado): lado -1 = variante espejada (giros y laterales al revés).
            public Action<ChunkPath, int> Path;
            public Action<Chunk> Content;
        }
    }
}
