using GasSafari.Core;
using GasSafari.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GasSafari.Editor
{
    // Tomas de cámara de los momentos estrella de W2 (docs/04): una zona sobre la ruta que, al cruzarla la balsa,
    // lanza una toma de Timeline de 3 s que la sigue. Se ancla a un objeto del nivel y toma la dirección del cauce,
    // así sirve igual al regenerar el nivel (los constructores la llaman) y al corregir una escena ya hecha (menú).
    public static class W2StarShots
    {
        private const string SHOT_FOLDER = "Assets/_Project/Data/Levels/";
        private const string ZONE_NAME = "MomentoEstrella";

        private struct Shot
        {
            public string Scene;
            public string Id;
            public string Anchor;
            public Vector3 ZoneSize;            // x = ancho del carril, y = alto, z = largo a lo largo del cauce
            public Vector3 CameraOffset;        // en el espacio del cauce: x = derecha, y = arriba, z = delante
            public float Seconds;
            public bool SlowMotion;
            public bool MainPath;
        }

        private static readonly Shot[] Shots =
        {
            // W2-1: derrumbar la montaña de cajas abre el atajo; la zona está tras la montaña (solo se llega rompiéndola).
            new Shot { Scene = "L_W2_1_CajasDeriva", Id = "W2-1", Anchor = "MangoSpot_2", ZoneSize = new Vector3(16f, 6f, 10f),
                CameraOffset = new Vector3(-9f, 7f, -11f), Seconds = 3f, SlowMotion = true, MainPath = false },
            // W2-2: surfear la ola gigante hasta la playa del faro (cresta con el mango).
            new Shot { Scene = "L_W2_2_Tormenta", Id = "W2-2", Anchor = "MangoSpot_2", ZoneSize = new Vector3(20f, 6f, 16f),
                CameraOffset = new Vector3(11f, 6f, -12f), Seconds = 3f, SlowMotion = false, MainPath = true },
            // W2-3: Honda de la Selva alrededor del gran mástil.
            new Shot { Scene = "L_W2_3_CementerioBarcos", Id = "W2-3", Anchor = "GranMastil", ZoneSize = new Vector3(34f, 8f, 20f),
                CameraOffset = new Vector3(-12f, 9f, -10f), Seconds = 3f, SlowMotion = false, MainPath = true },
            // W2-4: escapar del gran remolino (el carril lo bordea).
            new Shot { Scene = "L_W2_4_GranRemolino", Id = "W2-4", Anchor = "Remolino_GranRemolino", ZoneSize = new Vector3(40f, 8f, 26f),
                CameraOffset = new Vector3(-6f, 13f, -12f), Seconds = 3f, SlowMotion = false, MainPath = true },
            // W2-5: el pasillo final con los tentáculos golpeando detrás; la cámara va delante, mirando hacia atrás.
            new Shot { Scene = "L_W2_5_ArrecifeJefe", Id = "W2-5", Anchor = "ZonaJefe_Fase3", ZoneSize = new Vector3(30f, 6f, 14f),
                CameraOffset = new Vector3(7f, 6f, 14f), Seconds = 3f, SlowMotion = false, MainPath = true },
        };

        [MenuItem("Gas Safari/Setup/Tomas de cámara de los momentos estrella de W2")]
        public static void ApplyAll()
        {
            foreach (var shot in Shots)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/Levels/{shot.Scene}.unity", OpenSceneMode.Single);
                Apply(scene, shot.Id);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"W2SHOTS tomas: {Shots.Length}");
        }

        // Pone (o rehace) la zona del momento estrella de un nivel de W2 en la escena dada.
        public static void Apply(Scene scene, string levelId)
        {
            foreach (var shot in Shots)
            {
                if (shot.Id == levelId)
                {
                    Build(scene, shot);
                    return;
                }
            }
        }

        private static void Build(Scene scene, Shot shot)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == ZONE_NAME)
                    Object.DestroyImmediate(root);
            }

            var anchor = FindByName(scene, shot.Anchor);
            if (anchor == null)
            {
                Debug.LogWarning($"W2SHOTS {shot.Id}: falta el ancla {shot.Anchor}");
                return;
            }

            var forward = FlowDirection(scene, anchor.position);
            var rotation = Quaternion.LookRotation(forward);
            var go = new GameObject(ZONE_NAME);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.layer = GameLayers.Trigger;
            go.transform.SetPositionAndRotation(new Vector3(anchor.position.x, anchor.position.y + 1f, anchor.position.z), rotation);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = shot.ZoneSize;

            // La cámara de la toma usa un desplazamiento en el mundo: se pasa del espacio del cauce al del mundo.
            var world = rotation * shot.CameraOffset;
            var director = ShotBuilder.Create(go.transform, "Toma", $"{SHOT_FOLDER}{shot.Id}_StarShot.playable", shot.Seconds, world, out var vcam);
            go.AddComponent<StarMomentZone>().Configure(director, vcam, shot.SlowMotion, shot.MainPath);
        }

        private static Transform FindByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                        return t;
                }
            }
            return null;
        }

        private static Vector3 FlowDirection(Scene scene, Vector3 position)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var flow = root.GetComponentInChildren<FlowSpline>(true);
                if (flow == null)
                    continue;
                flow.Bake();
                int hint = 0;
                flow.GetFlow(position, ref hint, out var direction, out _);
                direction.y = 0f;
                if (direction.sqrMagnitude > 1e-4f)
                    return direction.normalized;
            }
            return Vector3.forward;
        }
    }
}
