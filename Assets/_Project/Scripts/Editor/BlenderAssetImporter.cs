using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GasSafari.Editor
{
    // M2-01 / M5-38: importa lo que sale de Blender (Tools/Export): clips de personaje, fauna (Generic + controller),
    // jefe grúa y escenario W1-W5 con materiales SG_Toon (los emisivos "E_*" llevan _EmissionColor HDR para el Bloom).
    // Los FBX ya están copiados en Art/. Se puede repetir sin efectos secundarios.
    public static class BlenderAssetImporter
    {
        private const string ART = "Assets/_Project/Art/";
        private const string TOON = "GasSafari/SG_Toon";
        private const float EmissionBoost = 2.5f; // emisivo = color base x 2.5 (HDR) para que supere el umbral del Bloom

        private static readonly string[] Names = { "Alex", "Marty", "Melman", "Gloria", "Julien" };
        private static readonly HashSet<string> HumanLoops = new HashSet<string> { "Shuffle_L", "Shuffle_R", "Grab", "Victory", "Select_Pose" };
        private static readonly HashSet<string> CreatureLoops = new HashSet<string> { "Idle", "Walk", "Swim", "Sleep", "Move" };

        // Sombra del toon por mundo (SG_Toon._ShadowColor), docs/09.
        private static readonly Dictionary<string, Color> ShadowByWorld = new Dictionary<string, Color>
        {
            { "W1", new Color(0.30f, 0.33f, 0.58f) }, // noche azulada
            { "W2", new Color(0.45f, 0.48f, 0.65f) }, // día soleado
            { "W3", new Color(0.55f, 0.36f, 0.45f) }, // atardecer tropical
            { "W4", new Color(0.28f, 0.24f, 0.46f) }, // cuevas
            { "W5", new Color(0.55f, 0.40f, 0.38f) }  // atardecer dorado
        };

        [MenuItem("Gas Safari/Setup/Importar Blender: clips, fauna, jefe y escenario (M2-01)")]
        public static void BuildAll()
        {
            ImportCharacterAnims();
            ImportCreatures("Fauna", ART + "Fauna/");
            ImportCreatures("Bosses", ART + "Bosses/");
            foreach (var world in new[] { "W1", "W2", "W3", "W4", "W5" })
                ImportEnvironment(world);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // Los overrides toman los clips propios en lugar de los sustitutos.
            ModelImportBuilder.Build();
        }

        // ---------------------------------------------------------------- clips de personajes
        public static void ImportCharacterAnims()
        {
            foreach (var name in Names)
            {
                string folder = ART + "Characters/" + name + "/";
                var avatar = AssetDatabase.LoadAllAssetsAtPath(folder + name + ".fbx").OfType<Avatar>().FirstOrDefault();
                if (avatar == null)
                {
                    Debug.LogError($"M2-01 {name}: el modelo no tiene Avatar, correr antes 'Importar personajes y props (M2-12)'");
                    continue;
                }
                int count = 0;
                foreach (var path in Directory.GetFiles(folder + "Anims", "*.fbx").Select(p => p.Replace('\\', '/')))
                {
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.animationType = ModelImporterAnimationType.Human;
                    // El FBX del clip no trae la capa raíz del modelo (Alex>Alex>Hips frente a Alex@X>Hips): copiar el avatar
                    // falla por jerarquía distinta. Los clips Humanoid se retargetean por músculos, basta con que cada
                    // FBX arme su propio avatar con el mismo esqueleto (el mapeo automático es el mismo).
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = true;
                    importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.SaveAndReimport();

                    string clipName = Path.GetFileNameWithoutExtension(path);
                    clipName = clipName.Substring(clipName.IndexOf('@') + 1);
                    importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    var clips = importer.defaultClipAnimations;
                    foreach (var clip in clips)
                    {
                        clip.name = clipName;
                        clip.loopTime = HumanLoops.Contains(clipName);
                        clip.loopPose = clip.loopTime;
                        // Sentado el animal no se desplaza (skill model-import).
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    count += clips.Length;
                    var own = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                    if (own == null || !own.isValid || !own.isHuman)
                        Debug.LogError($"M2-01 {Path.GetFileName(path)}: avatar no humanoide");
                }
                Debug.Log($"M2-01 {name}: {count} clips propios importados (cada FBX con su avatar Humanoid)");
            }
        }

        // ---------------------------------------------------------------- fauna y jefes (Generic)
        private static void ImportCreatures(string kind, string folder)
        {
            string toonFolder = folder + "Materials/";
            EnsureFolder(toonFolder.TrimEnd('/'));
            EnsureFolder(folder + "Controllers");
            foreach (var path in Directory.GetFiles(folder, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Low;
                importer.generateSecondaryUV = false; // se mueven: sin lightmaps
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                importer.optimizeGameObjects = false; // la grúa y las criaturas se animan también por código
                importer.SaveAndReimport();
                RemapToonMaterials(path, toonFolder, new Color(0.4f, 0.35f, 0.5f));

                string baseName = Path.GetFileNameWithoutExtension(path);
                importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.name = clip.takeName.Contains("|") ? clip.takeName.Substring(clip.takeName.LastIndexOf('|') + 1) : clip.takeName;
                    clip.loopTime = CreatureLoops.Contains(clip.name);
                    clip.loopPose = clip.loopTime;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                var controller = BuildCreatureController(folder + "Controllers/" + baseName + ".controller", path);
                Debug.Log($"M2-01 {kind} {baseName}: {Triangles(model)} tris, clips [{string.Join(", ", clips.Select(c => c.name))}], avatar {(avatar != null && avatar.isValid ? "válido" : "inválido")}, controller {(controller != null ? "ok" : "falló")}");
            }
        }

        // Estados = clips del FBX; un trigger con el nombre del clip dispara cada uno desde cualquier estado.
        // Los clips sin loop vuelven al estado base (Idle o Sleep) al terminar.
        private static AnimatorController BuildCreatureController(string controllerPath, string fbxPath)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview")).GroupBy(c => c.name).Select(g => g.First()).ToList();
            if (clips.Count == 0)
                return null;

            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = controller.layers[0].stateMachine;

            var states = new Dictionary<string, AnimatorState>();
            int i = 0;
            foreach (var clip in clips)
            {
                var state = sm.AddState(clip.name, new Vector3(300, i * 60));
                state.motion = clip;
                states[clip.name] = state;
                i++;
            }
            // Dormido si existe (cocodrilo), si no Idle, si no el primero.
            string baseName = states.ContainsKey("Sleep") ? "Sleep" : states.ContainsKey("Idle") ? "Idle" : clips[0].name;
            string rest = states.ContainsKey("Idle") ? "Idle" : baseName;
            sm.defaultState = states[baseName];

            foreach (var clip in clips)
            {
                controller.AddParameter(clip.name, AnimatorControllerParameterType.Trigger);
                var state = states[clip.name];
                var any = sm.AddAnyStateTransition(state);
                any.hasExitTime = false;
                any.hasFixedDuration = true;
                any.duration = 0.1f;
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0, clip.name);
                if (!clip.isLooping && clip.name != rest)
                {
                    var back = state.AddTransition(states[rest]);
                    back.hasExitTime = true;
                    back.exitTime = 0.95f;
                    back.hasFixedDuration = true;
                    back.duration = 0.1f;
                }
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---------------------------------------------------------------- obstáculos, tutorial y sombreros (M5-xx)
        // Copia los FBX de Tools/Export/<source> a Art/<target> (si cambiaron) y los importa estáticos con materiales SG_Toon.
        // Devuelve las rutas de los FBX. 'shadow' es el color de sombra del toon.
        public static string[] ImportStaticModels(string source, string target, Color shadow)
        {
            string toonFolder = ART + target + "/Materials";
            EnsureFolder(toonFolder);
            var paths = new List<string>();
            foreach (var file in Directory.GetFiles("Tools/Export/" + source, "*.fbx"))
            {
                string path = ART + target + "/" + Path.GetFileName(file);
                var from = new FileInfo(file);
                var to = new FileInfo(path);
                if (!to.Exists || to.Length != from.Length || to.LastWriteTimeUtc < from.LastWriteTimeUtc)
                    File.Copy(file, path, true);
                paths.Add(path);
            }
            AssetDatabase.Refresh();
            foreach (var path in paths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Low;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.optimizeGameObjects = false; // las partes móviles se buscan por nombre
                importer.generateSecondaryUV = false;
                importer.SaveAndReimport();
                RemapToonMaterials(path, toonFolder + "/", shadow);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (Triangles(model) > 3000)
                    Debug.LogWarning($"{Path.GetFileName(path)} pasa el presupuesto de 3000 tris: {Triangles(model)}");
            }
            AssetDatabase.SaveAssets();
            return paths.ToArray();
        }

        [MenuItem("Gas Safari/Setup/Importar obstáculos, tutorial y sombreros de Blender")]
        public static void ImportHazardTutorialHats()
        {
            var hazards = ImportStaticModels("Hazards", "Hazards", new Color(0.4f, 0.38f, 0.55f));
            var tutorial = ImportStaticModels("Tutorial", "Tutorial", new Color(0.4f, 0.38f, 0.55f));
            var hats = ImportStaticModels("Cosmetics/Hats", "Cosmetics/Hats", new Color(0.4f, 0.38f, 0.55f));
            var anchors = "Tools/Export/Cosmetics/Hats/hat_anchors.json";
            if (File.Exists(anchors))
                File.Copy(anchors, ART + "Cosmetics/Hats/hat_anchors.json", true);
            AssetDatabase.Refresh();
            Debug.Log($"BLENDER obstáculos {hazards.Length}, tutorial {tutorial.Length}, sombreros {hats.Length} importados");
        }

        // ---------------------------------------------------------------- escenario
        private static void ImportEnvironment(string world)
        {
            string folder = ART + "Environment/" + world + "/";
            string toonFolder = folder + "Materials/";
            EnsureFolder(toonFolder.TrimEnd('/'));
            int models = 0, emissive = 0, overBudget = 0;
            foreach (var path in Directory.GetFiles(folder, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Low;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.generateSecondaryUV = true; // los estáticos usan lightmaps
                importer.SaveAndReimport();
                RemapToonMaterials(path, toonFolder, ShadowByWorld[world]);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (Triangles(model) > 3000)
                {
                    overBudget++;
                    Debug.LogWarning($"M5-38 {Path.GetFileName(path)} pasa el presupuesto de 3000 tris: {Triangles(model)}");
                }
                models++;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { toonFolder.TrimEnd('/') }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.GetColor("_EmissionColor").maxColorComponent > 0.01f)
                    emissive++;
            }
            Debug.Log($"M5-38 {world}: {models} modelos, {AssetDatabase.FindAssets("t:Material", new[] { toonFolder.TrimEnd('/') }).Length} materiales SG_Toon ({emissive} emisivos), {overBudget} fuera de presupuesto");
        }

        // ---------------------------------------------------------------- materiales
        // Un material SG_Toon por nombre en la carpeta del grupo; cada FBX apunta a ellos con un remap
        // (así 40 props del mismo mundo comparten materiales y el static batching los junta).
        internal static void RemapToonMaterials(string modelPath, string folder, Color shadow)
        {
            var importer = AssetImporter.GetAtPath(modelPath);
            var shader = Shader.Find(TOON);
            var embedded = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToArray();
            bool changed = false;
            foreach (var source in embedded)
            {
                string target = folder + Sanitize(source.name) + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(target);
                if (material == null)
                {
                    var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor")
                        : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                    material = new Material(shader) { name = Sanitize(source.name) };
                    material.SetColor("_BaseColor", color);
                    material.SetColor("_ShadowColor", shadow);
                    if (source.name.StartsWith("E_"))
                    {
                        // Principled emission: si el FBX trae color emisivo se respeta; si no, color base x 2.5.
                        var emission = source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black;
                        if (emission.maxColorComponent < 0.01f)
                            emission = new Color(color.r * EmissionBoost, color.g * EmissionBoost, color.b * EmissionBoost, 1f);
                        material.SetColor("_EmissionColor", emission);
                        material.SetFloat("_RimStrength", 0f);
                    }
                    AssetDatabase.CreateAsset(material, target);
                }
                var id = new AssetImporter.SourceAssetIdentifier(source);
                if (!importer.GetExternalObjectMap().TryGetValue(id, out var current) || current != material)
                {
                    importer.AddRemap(id, material);
                    changed = true;
                }
            }
            if (changed)
                importer.SaveAndReimport();
        }

        internal static int Triangles(GameObject model)
        {
            int count = 0;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null)
                    for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
                        count += (int)filter.sharedMesh.GetIndexCount(i) / 3;
            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (skinned.sharedMesh != null)
                    for (int i = 0; i < skinned.sharedMesh.subMeshCount; i++)
                        count += (int)skinned.sharedMesh.GetIndexCount(i) / 3;
            return count;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
