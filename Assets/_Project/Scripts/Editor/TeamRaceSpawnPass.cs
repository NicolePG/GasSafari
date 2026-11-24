using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // M7-12: coloca las dos TeamRaceSpawn (docs/06 §1, docs/10 §5) de un nivel compatible con Carrera por Equipos, a los lados
    // de la puerta de salida, centradas en el cauce libre que miden rayos contra las paredes (capa Environment) de esa escena.
    // GreyboxLevel.Finish lo llama al construir un nivel; el menú lo aplica a las escenas ya construidas sin reconstruirlas.
    public static class TeamRaceSpawnPass
    {
        private const string SCENES = "Assets/_Project/Scenes/Juego/Niveles/";
        private const string LEVEL_DATA = "Assets/_Project/Data/Levels/";
        private const string ROOT_NAME = "TeamRace";
        // Hasta dónde se buscan las paredes y altura del rayo sobre la salida (m).
        private const float PROBE_DISTANCE = 30f;
        private const float PROBE_HEIGHT = 1f;
        // Separación lateral de cada balsa desde el centro del cauce libre (m): media balsa más 0.4 m de aire como mínimo
        // (radio de 2 m en BalanceConfig) y 4.5 m como máximo, para que no arranquen pegadas a la pared en cauces anchos.
        private const float MAX_LATERAL = 4.5f;
        private const float AIR = 0.4f;

        [MenuItem("Gas Safari/Setup/Colocar salidas de Carrera por Equipos (M7-12)")]
        public static void ApplyAll()
        {
            foreach (var id in LevelDataBuilder.RaceTracks)
            {
                var data = AssetDatabase.LoadAssetAtPath<LevelData>(LEVEL_DATA + id + ".asset");
                if (data == null || string.IsNullOrEmpty(data.sceneName))
                {
                    Debug.LogError("M7-12 " + id + ": falta el LevelData o su escena");
                    continue;
                }
                string path = SCENES + data.sceneName + ".unity";
                var scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.isLoaded;
                if (!wasOpen)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                Apply(scene, data);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (!wasOpen)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Quita las salidas anteriores y, si el nivel es compatible, pone las dos nuevas. Devuelve cuántas puso.
        public static int Apply(Scene scene, LevelData data)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var old in root.GetComponentsInChildren<TeamRaceSpawn>(true))
                {
                    var parent = old.transform.parent;
                    Object.DestroyImmediate(old.gameObject);
                    if (parent != null && parent.name == ROOT_NAME && parent.childCount == 0)
                        Object.DestroyImmediate(parent.gameObject);
                }
            }
            if (data == null || !data.raceCompatible)
                return 0;

            StartGate gate = null;
            Transform generated = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (gate == null)
                    gate = root.GetComponentInChildren<StartGate>(true);
                if (root.name == GreyboxLevel.GENERATED_ROOT)
                    generated = root.transform;
            }
            if (gate == null)
            {
                Debug.LogError("M7-12 " + data.id + ": la escena no tiene StartGate");
                return 0;
            }

            var origin = gate.Position + Vector3.up * PROBE_HEIGHT;
            var right = gate.Rotation * Vector3.right;
            right.y = 0f;
            right.Normalize();
            var walls = Walls(scene);
            Physics.SyncTransforms();
            float left = Probe(walls, origin, -right);
            float rightFree = Probe(walls, origin, right);
            float center = (rightFree - left) * 0.5f;
            float half = (left + rightFree) * 0.5f;
            float radius = RaftRadius();
            float lateral = Mathf.Clamp(half * 0.5f, radius + AIR, MAX_LATERAL);
            if (lateral + radius > half)
                Debug.LogWarning($"M7-12 {data.id}: cauce de {left + rightFree:F1} m en la salida, las balsas quedan a menos de {AIR} m de la pared");

            var holder = new GameObject(ROOT_NAME);
            SceneManager.MoveGameObjectToScene(holder, scene);
            if (generated != null)
                holder.transform.SetParent(generated, false);
            for (int team = 0; team < TeamRaceSpawn.TEAMS; team++)
            {
                // Azul a la izquierda (mitad izquierda de la pantalla dividida), Naranja a la derecha.
                float side = team == 0 ? -1f : 1f;
                var go = new GameObject("TeamRaceSpawn_" + team);
                go.transform.SetParent(holder.transform, false);
                go.transform.SetPositionAndRotation(gate.Position + right * (center + side * lateral), gate.Rotation);
                go.AddComponent<TeamRaceSpawn>().Configure(team);
            }
            Debug.Log($"M7-12 {data.id}: salidas a ±{lateral:F1} m del centro del cauce (libre {left:F1} m a la izquierda y {rightFree:F1} m a la derecha)");
            return TeamRaceSpawn.TEAMS;
        }

        private static List<Collider> Walls(Scene scene)
        {
            int environment = LayerMask.NameToLayer("Environment");
            var list = new List<Collider>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var collider in root.GetComponentsInChildren<Collider>(false))
                {
                    if (collider.enabled && !collider.isTrigger && collider.gameObject.layer == environment)
                        list.Add(collider);
                }
            }
            return list;
        }

        // Distancia libre hasta la pared más cercana en esa dirección (solo colisionadores de la escena del nivel).
        private static float Probe(List<Collider> walls, Vector3 origin, Vector3 direction)
        {
            float best = PROBE_DISTANCE;
            var ray = new Ray(origin, direction);
            foreach (var wall in walls)
            {
                if (wall.Raycast(ray, out var hit, best))
                    best = hit.distance;
            }
            return best;
        }

        private static float RaftRadius()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/_Project/Data/Balance/BalanceConfig.asset");
            return balance != null ? balance.raftRadius : 2f;
        }
    }
}
