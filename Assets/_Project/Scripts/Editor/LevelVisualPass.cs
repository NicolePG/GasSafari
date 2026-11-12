using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GasSafari.Core;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace GasSafari.Editor
{
    // Pulido visual de un nivel ya construido (skill level-builder, "Dirección visual"). Es idempotente: cada paso borra
    // lo que generó antes. Pasos: agua del mundo en todos los planos y cintas, cinta de corriente con espuma de orilla,
    // cielo con nubes y niebla, fondos por capas (LevelBackdrops).
    public static class LevelVisualPass
    {
        private const string ROOT_NAME = "VisualPass";
        private const float WATER_MARGIN = 900f;

        [MenuItem("Gas Safari/Visual/Pulir escena abierta")]
        public static void ApplyToOpenScene()
        {
            Apply(SceneManager.GetActiveScene());
        }

        [MenuItem("Gas Safari/Visual/Pulir todos los niveles")]
        public static void ApplyToAllLevels()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes/Juego/Niveles" })
                         .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Apply(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("VISUAL pulido terminado");
        }

        public static int WorldOf(string sceneName)
        {
            var m = Regex.Match(sceneName, @"_W(\d)-");
            return m.Success ? int.Parse(m.Groups[1].Value) : 1;
        }

        public static void Apply(Scene scene)
        {
            Apply(scene, WorldOf(scene.name), scene.name.Contains("Tormenta"));
        }

        public static void Apply(Scene scene, int world, bool storm)
        {
            var look = WorldLook.For(world, storm);

            // Raíz propia: lo generado por este pase vive aquí y se rehace entero cada vez.
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == ROOT_NAME).ToArray())
                Object.DestroyImmediate(old);
            var root = new GameObject(ROOT_NAME);
            SceneManager.MoveGameObjectToScene(root, scene);

            var waterMaterial = WorldLook.Water(world, storm);
            SetWaterMaterials(scene, waterMaterial);
            var flow = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Flow");
            Bounds route = RouteBounds(flow);
            ResizeWaterPlane(scene, route);
            bool openSea = scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<CourseBounds>(true) != null);
            BuildFlowRibbon(scene, flow, root.transform, WorldLook.Flow(world, openSea, storm), scene.name);

            WorldLook.ApplyEnvironment(world, storm);
            var camera = scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c != null);
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 4000f);
            }

            LevelBackdrops.Build(scene, world, storm, flow, route, root.transform);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<MeshRenderer>() != null && t.gameObject.name != "WaterFlow")
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"VISUAL {scene.name}: mundo {world}{(storm ? " tormenta" : "")}, ruta {route.size.x:F0}x{route.size.z:F0}");
        }

        // Todos los renderers de la capa Water (plano base, cintas y ramales) usan el material del mundo.
        private static void SetWaterMaterials(Scene scene, Material material)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.gameObject.layer == GameLayers.Water)
                    {
                        r.sharedMaterial = material;
                        EditorUtility.SetDirty(r);
                    }
                }
            }
        }

        public static Bounds RouteBounds(GameObject flow)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            if (flow == null)
                return bounds;
            var container = flow.GetComponent<SplineContainer>();
            bool first = true;
            for (int i = 0; i <= 80; i++)
            {
                Vector3 p = container.EvaluatePosition(i / 80f);
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
            return bounds;
        }

        // El plano de agua base llega muy lejos: bajo los fondos no se ve un borde de agua.
        private static void ResizeWaterPlane(Scene scene, Bounds route)
        {
            var water = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Water");
            if (water == null)
                return;
            var center = route.center;
            float sizeX = route.size.x + WATER_MARGIN * 2f;
            float sizeZ = route.size.z + WATER_MARGIN * 2f;
            water.transform.position = new Vector3(center.x, water.transform.position.y, center.z);
            water.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
            EditorUtility.SetDirty(water);
        }

        // ---------------------------------------------------------------- cinta de corriente
        private static void BuildFlowRibbon(Scene scene, GameObject flowObject, Transform parent, Material material, string sceneName)
        {
            if (flowObject == null)
                return;
            var container = flowObject.GetComponent<SplineContainer>();
            var flow = flowObject.GetComponent<FlowSpline>();
            float length = container.CalculateLength();
            int n = Mathf.Max(2, Mathf.CeilToInt(length / 2f) + 1);
            Physics.SyncTransforms();

            var center = new Vector3[n];
            var dirs = new Vector3[n];
            for (int i = 0; i < n; i++)
                center[i] = container.EvaluatePosition(i / (n - 1f));
            for (int i = 0; i < n; i++)
            {
                var d = center[Mathf.Min(i + 1, n - 1)] - center[Mathf.Max(i - 1, 0)];
                d.y = 0f;
                dirs[i] = d.normalized;
            }

            var vertices = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var triangles = new List<int>();
            bool[] hasWater = new bool[n];
            float run = 0f;
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                    run += Vector3.Distance(center[i - 1], center[i]);
                var right = Vector3.Cross(Vector3.up, dirs[i]);
                float waterY = center[i].y;
                hasWater[i] = Physics.Raycast(center[i] + Vector3.up * 3f, Vector3.down, out var hit, 14f, GameLayers.WaterMask, QueryTriggerInteraction.Ignore);
                if (hasWater[i])
                    waterY = hit.point.y;
                float left = WallDistance(new Vector3(center[i].x, waterY, center[i].z), -right, flow.Width * 0.5f);
                float rightSide = WallDistance(new Vector3(center[i].x, waterY, center[i].z), right, flow.Width * 0.5f);
                float half = (left + rightSide) * 0.5f;
                var mid = new Vector3(center[i].x, waterY, center[i].z) + right * ((rightSide - left) * 0.5f);
                vertices.Add(mid - right * half);
                vertices.Add(mid + right * half);
                uv0.Add(new Vector2(-half, run));
                uv0.Add(new Vector2(half, run));
                uv1.Add(new Vector2(half, 0f));
                uv1.Add(new Vector2(half, 0f));
                if (i > 0 && hasWater[i] && hasWater[i - 1])
                {
                    int v = i * 2;
                    triangles.AddRange(new[] { v - 2, v, v - 1, v - 1, v, v + 1 });
                }
            }
            if (triangles.Count == 0)
                return;

            var mesh = new Mesh { name = "WaterFlow_" + sceneName };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            const string folder = "Assets/_Project/Art/Meshes/WaterFlow";
            EnsureFolder(folder);
            string path = $"{folder}/{sceneName}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject("WaterFlow");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Distancia desde el eje del cauce hasta la cara interior de la pared más cercana en esa dirección.
        private static float WallDistance(Vector3 origin, Vector3 direction, float fallback)
        {
            var hits = Physics.RaycastAll(origin + Vector3.up * 0.5f, direction, 60f, 1 << GameLayers.Environment, QueryTriggerInteraction.Ignore)
                .OrderBy(h => h.distance);
            foreach (var h in hits)
            {
                if (h.collider.gameObject.name == "Wall")
                    return Mathf.Max(1f, h.distance);
            }
            return fallback;
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
