using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using GasSafari.Audio;
using GasSafari.Core;
using GasSafari.Hazards;
using GasSafari.Level;
using GasSafari.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace GasSafari.Editor
{
    // M5-01 T-0 Piscina del Zoológico (docs/04 T-0, docs/07 §8): obstáculos nuevos H33/H34/H35, las 10 lecciones,
    // el cartel y la toma del Súper Pedo, y el greybox del nivel. Se rehace entero al repetirlo.
    public static class T0LevelBuilder
    {
        private const string HZ_DATA = "Assets/_Project/Data/Hazards/";
        private const string HZ_PREFABS = "Assets/_Project/Prefabs/Hazards/";
        private const string HZ = HZ_PREFABS;
        private const string TUTORIAL = "Assets/_Project/Data/Tutorial/";
        private const string SOUNDS = "Assets/_Project/Data/Sounds/";
        private const float WIDTH = 10f;
        private static readonly Color BarrierColor = new Color(0.55f, 0.9f, 1f);
        private static readonly Color BalloonColor = new Color(1f, 0.45f, 0.7f);
        private static readonly Color BuoyColor = new Color(1f, 0.55f, 0.15f);
        private static readonly Color WallColor = new Color(0.75f, 0.72f, 0.65f);
        private static readonly Color RampColor = new Color(0.35f, 0.75f, 0.95f);
        private static readonly Color PlatformColor = new Color(0.55f, 0.4f, 0.25f);

        // ---------------------------------------------------------------- obstáculos (docs/05)
        [MenuItem("Gas Safari/Setup/Crear obstáculos H33, H34, H35 (M5-01)")]
        public static void BuildHazards()
        {
            EnsureFolder(HZ_DATA);
            var h33 = Config("H33_BalloonWall", "H33", "Pared de globos", 0.5f, ("impactToBreak", 30f), ("nearDistance", 6f));
            var h34 = Config("H34_HoseJet", "H34", "Chorro de manguera o fuente", 0.5f, ("accel", 6f), ("knockTime", 1.5f), ("width", 4f));
            var h35 = Config("H35_SinkingPlatform", "H35", "Plataforma que se hunde", 1f, ("sinkDelay", 1f), ("sinkTime", 6f));
            AssetDatabase.SaveAssets();

            Prefab("H33_BalloonWall", () =>
            {
                var root = new GameObject("H33_BalloonWall");
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(10f, 3f, 0.8f);
                box.center = new Vector3(0f, 1f, 0f);
                var material = Toon("M_H33_Balloon", BalloonColor);
                for (int i = 0; i < 18; i++)
                {
                    var balloon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.DestroyImmediate(balloon.GetComponent<Collider>());
                    balloon.name = "Balloon";
                    balloon.transform.SetParent(root.transform, false);
                    balloon.transform.localPosition = new Vector3(-4.5f + (i % 6) * 1.8f, 0.2f + (i / 6) * 0.9f, (i % 2) * 0.2f);
                    balloon.transform.localScale = new Vector3(1.1f, 1.25f, 1.1f);
                    balloon.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                root.AddComponent<BalloonWall>().Configure(h33);
                root.AddComponent<HazardTags>().Configure(HazardTag.Fragile);
                return root;
            });

            Prefab("H34_HoseJet", () =>
            {
                var root = new GameObject("H34_HoseJet");
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                // Tramo de 4 m a lo largo del cauce (z local) y todo el ancho (x local, hacia donde empuja).
                box.size = new Vector3(12f, 3f, h34.Get("width", 4f));
                box.center = new Vector3(0f, 1f, 0f);
                var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(nozzle.GetComponent<Collider>());
                nozzle.name = "Nozzle";
                nozzle.transform.SetParent(root.transform, false);
                nozzle.transform.localPosition = new Vector3(-6.2f, 1.2f, 0f);
                nozzle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                nozzle.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
                nozzle.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H34_Nozzle", new Color(0.2f, 0.6f, 0.3f));
                var stream = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(stream.GetComponent<Collider>());
                stream.name = "Stream";
                stream.transform.SetParent(root.transform, false);
                stream.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                stream.transform.localScale = new Vector3(11f, 0.25f, 0.5f);
                stream.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H34_Stream", new Color(0.7f, 0.9f, 1f));
                root.AddComponent<HoseJet>().Configure(h34);
                return root;
            });

            Prefab("H35_SinkingPlatform", () =>
            {
                var root = new GameObject("H35_SinkingPlatform");
                var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deck.name = "Deck";
                deck.transform.SetParent(root.transform, false);
                deck.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                deck.transform.localScale = new Vector3(8f, 0.6f, 8f);
                deck.GetComponent<MeshRenderer>().sharedMaterial = Toon("M_H35_Deck", PlatformColor);
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                root.AddComponent<SinkingPlatform>().Configure(h35);
                return root;
            });
            Debug.Log("M5-01 obstáculos H33, H34 y H35 listos");
        }

        // ---------------------------------------------------------------- lecciones (docs/04 T-0)
        private static TutorialStep[] BuildSteps()
        {
            EnsureFolder(TUTORIAL);
            var steps = new[]
            {
                Step(1, "Tírate un pedo", "Un toque mueve la balsa hacia el lado contrario.", TutorialAction.Fart, TutorialCondition.Puff, 0f),
                Step(2, "Muévete por el borde", "Rodea la balsa: el pedo empuja desde donde estás sentado.", TutorialAction.SeatMove, TutorialCondition.MoveSeat, 90f),
                Step(3, "Apunta para girar", "Apunta el pedo y rodea la boya por las tres marcas.", TutorialAction.Aim, TutorialCondition.RoundBuoy, 0f),
                Step(4, "Ráfaga", "Mantén el pedo para una ráfaga larga y cruza la piscina.", TutorialAction.Blast, TutorialCondition.Blast, 1f),
                Step(5, "¡Sincronía!", "Tírense un pedo a la vez para romper la pared de globos.", TutorialAction.Fart, TutorialCondition.Sync, 0f),
                Step(6, "Frijoles", "Recoge frijoles para recargar el gas.", TutorialAction.Fart, TutorialCondition.PickupBeans, 0f),
                Step(7, "Pings", "Marca \"¡Ahora!\" para avisar a tu equipo.", TutorialAction.Ping, TutorialCondition.PingNow, 0f),
                Step(8, "Agárrate", "Agárrate fuerte mientras el chorro empuja.", TutorialAction.Grab, TutorialCondition.GrabInJet, 3f),
                Step(9, "Pedo cargado", "Aguanta y suelta un pedo cargado para subir el tobogán.", TutorialAction.Charge, TutorialCondition.ChargedOnRamp, 1f),
                Step(10, "¡SÚPER PEDO!", "El medidor está lleno: ¡todos a la vez para saltar el muro!", TutorialAction.SuperFart, TutorialCondition.SuperFart, 0f),
            };
            steps[9].fillTeamMeter = true;
            EditorUtility.SetDirty(steps[9]);
            return steps;
        }

        private static TutorialStep Step(int n, string title, string text, TutorialAction action, TutorialCondition condition, float amount)
        {
            var step = LoadOrCreate<TutorialStep>($"{TUTORIAL}T0_L{n:00}.asset");
            step.textKey = $"tutorial.l{n:00}";
            step.title = title;
            step.text = text;
            step.action = action;
            step.condition = condition;
            step.amount = amount;
            step.fillTeamMeter = false;
            EditorUtility.SetDirty(step);
            return step;
        }

        // Sonido "checkpoint" de docs/08 (clips ya importados en Audio/SFX/Events).
        private static void EnsureCheckpointSound()
        {
            var def = LoadOrCreate<SoundDef>(SOUNDS + "Checkpoint.asset");
            var so = new SerializedObject(def);
            var clips = AssetDatabase.FindAssets("checkpoint_ t:AudioClip", new[] { "Assets/_Project/Audio/SFX/Events" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var clipsProp = so.FindProperty("clips");
            clipsProp.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.FindProperty("volume").floatValue = 1f;
            so.FindProperty("pitchMin").floatValue = 1f;
            so.FindProperty("pitchMax").floatValue = 1f;
            so.FindProperty("spatial").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SOUNDS + "SoundLibrary.asset");
            if (library != null && library.entries.All(e => e.id != SoundId.Checkpoint))
            {
                library.entries = library.entries.Append(new SoundLibrary.Entry { id = SoundId.Checkpoint, sound = def }).ToArray();
                EditorUtility.SetDirty(library);
            }
        }

        // ---------------------------------------------------------------- nivel
        [MenuItem("Gas Safari/Setup/Greybox T-0 Piscina del Zoológico (M5-01)")]
        public static void BuildLevel()
        {
            BuildHazards();
            var steps = BuildSteps();
            EnsureCheckpointSound();
            AssetDatabase.SaveAssets();

            var level = GreyboxLevel.Create("T-0", "L_T0_PiscinaZoo");
            // Piscina: tramos rectos con una zona ancha para la boya; tras el muro, el canal de la ciudad.
            level.StartPath(Vector3.zero, 0f).Straight(38f, WIDTH)   // L1-L2
                .Straight(30f, 18f)                                  // L3 boya (piscina ancha)
                .Straight(80f, WIDTH)                                // L4-L7
                .Straight(30f, 8f)                                   // L8 chorro
                .Straight(32f, WIDTH)                                // L9 tobogán
                .Straight(30f, 12f)                                  // L10 muro
                .Straight(50f, 12f);                                 // canal de la ciudad
            level.BuildChannel(0f); // docs/04 no pide corriente en la piscina

            var lessons = new List<TutorialLesson>();
            // (inicio de zona, fin de zona = barrera). Un checkpoint por lección, al empezarla.
            float[] starts = { 0f, 18f, 38f, 70f, 106f, 124f, 140f, 156f, 172f, 200f };
            float[] ends = { 16f, 36f, 66f, 104f, 122f, 138f, 154f, 172f, 176f, 213f };
            // La zona de la lección 9 cubre la rampa (el pedo cargado cuenta "en la rampa"); su barrera queda al pie.
            float[] zoneEnds = { 16f, 36f, 66f, 104f, 122f, 138f, 154f, 172f, 192f, 213f };
            for (int i = 0; i < 10; i++)
                level.AddCheckpoint(starts[i] + (i == 0 ? 0f : 1f));

            for (int i = 0; i < 10; i++)
            {
                bool hasBarrier = i != 4 && i != 9; // L5: la pared de globos es la barrera; L10: el muro del zoológico
                var barrier = hasBarrier ? Barrier(level, i + 1, ends[i]) : null;
                lessons.Add(Lesson(level, steps[i], starts[i], zoneEnds[i], barrier));
            }

            // Señalética de Blender: arco con tablón al empezar, un cartel grande por lección (con su título) y una boya de zona
            // en la orilla donde empieza cada una (al lado del checkpoint).
            float hangingWidth = WIDTH / 6.1f;
            TutorialModel(level, "Tutorial_HangingSign", level.PointAt(3f), Quaternion.LookRotation(level.DirectionAt(3f)).eulerAngles.y,
                new Vector3(hangingWidth, 1f, 1f));
            for (int i = 0; i < 10; i++)
            {
                float at = starts[i] + 4f;
                float half = level.WidthAt(at) * 0.5f;
                float yaw = Quaternion.LookRotation(level.DirectionAt(at)).eulerAngles.y;
                var sign = TutorialModel(level, "Tutorial_BigSign", level.PointAt(at, -(half + 3.5f)), yaw + 90f, Vector3.one * 0.7f);
                sign.name = $"Cartel_{i + 1:00}";
                var panel = sign.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Panel");
                if (panel != null)
                {
                    var text = new GameObject("Titulo").AddComponent<TMPro.TextMeshPro>();
                    text.transform.SetParent(panel, false);
                    text.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                    text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    text.rectTransform.sizeDelta = new Vector2(4.4f, 1.8f);
                    text.text = $"{i + 1}. {steps[i].title}";
                    text.fontSize = 3.2f;
                    text.alignment = TMPro.TextAlignmentOptions.Center;
                    text.color = new Color(0.15f, 0.12f, 0.3f);
                }
                TutorialModel(level, "Tutorial_LessonBuoy_Zone", level.PointAt(starts[i] + 1f, half - 0.9f), 0f, Vector3.one);
            }

            // L3: boya y 3 marcas en orden (derecha, detrás, izquierda): obliga a rodearla.
            float buoy = 50f;
            var buoyBlock = level.Cylinder("Boya", level.PointAt(buoy), 1.2f, 2.2f, BuoyColor);
            buoyBlock.GetComponent<MeshRenderer>().enabled = false; // collider; se ve la boya del modelo
            TutorialModel(level, "Tutorial_LessonBuoy_Turn", level.PointAt(buoy, 0f, -0.5f), 0f, Vector3.one);
            level.Mango(0, buoy, 0f, 2.3f); // ruta: sobre la boya
            var gates = new[]
            {
                Gate(level, "Marca_1", buoy, 5f),
                Gate(level, "Marca_2", buoy + 7f, 0f),
                Gate(level, "Marca_3", buoy, -5f),
            };
            lessons[2].Configure(steps[2], lessons[2].GetComponent<Collider>(), lessons[2].Barrier, gates, null);

            // L5: pared de globos (H33); mango detrás (sync).
            var wall = level.Place(HZ + "H33_BalloonWall.prefab", 120f);
            wall.transform.localScale = new Vector3(WIDTH / 10f, 1f, 1f);
            level.Mango(1, 126f, 0f, 1f);

            // L6: frijoles.
            level.PowerUp("P01", 128f, -2f);
            level.PowerUp("P01", 131f, 0f);
            level.PowerUp("P01", 134f, 2f);

            // L8: chorro de manguera (H34) en un tramo de 4 m que empuja hacia la derecha.
            var jet = level.Place(HZ + "H34_HoseJet.prefab", 164f).GetComponent<HoseJet>();
            lessons[7].Configure(steps[7], lessons[7].GetComponent<Collider>(), lessons[7].Barrier, null, jet);

            // L9: tobogán = rampa modular con agua inclinada y H27 al final; mango escondido detrás.
            Ramp(level, 178f, 190f, 2f);
            level.Place(HZ + "H27_Jet.prefab", 191f, 0f, 0f, 2f);
            level.Mango(2, 193f, -4f, 0.6f);
            level.Hint(Bots.BotHintType.Ramp, 176f, 0f, 4f);

            // L10: muro del zoológico (parte superior a 2.5 m del agua) a 14 m del inicio de la zona: el arco del Súper Pedo
            // mide unos 25 m y pasa sobre el muro cerca de su punto más alto. Después, caída al canal de la ciudad.
            float wallAt = 214f;
            var zooWall = level.Block("MuroZoo", level.PointAt(wallAt), new Vector3(30f, 3f, 1.2f), WallColor,
                Quaternion.LookRotation(level.DirectionAt(wallAt)).eulerAngles.y);
            zooWall.name = "MuroZoo";

            level.Section(1, "Toque", 0f);
            level.Section(2, "Borde", 18f);
            level.Section(3, "Boya", 38f);
            level.Section(4, "Rafaga", 70f);
            level.Section(5, "Sincronia", 106f);
            level.Section(6, "Frijoles", 124f);
            level.Section(7, "Pings", 140f);
            level.Section(8, "Agarrarse", 156f);
            level.Section(9, "Cargado", 172f);
            level.Section(10, "SuperPedo", 200f);

            BuildDirector(level, lessons.ToArray());
            level.Finish();
            Debug.Log($"M5-01 T-0 greybox listo: {level.Length:F0} m, 10 lecciones");
        }

        private const string TUTORIAL_ART = "Assets/_Project/Art/Tutorial/";

        // Modelo de Blender de Art/Tutorial colocado en la escena del nivel (sin collider: es señalética).
        private static GameObject TutorialModel(GreyboxLevel level, string fbx, Vector3 position, float yaw, Vector3 scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(TUTORIAL_ART + fbx + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, level.Scene);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            return go;
        }

        private static BubbleBarrier Barrier(GreyboxLevel level, int lesson, float at)
        {
            var go = level.Block($"Barrera_{lesson:00}", level.PointAt(at), new Vector3(level.WidthAt(at) + 1f, 2.5f, 0.6f), BarrierColor,
                Quaternion.LookRotation(level.DirectionAt(at)).eulerAngles.y);
            var barrier = go.AddComponent<BubbleBarrier>();
            // El bloque queda como collider invisible; lo que se ve son las burbujas del modelo (revientan al abrirse).
            go.GetComponent<MeshRenderer>().enabled = false;
            float width = level.WidthAt(at) + 1f;
            var model = TutorialModel(level, "Tutorial_BubbleBarrier", level.PointAt(at), Quaternion.LookRotation(level.DirectionAt(at)).eulerAngles.y,
                new Vector3(width / 8.4f, 1f, 1f));
            barrier.Configure(go.GetComponent<Collider>(), model.GetComponentsInChildren<Renderer>());
            return barrier;
        }

        private static TutorialLesson Lesson(GreyboxLevel level, TutorialStep step, float from, float to, BubbleBarrier barrier)
        {
            float mid = (from + to) * 0.5f;
            float width = Mathf.Max(level.WidthAt(from), level.WidthAt(to));
            var go = level.Block($"Leccion_{step.name}", level.PointAt(mid), new Vector3(width, 4f, to - from), Color.white,
                Quaternion.LookRotation(level.DirectionAt(mid)).eulerAngles.y, false, "Tutorial");
            go.layer = GameLayers.Trigger;
            var box = go.GetComponent<BoxCollider>();
            box.isTrigger = true;
            var lesson = go.AddComponent<TutorialLesson>();
            lesson.Configure(step, box, barrier, null, null);
            return lesson;
        }

        private static TutorialGate Gate(GreyboxLevel level, string name, float at, float lateral)
        {
            var go = level.Block(name, level.PointAt(at, lateral), new Vector3(3f, 3f, 3f), Color.yellow,
                Quaternion.LookRotation(level.DirectionAt(at)).eulerAngles.y, false, "Tutorial");
            go.layer = GameLayers.Trigger;
            go.GetComponent<BoxCollider>().isTrigger = true;
            // Marca visible flotando sobre el agua, por encima de la cabeza de cualquier animal (3.1 m) para no atravesarlos.
            string marker = "Tutorial_LessonBuoy_Marker" + name.Substring(name.Length - 1);
            TutorialModel(level, marker, level.PointAt(at, lateral) + Vector3.up * 3.2f, 0f, Vector3.one).name = name + "_Boya";
            return go.AddComponent<TutorialGate>();
        }

        // Agua inclinada (docs/02 §1): trigger en la capa Water que sigue la rampa y rampa sólida 0.6 m por debajo.
        private static void Ramp(GreyboxLevel level, float from, float to, float rise)
        {
            var a = level.PointAt(from);
            var b = level.PointAt(to) + Vector3.up * rise;
            var dir = (b - a).normalized;
            float length = Vector3.Distance(a, b);
            var rotation = Quaternion.LookRotation(dir);
            float width = level.WidthAt(from);

            var water = level.Block("AguaTobogan", (a + b) * 0.5f, new Vector3(width, 0.2f, length), RampColor, 0f, false, "Channel");
            water.layer = GameLayers.Water;
            water.transform.SetPositionAndRotation((a + b) * 0.5f - rotation * Vector3.up * 0.1f, rotation);
            water.GetComponent<BoxCollider>().isTrigger = true;

            var solid = level.Block("Tobogan", (a + b) * 0.5f, new Vector3(width, 0.4f, length), RampColor, 0f, true, "Channel");
            solid.transform.SetPositionAndRotation((a + b) * 0.5f - rotation * Vector3.up * 0.8f, rotation);

            // Las paredes del cauce miden 2 m: en la rampa se suben.
            for (int side = -1; side <= 1; side += 2)
            {
                var wall = level.Block("ParedTobogan", (a + b) * 0.5f, new Vector3(0.6f, 2.5f, length), WallColor, 0f, true, "Channel");
                var right = Vector3.Cross(Vector3.up, dir).normalized;
                wall.transform.SetPositionAndRotation((a + b) * 0.5f + right * side * (width * 0.5f + 0.3f) + Vector3.up * 1f, rotation);
            }
        }

        // Director, cartel (UGUI) y toma de Timeline de 2 s que sigue a la balsa por encima del muro.
        private static void BuildDirector(GreyboxLevel level, TutorialLesson[] lessons)
        {
            var root = new GameObject("TutorialDirector");
            SceneManager.MoveGameObjectToScene(root, level.Scene);
            var director = root.AddComponent<TutorialDirector>();

            var canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -150f);
            rect.sizeDelta = new Vector2(900f, 230f);
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.05f, 0.15f, 0.3f, 0.85f);
            var title = Text(panel.transform, "Title", 56, new Vector2(0f, -20f), new Vector2(860f, 70f), FontStyles.Bold);
            var body = Text(panel.transform, "Body", 34, new Vector2(0f, -95f), new Vector2(860f, 60f), FontStyles.Normal);
            var buttons = Text(panel.transform, "Buttons", 40, new Vector2(0f, -160f), new Vector2(860f, 55f), FontStyles.Bold);
            var card = canvasGo.AddComponent<TutorialCard>();
            card.Configure(rect, panel.GetComponent<CanvasGroup>(), title, body, buttons);

            // Toma: cámara virtual apagada que una pista de activación enciende 2 s.
            var shotGo = new GameObject("SuperFartShot");
            shotGo.transform.SetParent(root.transform, false);
            var vcam = shotGo.AddComponent<CinemachineVirtualCamera>();
            vcam.Priority = 100;
            var transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
            transposer.m_FollowOffset = new Vector3(9f, 5f, -6f);
            transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
            vcam.AddCinemachineComponent<CinemachineComposer>();
            shotGo.SetActive(false);

            var timeline = LoadOrCreateTimeline(TUTORIAL + "T0_SuperFartShot.playable");
            foreach (var track in timeline.GetOutputTracks().ToList())
                timeline.DeleteTrack(track);
            var activation = timeline.CreateTrack<ActivationTrack>(null, "Toma");
            var clip = activation.CreateDefaultClip();
            clip.start = 0;
            clip.duration = 2;
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            var playable = root.AddComponent<PlayableDirector>();
            playable.playableAsset = timeline;
            playable.playOnAwake = false;
            playable.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime; // la cámara lenta del Súper Pedo no la estira
            playable.SetGenericBinding(activation, shotGo);

            director.Configure(lessons, card, playable, vcam);
        }

        private static TextMeshProUGUI Text(Transform parent, string name, float size, Vector2 position, Vector2 box, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = box;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.richText = true;
            return text;
        }

        // ---------------------------------------------------------------- utilidades
        private static HazardConfig Config(string file, string id, string displayName, float warning, params (string key, float value)[] parameters)
        {
            var config = LoadOrCreate<HazardConfig>(HZ_DATA + file + ".asset");
            config.id = id;
            config.displayName = displayName;
            config.warningTime = warning;
            config.parameters = parameters.Select(p => new HazardConfig.Parameter { key = p.key, value = p.value }).ToArray();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void Prefab(string name, System.Func<GameObject> make)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = make();
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = GameLayers.Hazard;
            PrefabUtility.SaveAsPrefabAsset(root, HZ_PREFABS + name + ".prefab");
            Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
            HazardModelBuilder.ApplyIfMapped(name); // modelo de Blender (README de Tools/Export/Hazards)
        }

        private static Material Toon(string name, Color color)
        {
            string path = "Assets/_Project/Art/Materials/Hazards/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("GasSafari/SG_Toon"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static TimelineAsset LoadOrCreateTimeline(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
