using System.Linq;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace GasSafari.Editor
{
    // Auditoría del pulido visual (Dirección visual de level-builder): un renglón de log por nivel con el recorrido,
    // el plano de agua, la niebla y el cielo. Solo lee: abre cada escena sin guardarla.
    public static class LevelVisualAudit
    {
        // Deja abierta una escena vacía sin guardar: las pruebas que cargan niveles en aditivo no chocan con la escena abierta.
        [MenuItem("Gas Safari/Dev/Abrir escena vacía")]
        public static void OpenEmpty()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // Renderers sin material (se ven magenta) en todas las escenas de nivel; con 'fix', les asigna el material Greybox
        // M_<ID>_<nombre> si existe (los bloques de greybox se guardan así), si no el material del muro del mundo.
        [MenuItem("Gas Safari/Dev/Auditar materiales vacíos")]
        public static void AuditNullMaterials() => NullMaterials(false);

        [MenuItem("Gas Safari/Dev/Reparar materiales vacíos")]
        public static void FixNullMaterials() => NullMaterials(true);

        private static void NullMaterials(bool fix)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes/Juego/Niveles" }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var names = new System.Collections.Generic.Dictionary<string, int>();
                int fixedCount = 0;
                var id = System.Text.RegularExpressions.Regex.Match(scene.name, @"N\d\d_(T0|W\d-\d)_").Groups[1].Value.Replace("T0", "T-0");
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || !r.enabled || !r.gameObject.activeInHierarchy)
                            continue;
                        if (!r.sharedMaterials.Any(m => m == null))
                            continue;
                        string key = r.gameObject.name == "Model" && r.transform.parent != null ? r.transform.parent.name + "/Model" : r.gameObject.name;
                        names[key] = names.TryGetValue(key, out int c) ? c + 1 : 1;
                        if (!fix)
                            continue;
                        var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Art/Materials/Greybox/M_{id}_{r.gameObject.name}.mat");
                        if (material == null)
                            continue;
                        var list = r.sharedMaterials;
                        for (int i = 0; i < list.Length; i++)
                            if (list[i] == null) list[i] = material;
                        r.sharedMaterials = list;
                        EditorUtility.SetDirty(r);
                        fixedCount++;
                    }
                }
                if (names.Count > 0)
                    Debug.Log($"NULLMAT {scene.name}: {string.Join(", ", names.Select(k => k.Key + "x" + k.Value))}{(fix ? $" | reparados {fixedCount}" : "")}");
                if (fix && fixedCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            Debug.Log("NULLMAT terminado");
        }

        [MenuItem("Gas Safari/Dev/Auditoría visual de niveles")]
        public static void Run()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes/Juego/Niveles" }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var flow = Object.FindFirstObjectByType<FlowSpline>();
                string route = "sin Flow";
                if (flow != null)
                {
                    var container = flow.GetComponent<SplineContainer>();
                    float length = container.CalculateLength();
                    var min = Vector3.one * 1e9f;
                    var max = Vector3.one * -1e9f;
                    for (int i = 0; i <= 40; i++)
                    {
                        Vector3 p = container.EvaluatePosition(i / 40f);
                        min = Vector3.Min(min, p);
                        max = Vector3.Max(max, p);
                    }
                    route = $"len={length:F0} x[{min.x:F0},{max.x:F0}] y[{min.y:F1},{max.y:F1}] z[{min.z:F0},{max.z:F0}] w={flow.Width:F0}";
                }
                var water = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Water");
                string waterInfo = "sin Water";
                if (water != null)
                {
                    var r = water.GetComponent<Renderer>();
                    waterInfo = $"agua mat={(r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "-")} en={(r != null && r.enabled)} esc=({water.transform.localScale.x * 10f:F0}x{water.transform.localScale.z * 10f:F0}) y={water.transform.position.y:F1}";
                }
                string sky = RenderSettings.skybox != null ? RenderSettings.skybox.name + "/" + RenderSettings.skybox.shader.name : "ninguno";
                int art = 0;
                var artRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Art");
                if (artRoot != null) art = artRoot.GetComponentsInChildren<Renderer>(true).Length;
                Debug.Log($"AUDIT {scene.name} | {route} | {waterInfo} | niebla={RenderSettings.fog} {RenderSettings.fogStartDistance:F0}-{RenderSettings.fogEndDistance:F0} | cielo={sky} | renderersArt={art} | raices={scene.rootCount}");
            }
        }
    }
}
