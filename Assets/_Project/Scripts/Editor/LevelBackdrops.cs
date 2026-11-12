using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace GasSafari.Editor
{
    // Fondos por capas de Tools/Export/Backdrops (ver su README), importados por BackdropImporter:
    //  - Media (8 piezas por mundo): una cada 70-105 m a lo largo de la ruta, alternando lados, a 75-150 m del eje, mirando al cauce.
    //  - Lejana (6 por mundo): corredor alrededor de toda la ruta (no un anillo fijo: las rutas miden hasta 900 m) a 330/500 m, con
    //    remates detrás de la salida y delante de la meta.
    //  - Suelo: cuadrícula de 200 m (A/B) bajo el agua, más la falda y la cadena del horizonte.
    // Todas a la altura del agua base del nivel. Sin colliders. Static batching; LODGroup que las oculta si quedan diminutas.
    public static class LevelBackdrops
    {
        private const string FOLDER = "Assets/_Project/Art/Environment/Backdrops/";
        private const float MID_MIN_OFFSET = 75f, MID_MAX_OFFSET = 150f, MID_CLEARANCE = 70f;
        private const float MID_STEP_MIN = 70f, MID_STEP_MAX = 105f;
        private const float FAR_NEAR = 330f, FAR_FAR = 500f, FAR_SPREAD = 30f;
        private const float CEILING_MID = 55f, CEILING_FAR = 170f;
        private const float TILE = 200f, TILE_DROP = 1.8f; // README: 0.4 m; con el agua translúcida y la espuma por profundidad, el relieve de 0.7 m asomaba y dejaba bajíos blancos por todo el lago
        private const float SEA_DEPTH = 7f; // W2: mar profundo, sin bajíos de colores parecidos al agua

        public static void Build(Scene scene, int world, bool storm, GameObject flowObject, Bounds route, Transform parent)
        {
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Backdrops").ToArray())
                Object.DestroyImmediate(old);
            if (flowObject == null)
                return;
            var rng = new System.Random(scene.name.GetHashCode() ^ 0x5EED);
            var container = flowObject.GetComponent<SplineContainer>();
            float length = container.CalculateLength();
            int n = Mathf.Max(8, Mathf.CeilToInt(length / 5f) + 1);
            var points = new Vector3[n];
            for (int i = 0; i < n; i++)
                points[i] = container.EvaluatePosition(i / (n - 1f));
            var water = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Water");
            float baseY = water != null ? water.transform.position.y : points.Min(p => p.y);

            var root = new GameObject("Backdrops").transform;
            root.SetParent(parent, false);
            var mid = Group(root, "Mid");
            var far = Group(root, "Far");
            var ground = Group(root, "Ground");
            var pieces = new Dictionary<string, GameObject>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { FOLDER + "W" + world }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                pieces[go.name] = go;
            }
            var midPieces = pieces.Where(p => p.Key.Contains("_Mid_")).OrderBy(p => p.Key).Select(p => p.Value).ToList();
            var farPieces = pieces.Where(p => p.Key.Contains("_Far_") && !p.Key.Contains("SunsetRidge")).OrderBy(p => p.Key).Select(p => p.Value).ToList();
            if (midPieces.Count == 0)
            {
                Debug.LogWarning("Backdrops: faltan piezas de W" + world + " (importar con Gas Safari/Visual/Importar fondos)");
                return;
            }

            int midCount = BuildMid(midPieces, mid, points, length, baseY, rng);
            int farCount = BuildFar(farPieces, pieces, far, points, length, baseY, rng, world);
            int tiles = BuildGround(pieces, ground, route, baseY, world, world == 2 ? SEA_DEPTH : TILE_DROP);
            int tris = CountTriangles(root.gameObject);
            Debug.Log($"BACKDROP {scene.name}: media {midCount}, lejana {farCount}, suelo {tiles}, {tris / 1000}k triángulos");
        }

        // ---------------------------------------------------------------- capa media
        private static int BuildMid(List<GameObject> pieces, Transform parent, Vector3[] points, float length, float baseY, System.Random rng)
        {
            var bag = new Queue<GameObject>();
            int count = 0;
            int side = rng.Next(2) == 0 ? -1 : 1;
            float s = (float)rng.NextDouble() * 40f;
            while (s < length + 40f)
            {
                SampleAt(points, s, out var p, out var dir);
                var right = Vector3.Cross(Vector3.up, dir);
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    float d = Mathf.Lerp(MID_MIN_OFFSET, MID_MAX_OFFSET, (float)rng.NextDouble()) + attempt * 8f;
                    int sd = attempt % 2 == 0 ? side : -side;
                    var pos = p + right * (sd * d);
                    if (MinDistance(pos, points) < MID_CLEARANCE)
                        continue;
                    var piece = Next(bag, pieces, rng);
                    var toRoute = p - pos;
                    toRoute.y = 0f;
                    float yaw = Mathf.Atan2(toRoute.x, toRoute.z) * Mathf.Rad2Deg + (float)(rng.NextDouble() * 30.0 - 15.0);
                    Place(piece, parent, pos, baseY, yaw, CEILING_MID, 0.012f);
                    count++;
                    break;
                }
                side = -side;
                s += Mathf.Lerp(MID_STEP_MIN, MID_STEP_MAX, (float)rng.NextDouble());
            }
            return count;
        }

        // ---------------------------------------------------------------- capa lejana
        private static int BuildFar(List<GameObject> pieces, Dictionary<string, GameObject> all, Transform parent, Vector3[] points,
            float length, float baseY, System.Random rng, int world)
        {
            var bag = new Queue<GameObject>();
            int count = 0;
            var placed = new List<Vector3>();
            // Corredor a los dos lados de la ruta.
            int index = 0;
            for (float s = 40f; s < length + 120f; s += Mathf.Lerp(110f, 150f, (float)rng.NextDouble()), index++)
            {
                SampleAt(points, s, out var p, out var dir);
                var right = Vector3.Cross(Vector3.up, dir);
                for (int side = -1; side <= 1; side += 2)
                {
                    float radius = ((index + (side > 0 ? 1 : 0)) % 2 == 0 ? FAR_NEAR : FAR_FAR) + (float)(rng.NextDouble() * 2.0 - 1.0) * FAR_SPREAD;
                    TryFar(Next(bag, pieces, rng), parent, p + right * (side * radius), points, radius, baseY, placed, ref count);
                }
            }
            // Remates: detrás de la salida y delante de la meta.
            SampleAt(points, 0f, out var start, out var startDir);
            SampleAt(points, length, out var end, out var endDir);
            foreach (float angle in new[] { -75f, -25f, 25f, 75f })
            {
                float radius = (Mathf.Abs(angle) > 50f ? FAR_NEAR : FAR_FAR) + (float)(rng.NextDouble() * 2.0 - 1.0) * FAR_SPREAD;
                TryFar(Next(bag, pieces, rng), parent, start + Quaternion.Euler(0f, angle, 0f) * (-startDir) * radius, points, radius, baseY, placed, ref count);
                TryFar(Next(bag, pieces, rng), parent, end + Quaternion.Euler(0f, angle, 0f) * endDir * radius, points, radius, baseY, placed, ref count);
            }
            // W5: el disco solar de las crestas queda del lado de donde viene la luz.
            if (world == 5 && all.TryGetValue("W5_Far_SunsetRidge", out var ridge))
            {
                var light = Object.FindFirstObjectByType<Light>();
                var toSun = light != null ? -light.transform.forward : new Vector3(0.8f, 0f, 0.6f);
                toSun.y = 0f;
                toSun.Normalize();
                var center = Vector3.Lerp(start, end, 0.5f);
                var pos = center + toSun * 480f;
                Place(ridge, parent, pos, baseY, Mathf.Atan2(-toSun.x, -toSun.z) * Mathf.Rad2Deg, CEILING_FAR, 0.006f);
                count++;
            }
            return count;
        }

        private static void TryFar(GameObject piece, Transform parent, Vector3 pos, Vector3[] points, float radius, float baseY,
            List<Vector3> placed, ref int count)
        {
            if (MinDistance(pos, points) < radius - 80f)
                return; // la ruta da vueltas y la pieza quedaría cerca de otro tramo
            if (placed.Any(q => (new Vector2(q.x, q.z) - new Vector2(pos.x, pos.z)).magnitude < 70f))
                return;
            var nearest = NearestPoint(pos, points);
            var toRoute = nearest - pos;
            toRoute.y = 0f;
            Place(piece, parent, pos, baseY, Mathf.Atan2(toRoute.x, toRoute.z) * Mathf.Rad2Deg, CEILING_FAR, 0.006f);
            placed.Add(pos);
            count++;
        }

        // ---------------------------------------------------------------- suelo
        private static int BuildGround(Dictionary<string, GameObject> pieces, Transform parent, Bounds route, float baseY, int world, float drop)
        {
            int count = 0;
            var center = route.center;
            float y = baseY - drop;
            if (pieces.TryGetValue($"W{world}_Ground_TileA", out var a) && pieces.TryGetValue($"W{world}_Ground_TileB", out var b))
            {
                // Cuadrícula que cubre la ruta más 100 m de orilla: lo demás lo cubren la falda y el horizonte.
                int nx = Mathf.Max(2, Mathf.CeilToInt((route.size.x + 200f) / TILE));
                int nz = Mathf.Max(2, Mathf.CeilToInt((route.size.z + 200f) / TILE));
                for (int ix = 0; ix < nx; ix++)
                {
                    for (int iz = 0; iz < nz; iz++)
                    {
                        var pos = new Vector3(center.x + (ix - (nx - 1) * 0.5f) * TILE, y, center.z + (iz - (nz - 1) * 0.5f) * TILE);
                        Place((ix + iz) % 2 == 0 ? a : b, parent, pos, y, 0f, 0f, 0f, lod: false);
                        count++;
                    }
                }
            }
            foreach (var name in new[] { "Skirt", "HorizonRing" })
            {
                if (pieces.TryGetValue($"W{world}_Ground_{name}", out var piece))
                {
                    Place(piece, parent, new Vector3(center.x, y, center.z), y, 0f, 0f, 0f, lod: false);
                    count++;
                }
            }
            return count;
        }

        // ---------------------------------------------------------------- utilidades
        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Next(Queue<GameObject> bag, List<GameObject> pieces, System.Random rng)
        {
            if (bag.Count == 0)
            {
                foreach (var piece in pieces.OrderBy(_ => rng.Next()))
                    bag.Enqueue(piece);
            }
            return bag.Dequeue();
        }

        private static void Place(GameObject prefab, Transform parent, Vector3 position, float y, float yaw, float ceiling, float lodHeight, bool lod = true)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            // Piezas colgantes (estalactitas, techo): el pivote está en el techo.
            bool hanging = prefab.name.Contains("Stalactites") || prefab.name.Contains("CeilingTile");
            go.transform.SetPositionAndRotation(new Vector3(position.x, hanging ? y + ceiling : y, position.z), Quaternion.Euler(0f, yaw, 0f));
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            if (lod)
            {
                var group = go.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(lodHeight, go.GetComponentsInChildren<Renderer>()) });
                group.RecalculateBounds();
            }
        }

        private static void SampleAt(Vector3[] points, float arc, out Vector3 position, out Vector3 direction)
        {
            float spacing = 0f;
            for (int i = 1; i < points.Length; i++)
                spacing += Vector3.Distance(points[i - 1], points[i]);
            spacing /= points.Length - 1;
            float f = Mathf.Clamp(arc / Mathf.Max(spacing, 0.01f), 0f, points.Length - 1.001f);
            int i0 = Mathf.FloorToInt(f);
            position = Vector3.Lerp(points[i0], points[i0 + 1], f - i0);
            var d = points[Mathf.Min(i0 + 2, points.Length - 1)] - points[Mathf.Max(i0 - 1, 0)];
            d.y = 0f;
            direction = d.sqrMagnitude < 1e-6f ? Vector3.forward : d.normalized;
        }

        private static float MinDistance(Vector3 position, Vector3[] points)
        {
            float best = float.MaxValue;
            var p = new Vector2(position.x, position.z);
            foreach (var q in points)
                best = Mathf.Min(best, (new Vector2(q.x, q.z) - p).magnitude);
            return best;
        }

        private static Vector3 NearestPoint(Vector3 position, Vector3[] points)
        {
            float best = float.MaxValue;
            var result = points[0];
            var p = new Vector2(position.x, position.z);
            foreach (var q in points)
            {
                float d = (new Vector2(q.x, q.z) - p).sqrMagnitude;
                if (d < best) { best = d; result = q; }
            }
            return result;
        }

        private static int CountTriangles(GameObject root)
        {
            int count = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null)
                    for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
                        count += (int)filter.sharedMesh.GetIndexCount(i) / 3;
            return count;
        }
    }
}
