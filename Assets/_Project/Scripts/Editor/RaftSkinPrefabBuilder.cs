using System.IO;
using GasSafari.Raft;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Prefabs visuales de las 6 balsas de la tienda y de las mini balsas de Fiesta: el modelo más RaftWaterMask,
    // para que el agua nunca se dibuje dentro del bote (docs/09 §2). La física sigue siendo la de Raft.prefab:
    // la tienda solo cambia el modelo.
    public static class RaftSkinPrefabBuilder
    {
        private const string SKIN_MODELS = "Assets/_Project/Art/Props/Raft/Skins";
        private const string MINI_MODELS = "Assets/_Project/Art/Props/MiniRaft";
        private const string SKIN_PREFABS = "Assets/_Project/Prefabs/Raft/Skins";
        private const string MINI_PREFABS = "Assets/_Project/Prefabs/MiniRaft";

        [MenuItem("Gas Safari/Setup/Prefabs de balsas de la tienda y mini balsas con máscara de agua")]
        public static void BuildAll()
        {
            int count = Build(SKIN_MODELS, SKIN_PREFABS, "RaftSkin_") + Build(MINI_MODELS, MINI_PREFABS, "");
            AssetDatabase.SaveAssets();
            Debug.Log($"RAFTSKINS prefabs con máscara de agua: {count}");
        }

        private static int Build(string modelFolder, string prefabFolder, string prefix)
        {
            if (!AssetDatabase.IsValidFolder(modelFolder))
                return 0;
            Directory.CreateDirectory(prefabFolder);

            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { modelFolder }))
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                    continue;

                string prefabPath = $"{prefabFolder}/{prefix}{Path.GetFileNameWithoutExtension(modelPath)}.prefab";
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                try
                {
                    var mask = RaftWaterMask.Attach(instance);
                    if (mask == null)
                        continue;
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    count++;
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
            return count;
        }
    }
}
