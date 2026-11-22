using System.Collections.Generic;
using System.Text;
using GasSafari.Bots;
using GasSafari.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace GasSafari.Editor
{
    // PV-12: marcas BotHint que faltan (docs/10 §15): Ramp delante de cada rampa del cauce (Marty galopa), Drop delante de cada
    // abismo H22 (Marty galopa) y Whirlpool sobre cada remolino H09 (Gloria hace Panzazo dentro). Lee la escena ya armada:
    // las rampas salen de la pendiente de la spline del cauce, los abismos de los Abyss y los remolinos de los Whirlpool.
    // No duplica las marcas que el builder ya puso a mano. Lo llama GreyboxLevel.Finish y el menú para las escenas existentes.
    public static class BotHintPass
    {
        private const string SCENES = "Assets/_Project/Scenes/Juego/Niveles/";
        // Una rampa: subida de al menos 4.5° sostenida 4 m que levanta al menos 0.8 m.
        private const float RAMP_SLOPE = 0.08f;
        private const float RAMP_MIN_RUN = 4f;
        private const float RAMP_MIN_RISE = 0.8f;
        // La marca queda unos metros antes del borde (BotConfig.hintRange 12 m: Marty galopa al tenerla delante).
        private const float RAMP_LEAD = 4f;
        private const float DROP_LEAD = 4f;
        private const float HINT_RADIUS = 5f;
        // Una marca Ramp o Drop a menos de esto antes del punto ya lo cubre (la rampa de despegue de un abismo, por ejemplo).
        private const float COVERED_BEFORE = 20f;
        private const float COVERED_AFTER = 10f;

        public struct Counts
        {
            public int Ramp, Drop, Whirlpool;
            public int Total => Ramp + Drop + Whirlpool;
        }

        [MenuItem("Gas Safari/Dev/Agregar marcas BotHint que faltan (PV-12)")]
        public static void ApplyAll() => Run(true);

        [MenuItem("Gas Safari/Dev/Contar marcas BotHint que faltan (PV-12)")]
        public static void CountAll() => Run(false);

        private static void Run(bool save)
        {
            var report = new StringBuilder(save ? "PV-12 BotHint agregadas:" : "PV-12 BotHint que faltan:");
            int total = 0;
            foreach (var name in CollisionAudit.Scenes)
            {
                var scene = EditorSceneManager.OpenScene(SCENES + name + ".unity", OpenSceneMode.Single);
                var counts = Apply(scene, report);
                total += counts.Total;
                report.Append($" | {name}: rampa {counts.Ramp}, abismo {counts.Drop}, remolino {counts.Whirlpool}");
                if (save && counts.Total > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            Debug.Log(report.Append($" | total {total}").ToString());
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        public static Counts Apply(Scene scene, StringBuilder detail = null)
        {
            var counts = new Counts();
            SplineContainer container = null;
            var hints = new List<BotHint>();
            var abysses = new List<Abyss>();
            var pools = new List<Whirlpool>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (container == null)
                    container = root.GetComponentInChildren<SplineContainer>(true);
                hints.AddRange(root.GetComponentsInChildren<BotHint>(true));
                abysses.AddRange(root.GetComponentsInChildren<Abyss>(true));
                pools.AddRange(root.GetComponentsInChildren<Whirlpool>(true));
            }
            if (container == null)
                return counts;

            var course = new Course(container);
            // El grupo se crea solo si hace falta una marca.
            Transform parent = null;
            Transform Parent() => parent != null ? parent : (parent = HintParent(scene, hints));
            if (detail != null)
                foreach (var hint in hints)
                    if (hint.Type == BotHintType.Ramp || hint.Type == BotHintType.Drop || hint.Type == BotHintType.SuperFart)
                        detail.Append($" ({hint.Type} {course.DistanceOf(hint.transform.position):F0})");

            // Rampas: tramos que suben.
            foreach (float start in course.RampStarts(RAMP_SLOPE, RAMP_MIN_RUN, RAMP_MIN_RISE))
            {
                float at = Mathf.Max(0f, start - RAMP_LEAD);
                if (Covered(course, hints, at))
                    continue;
                hints.Add(Create(Parent(), course, BotHintType.Ramp, at, HINT_RADIUS));
                counts.Ramp++;
                detail?.Append($" [rampa en {start:F0} m, marca {at:F0}]");
            }

            // Abismos: el borde de entrada del trigger (el hueco sin agua).
            foreach (var abyss in abysses)
            {
                var box = abyss.GetComponent<BoxCollider>();
                if (box == null)
                    continue;
                float edge = course.EntryDistance(box);
                float at = Mathf.Max(0f, edge - DROP_LEAD);
                if (Covered(course, hints, at))
                    continue;
                hints.Add(Create(Parent(), course, BotHintType.Drop, at, HINT_RADIUS));
                counts.Drop++;
                detail?.Append($" [abismo {abyss.name} en {edge:F0} m, marca {at:F0}]");
            }

            // Remolinos: la marca cubre el radio de atracción.
            foreach (var pool in pools)
            {
                float radius = pool.Radius;
                bool marked = false;
                foreach (var hint in hints)
                {
                    if (hint != null && hint.Type == BotHintType.Whirlpool && Flat(hint.transform.position - pool.transform.position).magnitude <= radius)
                        marked = true;
                }
                if (marked)
                    continue;
                var go = new GameObject("BotHint_Whirlpool");
                go.transform.SetParent(Parent(), false);
                go.transform.position = pool.transform.position;
                var added = go.AddComponent<BotHint>();
                added.Configure(BotHintType.Whirlpool, radius);
                hints.Add(added);
                counts.Whirlpool++;
                detail?.Append($" [remolino {pool.name} r {radius:F0}]");
            }
            return counts;
        }

        // ¿Ya hay una marca Ramp o Drop sobre el cauce entre COVERED_BEFORE m antes y COVERED_AFTER m después de 'distance'?
        private static bool Covered(Course course, List<BotHint> hints, float distance)
        {
            foreach (var hint in hints)
            {
                if (hint == null || (hint.Type != BotHintType.Ramp && hint.Type != BotHintType.Drop))
                    continue;
                if (course.LateralOffset(hint.transform.position) > 10f)
                    continue;
                float d = course.DistanceOf(hint.transform.position);
                if (d >= distance - COVERED_BEFORE && d <= distance + COVERED_AFTER)
                    return true;
            }
            return false;
        }

        private static BotHint Create(Transform parent, Course course, BotHintType type, float distance, float radius)
        {
            var go = new GameObject("BotHint_" + type);
            go.transform.SetParent(parent, false);
            go.transform.position = course.PointAt(distance);
            var forward = Flat(course.DirectionAt(distance));
            go.transform.rotation = Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward);
            var hint = go.AddComponent<BotHint>();
            hint.Configure(type, radius);
            return hint;
        }

        // Junto a las marcas que ya haya; si no, un grupo BotHints al lado del de checkpoints (o en la raíz).
        private static Transform HintParent(Scene scene, List<BotHint> hints)
        {
            foreach (var hint in hints)
                if (hint != null)
                    return hint.transform.parent;
            Transform checkpoints = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Checkpoints")
                        checkpoints = t;
            var go = new GameObject("BotHints");
            SceneManager.MoveGameObjectToScene(go, scene);
            if (checkpoints != null)
                go.transform.SetParent(checkpoints.parent, false);
            return go.transform;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        // El cauce muestreado cada metro (distancia 3D, como GreyboxLevel).
        private class Course
        {
            private const float STEP = 1f;
            private readonly List<Vector3> points = new List<Vector3>();

            public Course(SplineContainer container)
            {
                float length = container.CalculateLength();
                int steps = Mathf.Max(2, Mathf.CeilToInt(length / STEP));
                for (int i = 0; i <= steps; i++)
                    points.Add(container.EvaluatePosition(i / (float)steps));
            }

            private float Step(int i) => Vector3.Distance(points[i], points[i + 1]);

            public float Length
            {
                get
                {
                    float sum = 0f;
                    for (int i = 0; i < points.Count - 1; i++)
                        sum += Step(i);
                    return sum;
                }
            }

            public Vector3 PointAt(float distance)
            {
                Locate(distance, out int i, out float t);
                return Vector3.Lerp(points[i], points[i + 1], t);
            }

            public Vector3 DirectionAt(float distance)
            {
                Locate(distance, out int i, out _);
                return points[i + 1] - points[i];
            }

            private void Locate(float distance, out int index, out float t)
            {
                float sum = 0f;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    float step = Step(i);
                    if (sum + step >= distance)
                    {
                        index = i;
                        t = step > 1e-4f ? (distance - sum) / step : 0f;
                        return;
                    }
                    sum += step;
                }
                index = points.Count - 2;
                t = 1f;
            }

            // Distancia por el cauce del punto más cercano (en planta).
            public float DistanceOf(Vector3 point)
            {
                float best = float.MaxValue, bestDistance = 0f, sum = 0f;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var a = Flat(points[i]);
                    var ab = Flat(points[i + 1]) - a;
                    float t = Mathf.Clamp01(Vector3.Dot(Flat(point) - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                    float d = (Flat(point) - (a + ab * t)).sqrMagnitude;
                    float step = Step(i);
                    if (d < best)
                    {
                        best = d;
                        bestDistance = sum + step * t;
                    }
                    sum += step;
                }
                return bestDistance;
            }

            public float LateralOffset(Vector3 point)
            {
                var nearest = PointAt(DistanceOf(point));
                return Flat(point - nearest).magnitude;
            }

            // Primer metro del cauce dentro de la caja, en planta y en su espacio local (el trigger gira con el cauce: su AABB
            // empezaba hasta 18 m antes del hueco).
            public float EntryDistance(BoxCollider box)
            {
                var t = box.transform;
                var half = Vector3.Scale(box.size, new Vector3(0.5f, 0.5f, 0.5f));
                float sum = 0f;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var world = points[i];
                    world.y = t.TransformPoint(box.center).y;
                    var local = t.InverseTransformPoint(world) - box.center;
                    if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z)
                        return sum;
                    sum += Step(i);
                }
                return DistanceOf(t.TransformPoint(box.center));
            }

            // Comienzo de cada subida: pendiente > slope sostenida 'minRun' m que suma 'minRise' m.
            public List<float> RampStarts(float slope, float minRun, float minRise)
            {
                var starts = new List<float>();
                float sum = 0f, runStart = -1f, run = 0f, rise = 0f;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    float step = Step(i);
                    float horizontal = Flat(points[i + 1] - points[i]).magnitude;
                    float dy = points[i + 1].y - points[i].y;
                    bool up = horizontal > 1e-4f && dy / horizontal > slope;
                    if (up)
                    {
                        if (runStart < 0f)
                        {
                            runStart = sum;
                            run = rise = 0f;
                        }
                        run += horizontal;
                        rise += dy;
                    }
                    else if (runStart >= 0f)
                    {
                        if (run >= minRun && rise >= minRise)
                            starts.Add(runStart);
                        runStart = -1f;
                    }
                    sum += step;
                }
                if (runStart >= 0f && run >= minRun && rise >= minRise)
                    starts.Add(runStart);
                return starts;
            }
        }
    }
}
