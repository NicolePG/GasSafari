using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GasSafari.Editor
{
    // M2-12: ajustes de importación de los FBX de personajes y props, materiales toon, CharacterBase.controller
    // y un Animator Override Controller por personaje (docs/09 §3-4, skill model-import). Se puede repetir:
    // cuando lleguen las animaciones que faltan, los overrides toman el clip propio en lugar del sustituto.
    public static class ModelImportBuilder
    {
        private const string CHARACTERS = "Assets/_Project/Art/Characters/";
        private const string SHARED = CHARACTERS + "_Shared/";
        private const string CONTROLLER = SHARED + "CharacterBase.controller";
        private const string DATA = "Assets/_Project/Data/Characters/";
        private const string TOON = "GasSafari/SG_Toon";

        private static readonly string[] Names = { "Alex", "Marty", "Melman", "Gloria", "Julien" };
        private static readonly string[] Props =
        {
            "Assets/_Project/Art/Props/PowerUps/P01_Beans.fbx",
            "Assets/_Project/Art/Props/PowerUps/P02_Locoto.fbx",
            "Assets/_Project/Art/Props/PowerUps/P03_Mango.fbx",
            "Assets/_Project/Art/Props/PowerUps/P04_CoconutShield.fbx",
            "Assets/_Project/Art/Props/PowerUps/P05_Soda.fbx",
            "Assets/_Project/Art/Props/PowerUps/P06_Onion.fbx",
            "Assets/_Project/Art/Props/PowerUps/P07_GoldenMango.fbx",
            "Assets/_Project/Art/Props/Raft/Raft.fbx"
        };

        // Clips de docs/03 (Animaciones) con su Loop. Los Fart_* se animan por código (FartJuice).
        private static readonly (string name, bool loop)[] Slots =
        {
            ("Idle_Sit", true), ("Shuffle_L", true), ("Shuffle_R", true), ("Fart_Puff", false), ("Fart_Blast", true),
            ("Charge", true), ("Fart_Mega", false), ("Cough", false), ("Ability", false), ("Grab", true), ("Hit", false),
            ("Fall", false), ("Swim", true), ("Climb", false), ("Celebrate", false), ("Disappointed", false),
            ("Victory", true), ("Select_Pose", true)
        };

        // Clip sustituto mientras falta el propio: el más parecido en movimiento y duración.
        private static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>
        {
            { "Shuffle_L", "Idle_Sit" }, { "Shuffle_R", "Idle_Sit" }, { "Fart_Puff", "Idle_Sit" }, { "Fart_Blast", "Idle_Sit" },
            { "Charge", "Idle_Sit" }, { "Fart_Mega", "Idle_Sit" }, { "Cough", "Idle_Sit" }, { "Ability", "Celebrate" },
            { "Grab", "Idle_Sit" }, { "Disappointed", "Hit" }, { "Victory", "Celebrate" }, { "Select_Pose", "Idle_Sit" }
        };

        private static readonly HashSet<string> LoopClips = new HashSet<string> { "Idle_Sit", "Swim" };

        [MenuItem("Gas Safari/Setup/Importar personajes y props (M2-12)")]
        public static void Build()
        {
            foreach (var name in Names)
                ConfigureCharacter(name);
            foreach (var path in Props)
                ConfigureProp(path);
            var controller = BuildController();
            foreach (var name in Names)
                BuildOverride(name, controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------- personajes
        private static void ConfigureCharacter(string name)
        {
            string path = CHARACTERS + name + "/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA + name + ".asset");

            // Escala: la altura medida a escala 1 debe dar CharacterData.height.
            float measured = MeasureHeight(path) / importer.globalScale;
            importer.globalScale = data.height / measured;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.optimizeGameObjects = false; // FartJuice y HatSocket necesitan los huesos
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            ExtractToonMaterials(path, CHARACTERS + name + "/Materials/");

            importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = clip.takeName.Contains("|") ? clip.takeName.Substring(clip.takeName.LastIndexOf('|') + 1) : clip.takeName;
                clip.loopTime = LoopClips.Contains(clip.name);
                clip.loopPose = clip.loopTime;
                // Sentado el animal no se desplaza: "Bake Into Pose" en rotación, altura y posición (skill model-import).
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"M2-12 {name}: escala {importer.globalScale:F3}, avatar {(avatar != null && avatar.isValid ? (avatar.isHuman ? "Humanoid válido" : "no humanoide") : "inválido")}");
        }

        private static float MeasureHeight(string path)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds.size.y;
        }

        // ---------------------------------------------------------------- props
        private static void ConfigureProp(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.generateSecondaryUV = false; // se mueven: sin lightmaps
            importer.SaveAndReimport();
            ExtractToonMaterials(path, Path.GetDirectoryName(path).Replace('\\', '/') + "/Materials/");
        }

        // ---------------------------------------------------------------- materiales
        // Extrae los materiales embebidos a una carpeta y los pasa a SG_Toon conservando el color.
        private static void ExtractToonMaterials(string modelPath, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder.TrimEnd('/')))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder.TrimEnd('/')).Replace('\\', '/'), Path.GetFileName(folder.TrimEnd('/')));

            var importer = AssetImporter.GetAtPath(modelPath);
            var embedded = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToArray();
            foreach (var material in embedded)
            {
                string error = AssetDatabase.ExtractAsset(material, folder + Sanitize(material.name) + ".mat");
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning($"M2-12 no se pudo extraer {material.name} de {modelPath}: {error}");
            }
            if (embedded.Length > 0)
                importer.SaveAndReimport();

            var shader = Shader.Find(TOON);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder.TrimEnd('/') }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader == shader)
                    continue;
                var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
                    : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                material.shader = shader;
                material.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(material);
            }
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        // ---------------------------------------------------------------- controlador base
        private static AnimatorController BuildController()
        {
            if (!AssetDatabase.IsValidFolder(SHARED.TrimEnd('/')))
                AssetDatabase.CreateFolder(CHARACTERS.TrimEnd('/'), "_Shared");
            if (!AssetDatabase.IsValidFolder(SHARED + "Slots"))
                AssetDatabase.CreateFolder(SHARED.TrimEnd('/'), "Slots");

            AssetDatabase.DeleteAsset(CONTROLLER);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("Seat", AnimatorControllerParameterType.Float);
            controller.AddParameter("Charge", AnimatorControllerParameterType.Float);
            controller.AddParameter("Blast", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Pose", AnimatorControllerParameterType.Int);
            foreach (var trigger in new[] { "Puff", "Mega", "Cough", "Ability", "Hit", "Celebrate" })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            var clips = Slots.ToDictionary(s => s.name, s => SlotClip(s.name, s.loop));
            var sm = controller.layers[0].stateMachine;
            sm.entryPosition = new Vector3(-200, 0);

            // Centro: sentado, con desplazamiento por el borde (Seat -1..1).
            var tree = new BlendTree { name = "Seat", blendType = BlendTreeType.Simple1D, blendParameter = "Seat", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(clips["Shuffle_L"], -1f);
            tree.AddChild(clips["Idle_Sit"], 0f);
            tree.AddChild(clips["Shuffle_R"], 1f);
            var seated = sm.AddState("Seated", new Vector3(0, 0));
            seated.motion = tree;
            sm.defaultState = seated;

            AnimatorState State(string name, string slot, int x, int y)
            {
                var s = sm.AddState(name, new Vector3(x, y));
                s.motion = clips[slot];
                return s;
            }

            // Estados que dependen del AvatarState (int State).
            var charge = State("Charge", "Charge", 300, -200);
            var grab = State("Grab", "Grab", 300, -140);
            var fall = State("Fall", "Fall", 300, -80);
            var swim = State("Swim", "Swim", 300, -20);
            var climb = State("Climb", "Climb", 300, 40);
            var blast = State("Fart_Blast", "Fart_Blast", 300, 120);
            foreach (var (s, value) in new[] { (charge, 1), (grab, 2), (fall, 4), (swim, 5), (climb, 6) })
            {
                var any = sm.AddAnyStateTransition(s);
                Configure(any, 0.1f);
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.Equals, value, "State");
                // Vuelta al centro: sentado (0), lanzando habilidad (3) o reapareciendo (7).
                foreach (var back in new[] { 0, 3, 7 })
                {
                    var t = s.AddTransition(seated);
                    Configure(t, 0.1f);
                    t.AddCondition(AnimatorConditionMode.Equals, back, "State");
                }
            }
            var toBlast = seated.AddTransition(blast);
            Configure(toBlast, 0.05f);
            toBlast.AddCondition(AnimatorConditionMode.If, 0, "Blast");
            var fromBlast = blast.AddTransition(seated);
            Configure(fromBlast, 0.05f);
            fromBlast.AddCondition(AnimatorConditionMode.IfNot, 0, "Blast");

            // Acciones de una vez (triggers): vuelven al centro al terminar.
            int y2 = 200;
            foreach (var (name, slot, trigger) in new[]
            {
                ("Fart_Puff", "Fart_Puff", "Puff"), ("Fart_Mega", "Fart_Mega", "Mega"), ("Cough", "Cough", "Cough"),
                ("Ability", "Ability", "Ability"), ("Hit", "Hit", "Hit"), ("Celebrate", "Celebrate", "Celebrate")
            })
            {
                var s = State(name, slot, 600, y2);
                y2 += 60;
                var any = sm.AddAnyStateTransition(s);
                Configure(any, 0.05f);
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0, trigger);
                var back = s.AddTransition(seated);
                back.hasExitTime = true;
                back.exitTime = 0.9f;
                back.hasFixedDuration = true;
                back.duration = 0.1f;
            }

            // Poses de la interfaz (int Pose): se mantienen mientras dure.
            int y3 = 200;
            foreach (var (name, value) in new[] { ("Disappointed", 1), ("Victory", 2), ("Select_Pose", 3) })
            {
                var s = State(name, name, 900, y3);
                y3 += 60;
                var any = sm.AddAnyStateTransition(s);
                Configure(any, 0.2f);
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.Equals, value, "Pose");
                var back = s.AddTransition(seated);
                Configure(back, 0.2f);
                back.AddCondition(AnimatorConditionMode.Equals, 0, "Pose");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void Configure(AnimatorStateTransition t, float duration)
        {
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = duration;
        }

        // Clip vacío que reservan los overrides; la clave del override es su nombre.
        private static AnimationClip SlotClip(string name, bool loop)
        {
            string path = SHARED + "Slots/Slot_" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        // ---------------------------------------------------------------- overrides
        private static void BuildOverride(string name, AnimatorController controller)
        {
            string folder = CHARACTERS + name + "/";
            string path = folder + name + "_Override.overrideController";
            var over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (over == null)
            {
                over = new AnimatorOverrideController(controller);
                AssetDatabase.CreateAsset(over, path);
            }
            over.runtimeAnimatorController = controller;

            var own = LoadClips(folder);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            over.GetOverrides(overrides);
            var notes = new List<string>();
            for (int i = 0; i < overrides.Count; i++)
            {
                string slot = overrides[i].Key.name.Replace("Slot_", "");
                AnimationClip clip;
                if (own.TryGetValue(slot, out clip))
                { }
                else if (Fallback.TryGetValue(slot, out var substitute) && own.TryGetValue(substitute, out clip))
                    notes.Add($"{slot}←{substitute}");
                else
                    notes.Add($"{slot}←(vacío)");
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);
            }
            over.ApplyOverrides(overrides);
            EditorUtility.SetDirty(over);

            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA + name + ".asset");
            data.overrideController = over;
            EditorUtility.SetDirty(data);
            Debug.Log($"M2-12 {name} override: sustitutos {string.Join(", ", notes)}");
        }

        // Clips por nombre del FBX del personaje y de su carpeta Anims/ (Mixamo u otros).
        private static Dictionary<string, AnimationClip> LoadClips(string folder)
        {
            var result = new Dictionary<string, AnimationClip>();
            var fbxs = AssetDatabase.FindAssets("t:Model", new[] { folder.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath);
            foreach (var fbx in fbxs)
            {
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>())
                {
                    if (clip.name.StartsWith("__preview"))
                        continue;
                    // Un clip de la carpeta Anims/ gana sobre el del modelo si se llaman igual.
                    if (!result.ContainsKey(clip.name) || fbx.Contains("/Anims/"))
                        result[clip.name] = clip;
                }
            }
            return result;
        }

        // ---------------------------------------------------------------- balsa
        [MenuItem("Gas Safari/Setup/Reemplazar visual de la balsa (M2-12)")]
        public static void ReplaceRaftVisual()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Raft/Raft.prefab";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Props/Raft/Raft.fbx");
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find("Model");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                // El cubo "Body" se queda solo como colisión (misma física que antes); el modelo es el visual.
                var body = root.transform.Find("Body");
                var bodyRenderer = body.GetComponent<MeshRenderer>();
                if (bodyRenderer != null)
                    Object.DestroyImmediate(bodyRenderer);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Model";
                // El modelo mide 0.6 m con la base en y=0; la línea de flotación queda a media altura.
                instance.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
