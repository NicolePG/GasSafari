using System.Collections.Generic;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace GasSafari.Editor
{
    // PV-11: aplica a las escenas ya construidas las distancias de checkpoints de W1LevelsBuilder (docs/04: tramos de 60-190 m)
    // sin reconstruir el nivel (el arte y los pases visuales no cambian). Coloca cada checkpoint sobre la spline del cauce,
    // como GreyboxLevel.AddCheckpoint, y rehace la lista de CheckpointSystem.
    public static class CheckpointPatch
    {
        private const string CHECKPOINT = "Assets/_Project/Prefabs/Level/Checkpoint.prefab";
        private const string SCENES = "Assets/_Project/Scenes/Juego/Niveles/";

        [MenuItem("Gas Safari/Dev/Aplicar checkpoints de W1 (PV-11)")]
        public static void ApplyW1()
        {
            foreach (var entry in W1Distances)
                Apply(entry.Key, entry.Value);
        }

        // Las mismas distancias que W1LevelsBuilder (los arcos suman radio x ángulo): si el builder cambia, cambiar aquí.
        private static readonly Dictionary<string, float[]> W1Distances = new Dictionary<string, float[]>
        {
            { "N01_W1-1_FuentesZoo", new[] { 0f, 150f, 300f, 470.95f, 650.95f, 755.95f } },
            { "N03_W1-3_RioCiudad", new[] { 0f, 140f, 300f, 471.89f, 606.89f } },
            { "N04_W1-4_MercadoFlotante", new[] { 0f, 190f, 330.27f, 480.27f, 603.27f } },
            { "N05_W1-5_PuertoJefe", new[] { 0f, 100f, 200f, 380f, 496.18f, 601.18f } },
        };

        public static void Apply(string scene, float[] distances)
        {
            var opened = EditorSceneManager.OpenScene(SCENES + scene + ".unity", OpenSceneMode.Single);
            var container = Object.FindFirstObjectByType<SplineContainer>();
            var system = Object.FindFirstObjectByType<CheckpointSystem>();
            var first = GameObject.Find("Checkpoint_0");
            if (container == null || system == null || first == null)
            {
                Debug.LogError("PV-11 " + scene + ": falta la spline, CheckpointSystem o Checkpoint_0");
                return;
            }
            var parent = first.transform.parent;
            float length = container.CalculateLength();
            // Control: la salida debe quedar donde la spline pone la distancia 0 (+0.5 m de altura, como el builder).
            var start = (Vector3)container.EvaluatePosition(0f) + Vector3.up * 0.5f;
            Debug.Log($"PV-11 {scene}: desvío de Checkpoint_0 {Vector3.Distance(start, first.transform.position):F2} m");

            // Fuera los anteriores (salvo la salida), en cualquier grupo.
            var old = new List<GameObject>();
            foreach (var root in opened.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Checkpoint_") && t.name != "Checkpoint_0" && t.GetComponent<Checkpoint>() != null)
                        old.Add(t.gameObject);
            if (old.Count > 0)
                parent = old[0].transform.parent;
            foreach (var go in old)
                Object.DestroyImmediate(go);

            var list = new List<Checkpoint> { first.GetComponent<Checkpoint>() };
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHECKPOINT);
            for (int i = 1; i < distances.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, opened);
                go.name = "Checkpoint_" + i;
                go.transform.SetParent(parent, false);
                float t = Mathf.Clamp01(distances[i] / length);
                Vector3 position = container.EvaluatePosition(t);
                Vector3 tangent = container.EvaluateTangent(t);
                tangent.y = 0f;
                go.transform.position = position + Vector3.up * 0.5f;
                go.transform.rotation = Quaternion.LookRotation(tangent.normalized);
                list.Add(go.GetComponent<Checkpoint>());
            }

            var so = new SerializedObject(system);
            var array = so.FindProperty("checkpoints");
            array.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(opened);
            EditorSceneManager.SaveScene(opened);
            Debug.Log($"PV-11 {scene}: {list.Count} checkpoints, cauce {length:F0} m");
        }
    }
}
