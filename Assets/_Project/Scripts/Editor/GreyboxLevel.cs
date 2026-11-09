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
    // Greybox de un nivel (skill level-builder): parte de TEST_LevelTemplate.unity y arma el cauce a partir de un trayecto de
    // rectas y arcos (spline, paredes con rebote H01, agua), más los elementos del diseño: obstáculos y power-ups desde
    // sus prefabs, checkpoints, mangos dorados, secciones y marcas BotHint. Todo se coloca por distancia a lo largo del
    // cauce y desplazamiento lateral, no por coordenadas.
    public sealed class GreyboxLevel
    {
        private const string TEMPLATE = "Assets/_Project/Scenes/Pruebas/TEST_LevelTemplate.unity";
        private const string SCENES = "Assets/_Project/Scenes/Juego/Niveles/";
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
        private readonly List<(float from, float to)> waterGaps = new List<(float, float)>();
        private readonly List<(float from, float to, float accel)> flowZones = new List<(float, float, float)>();
        private readonly List<(List<Vector3> points, float width)> branches = new List<(List<Vector3>, float)>();
        private float heading;
        private bool useBaseWater = true;
        private bool openWater;
        // Agua abierta (W2): límite invisible a esta distancia del borde del carril.
        private const float OPEN_MARGIN = 12f;

        public Scene Scene { get; private set; }
        public float Length => cumulative.Count > 0 ? cumulative[cumulative.Count - 1] : 0f;

        private GreyboxLevel(string id, string scene)
        {
            levelId = id;
            sceneName = scene;
        }

        // Todo lo que arma el constructor cuelga de este objeto raíz (se crea en Finish). Volver a correr el constructor lo
        // borra y lo rehace: nunca suma encima, y lo que se puso a mano fuera de él se conserva.
        public const string GENERATED_ROOT = "_Generated";
        // Objetos raíz que existen al abrir la escena (plantilla y lo hecho a mano): no se mueven a _Generated.
        private readonly HashSet<GameObject> keptRoots = new HashSet<GameObject>();

        // Abre la escena del nivel de forma aditiva. Si ya existe y tiene _Generated, borra ese objeto y deja la plantilla en su
        // estado inicial (se conserva lo hecho a mano); si no existe, o es una escena anterior a _Generated, parte de la plantilla.
        public static GreyboxLevel Create(string id, string scene)
        {
            string path = SCENES + scene + ".unity";
            var level = new GreyboxLevel(id, scene);
            bool reused = false;
            if (File.Exists(path))
            {
                var open = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var generated = open.GetRootGameObjects().FirstOrDefault(g => g.name == GENERATED_ROOT);
                if (generated != null)
                {
                    Object.DestroyImmediate(generated);
                    level.Scene = open;
                    level.ResetTemplate();
                    reused = true;
                }
                else
                {
                    EditorSceneManager.CloseScene(open, true);
                }
            }
            if (!reused)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CopyAsset(TEMPLATE, path);
                level.Scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            SceneManager.SetActiveScene(level.Scene);
            foreach (var root in level.Scene.GetRootGameObjects())
                level.keptRoots.Add(root);
            return level;
        }

        // Deja los objetos de la plantilla que el constructor modifica como estaban al copiarla.
        private void ResetTemplate()
        {
            var water = Find("Water");
            var renderer = water.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = true;
            var collider = water.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;
            var baseLevel = water.GetComponent<WaterBaseLevel>();
            if (baseLevel != null)
                Object.DestroyImmediate(baseLevel);
            var bounds = Find("Flow").GetComponent<CourseBounds>();
            if (bounds != null)
                Object.DestroyImmediate(bounds);
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

        // rise: metros que sube (negativo: baja) a lo largo del tramo.
        public GreyboxLevel Straight(float length, float width, float rise = 0f)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / STEP));
            float step = length / steps;
            for (int i = 0; i < steps; i++)
                Add(points[points.Count - 1] + Dir(heading) * step + Vector3.up * (rise / steps), width);
            if (points.Count > 1 && widths[0] == 0f)
                widths[0] = width;
            return this;
        }

        // degrees > 0 gira a la derecha (hacia +X con el rumbo inicial +Z), < 0 a la izquierda.
        public GreyboxLevel Arc(float radius, float degrees, float width, float rise = 0f)
        {
            float arc = radius * Mathf.Abs(degrees) * Mathf.Deg2Rad;
            int steps = Mathf.Max(2, Mathf.CeilToInt(arc / STEP));
            float turn = degrees / steps;
            float step = arc / steps;
            for (int i = 0; i < steps; i++)
            {
                heading += turn * 0.5f;
                Add(points[points.Count - 1] + Dir(heading) * step + Vector3.up * (rise / steps), width);
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

        // Distancia horizontal de un punto al borde de agua más cercano del cauce (negativa dentro del cauce).
        public float ClearanceTo(Vector3 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var ab = new Vector2(b.x - a.x, b.z - a.z);
                var ap = new Vector2(point.x - a.x, point.z - a.z);
                float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                float d = (ap - ab * t).magnitude - Mathf.Lerp(widths[i], widths[i + 1], t) * 0.5f;
                best = Mathf.Min(best, d);
            }
            return best;
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

        // Corriente distinta en un tramo (m/s²); fuera de los tramos vale la de BuildChannel.
        public void FlowZone(float from, float to, float accel) => flowZones.Add((from, to, accel));

        // Ramal paralelo (bifurcación): sale del cauce en 'from' y vuelve en 'to' en línea recta. No lleva corriente ni lo
        // siguen los bots (siguen el cauce principal). Las paredes del cauce se abren donde se tocan.
        public void Branch(float from, float to, float width, float lateral = 0f)
        {
            var a = PointAt(from, lateral);
            var b = PointAt(to, lateral);
            var list = new List<Vector3>();
            int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / STEP));
            for (int i = 0; i <= steps; i++)
                list.Add(Vector3.Lerp(a, b, i / (float)steps));
            branches.Add((list, width));
            float open = width * 0.5f + 2f;
            wallGaps.Add((from - open, from + open));
            wallGaps.Add((to - open, to + open));
        }

        // Agua abierta sin paredes (docs/04 W2): límite invisible lejos del carril y corriente que devuelve (CourseBounds).
        public void OpenWater() => openWater = true;
        public bool IsOpenWater => openWater;

        // Niveles con abismos (H22): sin nivel base, debajo del hueco no hay agua.
        // Abismo (H22): tramo del cauce sin agua; la balsa cae. Hace falta una cinta de agua (se fuerza) y NoBaseWater.
        public void WaterGap(float from, float to) => waterGaps.Add((from, to));

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
            so.FindProperty("intensity").animationCurveValue = FlowCurve(flow);
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

        private AnimationCurve FlowCurve(float flow)
        {
            var curve = new AnimationCurve(new Keyframe(0f, flow), new Keyframe(1f, flow));
            float length = Mathf.Max(1f, Length);
            foreach (var z in flowZones)
            {
                curve.AddKey(Mathf.Clamp01((z.from - 2f) / length), flow);
                curve.AddKey(Mathf.Clamp01(z.from / length), z.accel);
                curve.AddKey(Mathf.Clamp01(z.to / length), z.accel);
                curve.AddKey(Mathf.Clamp01((z.to + 2f) / length), flow);
            }
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
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
                    float margin = openWater ? OPEN_MARGIN : 0f;
                    edge.Add(points[i] + right * side * (widths[i] * 0.5f + margin + WALL_THICKNESS * 0.5f));
                }
                for (int i = 0; i < edge.Count - 1; i++)
                {
                    float mid = (cumulative[i] + cumulative[i + 1]) * 0.5f;
                    if (wallGaps.Any(g => mid >= g.from && mid <= g.to))
                        continue;
                    // Junto a un ramal, la pared que caería dentro de su agua no se pone.
                    if (InsideBranch((edge[i] + edge[i + 1]) * 0.5f))
                        continue;
                    var wall = Segment(wallParent.transform, edge[i], edge[i + 1], material);
                    if (openWater)
                        wall.GetComponent<MeshRenderer>().enabled = false;
                }
            }

            if (openWater)
            {
                var flowObject = Find("Flow");
                var bounds = flowObject.GetComponent<CourseBounds>() ?? flowObject.AddComponent<CourseBounds>();
                bounds.Configure(widths.Max() * 0.5f, 0.6f, 4f);
            }

            foreach (var (branch, width) in branches)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < branch.Count - 1; i++)
                    {
                        var dir = (branch[i + 1] - branch[i]).normalized;
                        var right = Vector3.Cross(Vector3.up, dir);
                        var a = branch[i] + right * side * (width * 0.5f + WALL_THICKNESS * 0.5f);
                        var b = branch[i + 1] + right * side * (width * 0.5f + WALL_THICKNESS * 0.5f);
                        if (ClearanceTo((a + b) * 0.5f) < WALL_THICKNESS)
                            continue; // dentro del cauce principal (salida y llegada del ramal)
                        Segment(wallParent.transform, a, b, material);
                    }
                }
            }
        }

        private bool InsideBranch(Vector3 point)
        {
            foreach (var (branch, width) in branches)
            {
                for (int i = 0; i < branch.Count - 1; i++)
                {
                    var a = new Vector2(branch[i].x, branch[i].z);
                    var ab = new Vector2(branch[i + 1].x, branch[i + 1].z) - a;
                    var ap = new Vector2(point.x, point.z) - a;
                    float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                    if ((ap - ab * t).magnitude < width * 0.5f + WALL_THICKNESS)
                        return true;
                }
            }
            return false;
        }

        private static GameObject Segment(Transform parent, Vector3 a, Vector3 b, Material material)
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
            return cube;
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
            float margin = openWater ? 400f : 40f; // mar abierto: el agua llega al horizonte
            var center = (min + max) * 0.5f;
            // Cauce que baja por debajo de y = 0: el plano y el nivel base se ponen en el punto más bajo (la cinta de agua es el agua real).
            float baseY = Mathf.Min(0f, points.Min(p => p.y));
            water.position = new Vector3(center.x, baseY, center.z);
            // El plano de Unity mide 10 x 10.
            water.localScale = new Vector3((max.x - min.x + margin * 2f) / 10f, 1f, (max.z - min.z + margin * 2f) / 10f);
            // Con desnivel, el agua de cada tramo es una cinta que sigue el cauce (docs/02 §1); el plano queda como nivel base.
            if (points.Any(p => Mathf.Abs(p.y) > 0.01f) || waterGaps.Count > 0)
            {
                var material = water.GetComponent<Renderer>().sharedMaterial;
                // El agua se alarga 8 m antes de la salida y después de la meta: sin nivel base (abismos) la popa de la balsa
                // recién nacida no tendría agua debajo.
                var lead = (points[0] - (points[1] - points[0]).normalized * LEAD);
                var tail = points[points.Count - 1] + (points[points.Count - 1] - points[points.Count - 2]).normalized * LEAD;
                var extended = new List<Vector3> { lead };
                extended.AddRange(points);
                extended.Add(tail);
                var extendedWidths = new List<float> { widths[0] };
                extendedWidths.AddRange(widths);
                extendedWidths.Add(widths[widths.Count - 1]);
                Ribbon("AguaCauce", extended, extendedWidths, material, true);
                foreach (var (branch, width) in branches)
                    Ribbon("AguaRamal", branch, branch.Select(_ => width).ToList(), material);
            }
            // Con abismos el plano de agua base no puede estar: taparía los huecos.
            if (!useBaseWater)
            {
                var planeCollider = water.GetComponent<Collider>();
                if (planeCollider != null)
                    planeCollider.enabled = false;
                water.GetComponent<Renderer>().enabled = false;
            }
            // Nivel base del agua fuera de los tramos (docs/02 §1); los niveles con abismos lo quitan con NoBaseWater.
            if (useBaseWater)
            {
                var baseLevel = water.GetComponent<WaterBaseLevel>();
                if (baseLevel == null)
                    baseLevel = water.gameObject.AddComponent<WaterBaseLevel>();
                baseLevel.Configure(baseY);
            }
        }

        private const float LEAD = 8f;

        private void Ribbon(string name, List<Vector3> line, List<float> lineWidths, Material material, bool main = false)
        {
            // En la cinta principal el primer punto está LEAD m antes de la salida: las distancias de los abismos se miden desde ahí.
            float start = main ? -LEAD : 0f;
            float run = start;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < line.Count; i++)
            {
                var dir = line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)];
                dir.y = 0f;
                var right = Vector3.Cross(Vector3.up, dir.normalized);
                float half = lineWidths[i] * 0.5f + WALL_THICKNESS;
                vertices.Add(line[i] - right * half);
                vertices.Add(line[i] + right * half);
                float previous = run;
                if (i > 0)
                    run += Vector3.Distance(line[i - 1], line[i]);
                if (i == 0)
                    continue;
                // Abismo: sin agua entre las muestras cuyo punto medio cae en un hueco.
                float mid = (previous + run) * 0.5f;
                if (main && waterGaps.Any(g => mid >= g.from && mid <= g.to))
                    continue;
                int v = i * 2;
                triangles.AddRange(new[] { v - 2, v, v - 1, v - 1, v, v + 1 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            const string folder = "Assets/_Project/Art/Meshes/Greybox";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Meshes"))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Meshes");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Meshes", "Greybox");
            string path = $"{folder}/{levelId}_{name}_{line.Count}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, Scene);
            go.layer = GameLayers.Water;
            go.transform.SetParent(Group("Channel"), true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
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
            go.transform.rotation = Quaternion.LookRotation(DirectionAt(distance)); // sentido del cauce (la marca SuperFart lo usa)
            go.AddComponent<BotHint>().Configure(type, radius);
        }

        // Ruta alternativa para los bots (BotHint Route): puntos (distancia del cauce, lateral) en orden; el primero es la entrada,
        // sobre el cauce principal, y el último vuelve a él. Los bots la siguen solo si la prueba fuerza esa ruta
        // (BotHint.ForcedRoute); en el juego siguen el cauce principal.
        public void RouteHint(string routeId, params (float distance, float lateral)[] route)
        {
            RoutePoints(routeId, route.Select(r => PointAt(r.distance, r.lateral, 0.4f)).ToArray());
        }

        // Igual con puntos del mundo (el atajo es una recta entre dos puntos del cauce).
        public void RoutePoints(string routeId, Vector3[] points)
        {
            var go = new GameObject("BotHint_Route_" + routeId);
            go.transform.SetParent(Group("BotHints"), false);
            go.transform.position = points[0];
            var hint = go.AddComponent<BotHint>();
            hint.Configure(BotHintType.Route, 6f);
            hint.ConfigureRoute(routeId, points);
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

        // Puente de boyas sobre un abismo (ayuda de H22): una franja de agua (capa Water: la balsa flota en ella) entre dos
        // hileras de boyas. Devuelve la raíz DESACTIVADA; el Abyss la activa al caer el equipo 2 veces.
        public GameObject BuoyBridge(float from, float to, float width, string group = "Obstacles")
        {
            float length = to - from;
            float middle = (from + to) * 0.5f;
            var root = new GameObject("PuenteBoyas");
            root.transform.SetParent(Group(group), false);
            root.transform.position = PointAt(middle);
            root.transform.rotation = Quaternion.LookRotation(DirectionAt(middle));

            // Franja de agua: el collider (Water) fija la superficie, y la malla la dibuja.
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Agua";
            strip.layer = GameLayers.Water;
            strip.transform.SetParent(root.transform, false);
            strip.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            strip.transform.localScale = new Vector3(width, 0.2f, length + 6f);
            strip.GetComponent<MeshRenderer>().sharedMaterial = Find("Water").GetComponent<Renderer>().sharedMaterial;

            var buoyMaterial = new Material(Shader.Find("GasSafari/SG_Toon"));
            buoyMaterial.SetColor("_BaseColor", new Color(1f, 0.5f, 0.15f));
            SaveMaterial(buoyMaterial, $"{levelId}_PuenteBoyas");
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -length * 0.5f - 2f; z <= length * 0.5f + 2f; z += 2.5f)
                {
                    var buoy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    buoy.name = "Boya";
                    buoy.layer = GameLayers.Environment;
                    buoy.transform.SetParent(root.transform, false);
                    buoy.transform.localPosition = new Vector3(side * (width * 0.5f - 0.3f), 0.2f, z);
                    buoy.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);
                    Object.DestroyImmediate(buoy.GetComponent<Collider>());
                    buoy.GetComponent<MeshRenderer>().sharedMaterial = buoyMaterial;
                }
            }
            root.SetActive(false);
            return root;
        }

        // Grupo de la escena del nivel (se crea al pedirlo): para props con arte propio.
        public Transform GroupOf(string name) => Group(name);

        // Material SG_Toon guardado en Art/Materials/Greybox con color base y, si no es negro, emisivo HDR (Bloom).
        public Material ToonMaterial(string name, Color color, Color emission = default)
        {
            var material = new Material(Shader.Find("GasSafari/SG_Toon"));
            material.SetColor("_BaseColor", color);
            if (emission.maxColorComponent > 0f)
                material.SetColor("_EmissionColor", emission);
            return SaveMaterial(material, $"{levelId}_{name}");
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

            // Todo lo creado durante la construcción (grupos, arte, momentos estrella...) pasa a _Generated.
            var generated = new GameObject(GENERATED_ROOT);
            SceneManager.MoveGameObjectToScene(generated, Scene);
            foreach (var root in Scene.GetRootGameObjects())
            {
                if (root != generated && !keptRoots.Contains(root))
                    root.transform.SetParent(generated.transform, true);
            }

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
