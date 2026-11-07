using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Bots;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // Greybox de un nivel (skill level-builder): parte de _LevelTemplate.unity y arma el cauce a partir de un trayecto de
    // rectas y arcos (spline, paredes con rebote H01, agua), más los elementos del diseño: obstáculos y power-ups desde
    // sus prefabs, checkpoints, mangos dorados, secciones y marcas BotHint. Todo se coloca por distancia a lo largo del
    // cauce y desplazamiento lateral, no por coordenadas.
    public sealed class GreyboxLevel
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Levels/_LevelTemplate.unity";
        private const string SCENES = "Assets/_Project/Scenes/Levels/";
        private const string LEVEL_DATA = "Assets/_Project/Data/Levels/";
        private const string WALL_MATERIAL = "Assets/_Project/Art/Materials/Hazards/M_H01_Wall.mat";
        private const string WALL_CONFIG = "Assets/_Project/Data/Hazards/H01_Wall.asset";
        private const string CHECKPOINT = "Assets/_Project/Prefabs/Level/Checkpoint.prefab";
        private const float STEP = 4f;
        private const float WALL_HEIGHT = 2.5f;
        private const float WALL_THICKNESS = 1f;

        private readonly string levelId;
        private readonly string sceneName;
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> widths = new List<float>();
        private readonly List<float> cumulative = new List<float>();
        private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
        private readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        private readonly List<(float from, float to)> wallGaps = new List<(float, float)>();
        private float heading;
        private bool useBaseWater = true;

        public Scene Scene { get; private set; }
        public float Length => cumulative.Count > 0 ? cumulative[cumulative.Count - 1] : 0f;

        private GreyboxLevel(string id, string scene)
        {
            levelId = id;
            sceneName = scene;
        }

        // Copia la plantilla (si no existe ya la escena del nivel) y la abre de forma aditiva.
        public static GreyboxLevel Create(string id, string scene)
        {
            string path = SCENES + scene + ".unity";
            AssetDatabase.DeleteAsset(path); // se rehace entera: el nivel es una función de este script
            AssetDatabase.CopyAsset(TEMPLATE, path);
            var level = new GreyboxLevel(id, scene);
            level.Scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(level.Scene);
            return level;
        }

        // ---------------------------------------------------------------- trayecto
        public GreyboxLevel StartPath(Vector3 position, float headingDegrees)
        {
            points.Clear();
            widths.Clear();
            cumulative.Clear();
            heading = headingDegrees;
            Add(position, 0f);
            return this;
        }

        public GreyboxLevel Straight(float length, float width)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / STEP));
            float step = length / steps;
            for (int i = 0; i < steps; i++)
                Add(points[points.Count - 1] + Dir(heading) * step, width);
            if (points.Count > 1 && widths[0] == 0f)
                widths[0] = width;
            return this;
        }

        // degrees > 0 gira a la derecha (hacia +X con el rumbo inicial +Z), < 0 a la izquierda.
        public GreyboxLevel Arc(float radius, float degrees, float width)
        {
            float arc = radius * Mathf.Abs(degrees) * Mathf.Deg2Rad;
            int steps = Mathf.Max(2, Mathf.CeilToInt(arc / STEP));
            float turn = degrees / steps;
            float step = arc / steps;
            for (int i = 0; i < steps; i++)
            {
                heading += turn * 0.5f;
                Add(points[points.Count - 1] + Dir(heading) * step, width);
                heading += turn * 0.5f;
            }
            return this;
        }

        private void Add(Vector3 point, float width)
        {
            float distance = points.Count == 0 ? 0f : cumulative[cumulative.Count - 1] + Vector3.Distance(points[points.Count - 1], point);
            points.Add(point);
            widths.Add(width);
            cumulative.Add(distance);
        }

        private static Vector3 Dir(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        // Punto del cauce a 'distance' m desde la salida y 'lateral' m a la derecha del rumbo.
        public Vector3 PointAt(float distance, float lateral = 0f, float height = 0f)
        {
            Sample(distance, out var point, out var direction, out _);
            var right = Vector3.Cross(Vector3.up, direction);
            return point + right * lateral + Vector3.up * height;
        }

        public Vector3 DirectionAt(float distance)
        {
            Sample(distance, out _, out var direction, out _);
            return direction;
        }

        public float WidthAt(float distance)
        {
            Sample(distance, out _, out _, out float width);
            return width;
        }

        private void Sample(float distance, out Vector3 point, out Vector3 direction, out float width)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            int i = 1;
            while (i < cumulative.Count - 1 && cumulative[i] < distance)
                i++;
            float span = Mathf.Max(0.001f, cumulative[i] - cumulative[i - 1]);
            float t = Mathf.Clamp01((distance - cumulative[i - 1]) / span);
            point = Vector3.Lerp(points[i - 1], points[i], t);
            direction = (points[i] - points[i - 1]).normalized;
            width = Mathf.Lerp(widths[i - 1], widths[i], t);
        }

        // Niveles con abismos (H22): sin nivel base, debajo del hueco no hay agua.
        public void NoBaseWater() => useBaseWater = false;

        // Tramo sin paredes (por ejemplo, una abertura a una rotonda ancha).
        public void WallGap(float from, float to) => wallGaps.Add((from, to));

        // ---------------------------------------------------------------- cauce
        // Spline de la corriente, paredes con rebote, agua, salida y meta. 'flow' = aceleración de la corriente (m/s²).
        public void BuildChannel(float flow = 0.2f)
        {
            var flowObject = Find("Flow");
            var container = flowObject.GetComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var p in points)
                spline.Add(new BezierKnot(new float3(p.x, p.y, p.z)), TangentMode.Linear);
            var flowSpline = flowObject.GetComponent<FlowSpline>();
            var so = new SerializedObject(flowSpline);
            so.FindProperty("width").floatValue = widths.Max();
            so.FindProperty("intensity").animationCurveValue = new AnimationCurve(new Keyframe(0f, flow), new Keyframe(1f, flow));
            so.ApplyModifiedPropertiesWithoutUndo();
            flowSpline.Bake();

            BuildWalls();
            BuildWater();

            var start = Find("StartGate").transform;
            start.position = points[0];
            start.rotation = Quaternion.LookRotation(DirectionAt(0f));

            var finish = Find("FinishGate").transform;
            finish.position = PointAt(Length - 1f, 0f, 2f);
            finish.rotation = Quaternion.LookRotation(DirectionAt(Length - 1f));
            var box = finish.GetComponent<BoxCollider>();
            box.size = new Vector3(WidthAt(Length - 1f), 6f, 2f);
        }

        private void BuildWalls()
        {
            var parent = Group("Channel");
            var wallParent = new GameObject("Walls");
            wallParent.transform.SetParent(parent, false);
            wallParent.layer = GameLayers.Environment;
            var config = AssetDatabase.LoadAssetAtPath<HazardConfig>(WALL_CONFIG);
            var behaviour = wallParent.AddComponent<BounceWall>();
            var so = new SerializedObject(behaviour);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            var material = AssetDatabase.LoadAssetAtPath<Material>(WALL_MATERIAL);

            for (int side = -1; side <= 1; side += 2)
            {
                var edge = new List<Vector3>();
                for (int i = 0; i < points.Count; i++)
                {
                    var dir = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                    var right = Vector3.Cross(Vector3.up, dir);
                    edge.Add(points[i] + right * side * (widths[i] * 0.5f + WALL_THICKNESS * 0.5f));
                }
                for (int i = 0; i < edge.Count - 1; i++)
                {
                    float mid = (cumulative[i] + cumulative[i + 1]) * 0.5f;
                    if (wallGaps.Any(g => mid >= g.from && mid <= g.to))
                        continue;
                    Segment(wallParent.transform, edge[i], edge[i + 1], material);
                }
            }
        }

        private static void Segment(Transform parent, Vector3 a, Vector3 b, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Wall";
            cube.layer = GameLayers.Environment;
            cube.transform.SetParent(parent, false);
            var delta = b - a;
            cube.transform.position = (a + b) * 0.5f + Vector3.up * (WALL_HEIGHT * 0.5f - 0.5f);
            cube.transform.rotation = Quaternion.LookRotation(delta.normalized);
            cube.transform.localScale = new Vector3(WALL_THICKNESS, WALL_HEIGHT, delta.magnitude + 0.3f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.BatchingStatic);
        }

        private void BuildWater()
        {
            var water = Find("Water").transform;
            var min = points[0];
            var max = points[0];
            foreach (var p in points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            const float margin = 40f;
            var center = (min + max) * 0.5f;
            water.position = new Vector3(center.x, 0f, center.z);
            // El plano de Unity mide 10 x 10.
            water.localScale = new Vector3((max.x - min.x + margin * 2f) / 10f, 1f, (max.z - min.z + margin * 2f) / 10f);
            // Nivel base del agua fuera de los tramos (docs/02 §1); los niveles con abismos lo quitan con NoBaseWater.
            if (useBaseWater)
            {
                var baseLevel = water.GetComponent<WaterBaseLevel>();
                if (baseLevel == null)
                    baseLevel = water.gameObject.AddComponent<WaterBaseLevel>();
                baseLevel.Configure(0f);
            }
        }

        // ---------------------------------------------------------------- contenido
        public GameObject Place(string prefabPath, float distance, float lateral = 0f, float yaw = 0f, float height = 0f, string group = "Obstacles")
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Scene);
            instance.transform.SetParent(Group(group), false);
            instance.transform.position = PointAt(distance, lateral, height);
            instance.transform.rotation = Quaternion.LookRotation(DirectionAt(distance)) * Quaternion.Euler(0f, yaw, 0f);
            return instance;
        }

        public GameObject PlaceAt(string prefabPath, Vector3 position, float yaw, string group = "Obstacles")
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Scene);
            instance.transform.SetParent(Group(group), false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return instance;
        }

        public GameObject PowerUp(string id, float distance, float lateral = 0f, float height = 1f)
        {
            string path = Directory.GetFiles("Assets/_Project/Prefabs/PowerUps", id + "_*.prefab").First().Replace('\\', '/');
            return Place(path, distance, lateral, 0f, height, "PowerUps");
        }

        // Mango dorado P07; slot 0 = ruta, 1 = habilidad o riesgo, 2 = escondido.
        public GameObject Mango(int slot, float distance, float lateral, float height = 1f)
        {
            var mango = Place("Assets/_Project/Prefabs/PowerUps/P07_GoldenMango.prefab", distance, lateral, 0f, height, "Mangos");
            mango.name = "MangoSpot_" + slot;
            var so = new SerializedObject(mango.GetComponent<PowerUps.GoldenMangoPowerUp>());
            so.FindProperty("slot").intValue = slot;
            so.ApplyModifiedPropertiesWithoutUndo();
            return mango;
        }

        // El primer checkpoint es el de la salida (Checkpoint_0 de la plantilla).
        public Checkpoint AddCheckpoint(float distance, bool familiarOnly = false, bool hardKeep = false)
        {
            Checkpoint checkpoint;
            if (checkpoints.Count == 0)
            {
                checkpoint = Find("Checkpoint_0").GetComponent<Checkpoint>();
            }
            else
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CHECKPOINT), Scene);
                go.name = "Checkpoint_" + checkpoints.Count;
                go.transform.SetParent(Group("Checkpoints"), false);
                checkpoint = go.GetComponent<Checkpoint>();
            }
            checkpoint.transform.position = PointAt(distance, 0f, 0.5f);
            checkpoint.transform.rotation = Quaternion.LookRotation(DirectionAt(distance));
            var so = new SerializedObject(checkpoint);
            so.FindProperty("familiarOnly").boolValue = familiarOnly;
            so.FindProperty("hardKeep").boolValue = hardKeep;
            so.ApplyModifiedPropertiesWithoutUndo();
            checkpoints.Add(checkpoint);
            return checkpoint;
        }

        public void Section(int number, string title, float distance)
        {
            var go = new GameObject($"Section_{number}_{title}");
            go.transform.SetParent(Group("Sections"), false);
            go.transform.position = PointAt(distance);
        }

        public void Hint(BotHintType type, float distance, float lateral = 0f, float radius = 4f)
        {
            var go = new GameObject("BotHint_" + type);
            go.transform.SetParent(Group("BotHints"), false);
            go.transform.position = PointAt(distance, lateral);
            go.AddComponent<BotHint>().Configure(type, radius);
        }

        // Caja de apoyo del greybox (estatuas, vallas, arbustos): sin arte.
        public GameObject Block(string name, Vector3 position, Vector3 size, Color color, float yaw = 0f, bool mesh = true, string group = "Props")
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.layer = GameLayers.Environment;
            cube.transform.SetParent(Group(group), false);
            cube.transform.position = position + Vector3.up * (size.y * 0.5f - 0.5f);
            cube.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cube.transform.localScale = size;
            var renderer = cube.GetComponent<MeshRenderer>();
            var material = new Material(Shader.Find("GasSafari/SG_Toon"));
            material.SetColor("_BaseColor", color);
            renderer.sharedMaterial = SaveMaterial(material, $"{levelId}_{name}");
            renderer.enabled = mesh;
            return cube;
        }

        public GameObject Cylinder(string name, Vector3 position, float radius, float height, Color color, string group = "Props")
        {
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            cyl.layer = GameLayers.Environment;
            cyl.transform.SetParent(Group(group), false);
            cyl.transform.position = position + Vector3.up * (height * 0.5f - 0.5f);
            cyl.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            var material = new Material(Shader.Find("GasSafari/SG_Toon"));
            material.SetColor("_BaseColor", color);
            cyl.GetComponent<MeshRenderer>().sharedMaterial = SaveMaterial(material, $"{levelId}_{name}");
            return cyl;
        }

        // ---------------------------------------------------------------- cierre
        public void Finish(float flowDummy = 0f)
        {
            // CheckpointSystem y LevelManager.
            var manager = Find("LevelManager");
            var system = manager.GetComponent<CheckpointSystem>();
            var so = new SerializedObject(system);
            var array = so.FindProperty("checkpoints");
            array.arraySize = checkpoints.Count;
            for (int i = 0; i < checkpoints.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = checkpoints[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var data = AssetDatabase.LoadAssetAtPath<LevelData>(LEVEL_DATA + levelId + ".asset");
            var manage = new SerializedObject(manager.GetComponent<LevelManager>());
            manage.FindProperty("data").objectReferenceValue = data;
            manage.ApplyModifiedPropertiesWithoutUndo();

            string path = SCENES + sceneName + ".unity";
            EditorSceneManager.MarkSceneDirty(Scene);
            EditorSceneManager.SaveScene(Scene, path);
            EditorSceneManager.CloseScene(Scene, true);
            AddToBuildSettings(path);
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------- utilidades
        private GameObject Find(string name)
        {
            var found = Scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (found == null)
                throw new System.InvalidOperationException($"La plantilla no tiene '{name}'");
            return found;
        }

        private Transform Group(string name)
        {
            if (groups.TryGetValue(name, out var existing))
                return existing;
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, Scene);
            groups[name] = go.transform;
            return go.transform;
        }

        private static Material SaveMaterial(Material material, string name)
        {
            const string folder = "Assets/_Project/Art/Materials/Greybox";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Materials", "Greybox");
            string path = $"{folder}/M_{name}.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
