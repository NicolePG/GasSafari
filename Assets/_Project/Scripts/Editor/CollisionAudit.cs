using System.Collections.Generic;
using System.Linq;
using GasSafari.Core;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace GasSafari.Editor
{
    // Auditoría de colisiones de los niveles (docs/11, checklist "nada se traspasa"):
    //  - todo collider sólido del entorno y de los obstáculos mide 0.3 m o más en su dimensión menor (si no, la balsa
    //    lo atraviesa a velocidad aunque sea ContinuousDynamic);
    //  - todo prop visible dentro del cauce (donde la balsa puede chocar con él) tiene un collider sólido simple.
    public static class CollisionAudit
    {
        public const float MIN_THICKNESS = 0.3f;
        // Margen de cauce sobre el ancho de la spline donde aún puede llegar la balsa (m).
        private const float CHANNEL_MARGIN = 1.5f;
        private const float OPEN_WATER_REACH = 12f;
        // Fuera de este rango vertical sobre el agua la balsa no choca con el prop (fondo del cauce o techo alto).
        private const float REACH_BELOW = -0.6f;
        private const float REACH_ABOVE = 3.0f;
        // Las banderas del tutorial quedan por encima de la cabeza de cualquier animal sentado en la balsa.
        private const float FLAG_MIN_HEIGHT = 3.1f;

        public static readonly string[] Scenes =
        {
            "N00_T0_PiscinaZoo", "N01_W1-1_FuentesZoo", "N02_W1-2_Alcantarillas", "N03_W1-3_RioCiudad", "N04_W1-4_MercadoFlotante",
            "N05_W1-5_PuertoJefe", "N06_W2-1_CajasDeriva", "N07_W2-2_Tormenta", "N08_W2-3_CementerioBarcos", "N09_W2-4_GranRemolino",
            "N10_W2-5_ArrecifeJefe",
            "N11_W3-1_PlayaLlegada", "N12_W3-2_RioLianas", "N13_W3-3_TerritorioFosas", "N14_W3-4_PantanoCocodrilos", "N15_W3-5_FiestaLemurJefe",
            "N16_W4-1_CascadasEscalonadas", "N17_W4-2_CuevaCristal", "N18_W4-3_GeiseresVapor", "N19_W4-4_RioSubterraneo", "N20_W4-5_VolcanJefe",
            "N21_W5-1_RapidosCanon", "N22_W5-2_RampasAbismo", "N23_W5-3_LaberintoManglares", "N24_W5-4_Represa", "N25_W5-5_MegatoboganJefe",
            "N26_W6-1_FiestaEspejo", "N27_W6-2_NocheSinLuz", "N28_W6-3_Contracorriente", "N29_W6-4_LluviaCocos", "N30_W6-5_CoronaRey",
        };

        [MenuItem("Gas Safari/QA/Auditar colisiones (T-0, W1-W6)")]
        public static void RunAllMenu()
        {
            var report = RunAll();
            Debug.Log($"COLAUDIT total={report.Count}");
            foreach (var line in report.Take(120))
                Debug.Log("COLAUDIT " + line);
        }

        public static List<string> RunAll()
        {
            var report = new List<string>();
            foreach (var name in Scenes)
                report.AddRange(RunScene($"Assets/_Project/Scenes/Juego/Niveles/{name}.unity"));
            return report;
        }

        [MenuItem("Gas Safari/QA/Corregir props sin collider (T-0, W1-W4)")]
        public static void FixAllMenu()
        {
            foreach (var name in Scenes)
            {
                string path = $"Assets/_Project/Scenes/Juego/Niveles/{name}.unity";
                var before = RunScene(path, true);
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"COLFIX {name} guardada");
            }
            RunAllMenu();
        }

        public static List<string> RunScene(string path, bool fix = false)
        {
            var report = new List<string>();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Physics.SyncTransforms();
            string tag = scene.name;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var c in root.GetComponentsInChildren<Collider>(false))
                {
                    if (c.isTrigger || !IsSolidLayer(c.gameObject.layer))
                        continue;
                    float thickness = SmallestDimension(c);
                    if (thickness < MIN_THICKNESS)
                        report.Add($"{tag}: collider fino ({thickness:F2} m) en {PathOf(c.transform)}");
                }
            }

            var samples = ChannelSamples(scene, out float reach);
            if (samples.Count > 0)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
                        CheckProp(r, samples, reach, tag, report, fix);
                }
            }
            return report.Distinct().ToList();
        }

        private static void CheckProp(MeshRenderer r, List<Vector3> samples, float reach, string tag, List<string> report, bool fixing)
        {
            if (!r.enabled || r.gameObject.layer == GameLayers.Water || r.GetComponent<MeshFilter>() == null)
                return;
            if (IsDecorationRoot(r.transform))
                return;
            var b = r.bounds;
            if (b.max.y < REACH_BELOW || b.min.y > REACH_ABOVE)
                return;
            if (!WithinChannel(b.center, samples, reach + Mathf.Max(b.extents.x, b.extents.z)))
                return;
            // Collider sólido en el propio prop (el objeto que cuelga de su contenedor): en el renderer, un padre o un hermano.
            var propRoot = PropRoot(r.transform);
            var colliders = propRoot.GetComponentsInChildren<Collider>(false);
            foreach (var c in colliders)
            {
                if (!c.isTrigger && IsSolidLayer(c.gameObject.layer))
                    return;
            }
            foreach (var c in r.GetComponentsInParent<Collider>(false))
            {
                if (!c.isTrigger && IsSolidLayer(c.gameObject.layer))
                    return;
            }
            // Otro collider sólido ya ocupa su volumen (el del greybox que se deja bajo el modelo, el ancla de un mástil,
            // una pared que la balsa no puede cruzar): la balsa no llega a atravesar este prop.
            int solidMask = (1 << GameLayers.Environment) | (1 << GameLayers.Hazard);
            if (Physics.CheckBox(b.center, b.extents * 0.6f, Quaternion.identity, solidMask, QueryTriggerInteraction.Ignore))
                return;
            // Chorros, redes y zonas de obstáculo con trigger: no son sólidos por diseño (se atraviesan y actúan por zona).
            foreach (var c in colliders)
            {
                if (c.isTrigger)
                    return;
            }
            if (fixing)
            {
                Fix(r, propRoot, b);
                return;
            }
            report.Add($"{tag}: prop sin collider sólido en {PathOf(r.transform)} (tamaño {b.size.x:F1}x{b.size.y:F1}x{b.size.z:F1}, y {b.min.y:F1}..{b.max.y:F1}, centro {b.center.x:F0},{b.center.z:F0}, a {DistanceToChannel(b.center, samples):F1} m del cauce)");
        }

        // Corrección de un prop sin collider: banderas sobre las cabezas, cestas flotantes livianas, el resto una caja sólida.
        private static void Fix(MeshRenderer r, Transform propRoot, Bounds b)
        {
            string n = propRoot.name;
            if (n.EndsWith("_Bandera"))
                propRoot.position += Vector3.up * (FLAG_MIN_HEIGHT - b.min.y);
            else if (n.Contains("Basket"))
                PropColliders.MakeLightFloater(propRoot.gameObject, 0.6f);
            else
                PropColliders.AddSolidBox(propRoot.gameObject);
        }

        private static readonly string[] Containers = { "Props", "Obstacles", "PowerUps", "Mangos", "Sections", "Hazards", "Channel", "Art", "Tutorial" };

        // El objeto que cuelga de un contenedor de la escena (o de la raíz): el prop completo con todas sus piezas.
        private static Transform PropRoot(Transform t)
        {
            while (t.parent != null && System.Array.IndexOf(Containers, t.parent.name) < 0)
                t = t.parent;
            return t;
        }

        // Superficies sin choque por diseño: agua, aceras, orillas decorativas, cielo.
        private static bool IsDecorationRoot(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                string n = p.name;
                // Puente_N es el tablero (alto, sobre el cauce); sus pilares son bloques sólidos aparte. Neon son tiras
                // de luz pegadas a la pared del túnel.
                if (n.StartsWith("Puente_") && !n.EndsWith("_Pilar") || n == "Neon" || n == "CortinaCascada" || n == "Derrumbe")
                    return true;
                // Boyas rojas del aviso de abismo (marca visual que no frena a la balsa) y luciérnagas (luces que flotan).
                if (n == "BoyaRoja" || n.StartsWith("Luciernaga"))
                    return true;
                if (n == "Ground" || n == "Banks" || n == "Acera" || n.StartsWith("Water") || n == "Sky" || n == "Art"
                    || n == "Decor" || n == "Mangos" || n == "PowerUps" || n.StartsWith("Cloud") || n.StartsWith("Fog") || n.StartsWith("Rain") || n == "Backdrop")
                    return true;
            }
            return false;
        }

        private static bool IsSolidLayer(int layer) => layer == GameLayers.Environment || layer == GameLayers.Hazard;

        private static float SmallestDimension(Collider c)
        {
            var scale = c.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            switch (c)
            {
                case BoxCollider box:
                    return Mathf.Min(box.size.x * scale.x, Mathf.Min(box.size.y * scale.y, box.size.z * scale.z));
                case SphereCollider sphere:
                    return sphere.radius * 2f * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                case CapsuleCollider capsule:
                    return capsule.radius * 2f * Mathf.Max(scale.x, scale.z);
                default:
                    // MeshCollider: dimensión menor de su caja en el espacio del objeto (una pared fina plana en cualquier eje).
                    var size = c.bounds.size;
                    var mesh = (c as MeshCollider)?.sharedMesh;
                    if (mesh != null)
                    {
                        var local = mesh.bounds.size;
                        return Mathf.Min(local.x * scale.x, Mathf.Min(local.y * scale.y, local.z * scale.z));
                    }
                    return Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            }
        }

        private static List<Vector3> ChannelSamples(Scene scene, out float reach)
        {
            reach = 0f;
            var samples = new List<Vector3>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var flow = root.GetComponentInChildren<FlowSpline>(true);
                if (flow == null)
                    continue;
                var container = flow.GetComponent<SplineContainer>();
                float length = container.CalculateLength();
                bool openWater = root.GetComponentInChildren<CourseBounds>(true) != null || scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<CourseBounds>(true) != null);
                reach = openWater ? OPEN_WATER_REACH : flow.Width * 0.5f + CHANNEL_MARGIN;
                for (float d = 0f; d <= length; d += 1f)
                    samples.Add(container.EvaluatePosition(d / Mathf.Max(length, 1f)));
                break;
            }
            return samples;
        }

        private static bool WithinChannel(Vector3 point, List<Vector3> samples, float reach)
        {
            return DistanceToChannel(point, samples) <= reach;
        }

        private static float DistanceToChannel(Vector3 point, List<Vector3> samples)
        {
            float best = float.MaxValue;
            var flat = new Vector2(point.x, point.z);
            for (int i = 0; i < samples.Count; i++)
            {
                float d = (new Vector2(samples[i].x, samples[i].z) - flat).sqrMagnitude;
                if (d < best)
                    best = d;
            }
            return Mathf.Sqrt(best);
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null; p = p.parent)
                parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
