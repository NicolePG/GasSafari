using System;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GasSafari.Editor
{
    // Variantes del avatar provisional, una por personaje con sus pasivas, hasta los prefabs finales (M2-13).
    public static class CharacterVariantBuilder
    {
        private const string FOLDER = "Assets/_Project/Prefabs/Characters/";
        private const string BASE = FOLDER + "Avatar_Placeholder.prefab";
        private const string DATA = "Assets/_Project/Data/Characters/";
        private const string BALANCE = "Assets/_Project/Data/Balance/BalanceConfig.asset";

        [MenuItem("Gas Safari/Setup/Crear variantes de personajes")]
        public static void Build()
        {
            Variant("Alex", typeof(AlexShowPassive));
            Variant("Marty");
            Variant("Melman", typeof(MelmanMedkitPassive), typeof(MelmanLookoutPassive));
            Variant("Gloria");
            Variant("Julien");
            AssetDatabase.SaveAssets();
        }

        private static void Variant(string name, params Type[] passives)
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA + name + ".asset");
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BALANCE);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE);

            var scene = EditorSceneManager.NewPreviewScene();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, scene);
            instance.name = "Avatar_" + name;
            SetReference(instance.GetComponent<PlayerAvatar>(), "character", data);
            foreach (var type in passives)
                SetReference(instance.AddComponent(type), "balance", balance);

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, FOLDER + instance.name + ".prefab");
            Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(scene);

            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }

        private static void SetReference(Component component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null)
                return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
