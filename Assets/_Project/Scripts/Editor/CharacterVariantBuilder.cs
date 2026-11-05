using System;
using System.Linq;
using GasSafari.Abilities;
using GasSafari.Audio;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // Prefabs finales de los 5 personajes (M2-13): el avatar provisional con el modelo de Blender, su Animator
    // (override del personaje), CharacterAnimator, sonidos y pasivas. Se puede repetir. Requiere haber corrido
    // "Importar personajes y props (M2-12)" y "Crear sonidos y SfxSet (M2-13)".
    public static class CharacterVariantBuilder
    {
        private const string FOLDER = "Assets/_Project/Prefabs/Characters/";
        private const string BASE = FOLDER + "Avatar_Placeholder.prefab";
        private const string DATA = "Assets/_Project/Data/Characters/";
        private const string ART = "Assets/_Project/Art/Characters/";
        private const string BALANCE = "Assets/_Project/Data/Balance/BalanceConfig.asset";
        // Radio de la cápsula del avatar provisional; la altura sale de CharacterData.height.
        private const float COLLIDER_RADIUS = 0.3f;
        // Altura del punto de sombrero sobre la cabeza, por metro de altura del animal.
        private const float HAT_RISE = 0.08f;

        [MenuItem("Gas Safari/Setup/Crear prefabs de personajes (M2-13)")]
        public static void Build()
        {
            Variant("Alex", typeof(AlexShowPassive));
            Variant("Marty");
            Variant("Melman", typeof(MelmanMedkitPassive), typeof(MelmanLookoutPassive));
            Variant("Gloria", typeof(GloriaHeavyPassive));
            Variant("Julien", typeof(JulienMagnetPassive), typeof(JulienLeap));
            AssetDatabase.SaveAssets();
        }

        private static void Variant(string name, params Type[] passives)
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA + name + ".asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BALANCE);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ART + name + "/" + name + ".fbx");

            var scene = EditorSceneManager.NewPreviewScene();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, scene);
            // Suelta el vínculo con el avatar provisional para poder quitarle los visuales de cápsula.
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "Avatar_" + name;
            SetReference(instance.GetComponent<PlayerAvatar>(), "character", data);
            foreach (var type in passives)
                SetReference(instance.AddComponent(type), "balance", balance);

            // Visuales provisionales fuera; "Butt" se queda como punto de salida del gas (sin malla).
            foreach (var child in instance.transform.Cast<Transform>().ToArray())
            {
                if (child.name == "Butt")
                {
                    Object.DestroyImmediate(child.GetComponent<MeshRenderer>());
                    Object.DestroyImmediate(child.GetComponent<MeshFilter>());
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, instance.transform);
            visual.name = "Model";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            var animator = visual.GetComponent<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = data.overrideController;
            animator.applyRootMotion = false;

            var hips = FindBone(visual.transform, "Hips");
            var head = FindBone(visual.transform, "Head");
            var hatSocket = new GameObject("HatSocket").transform;
            hatSocket.SetParent(instance.transform, false);
            hatSocket.position = head.position + Vector3.up * (HAT_RISE * data.height);
            hatSocket.SetParent(head, true);

            var capsule = instance.GetComponent<CapsuleCollider>();
            capsule.radius = COLLIDER_RADIUS;
            capsule.height = Mathf.Max(data.height, COLLIDER_RADIUS * 2f);
            capsule.center = new Vector3(0f, data.height * 0.5f, 0f);

            var characterAnimator = instance.AddComponent<CharacterAnimator>();
            SetReference(characterAnimator, "animator", animator);
            var juice = instance.GetComponent<VFX.FartJuice>();
            SetReference(juice, "hips", hips);
            SetReference(juice, "head", head);

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, FOLDER + instance.name + ".prefab");
            Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(scene);

            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }

        private static Transform FindBone(Transform root, string name)
        {
            var bone = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (bone == null)
                throw new InvalidOperationException($"El modelo {root.name} no tiene el hueso {name}");
            return bone;
        }

        private static void SetReference(Component component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"M2-13 {component.GetType().Name} no tiene el campo {field}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
