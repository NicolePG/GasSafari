using GasSafari.Bots;
using GasSafari.Core;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Crea Data/Balance/BotConfig.asset y lo asigna a la GameSession del prefab GameServices (M4-01). Repetible.
    public static class BotSetupBuilder
    {
        private const string CONFIG = "Assets/_Project/Data/Balance/BotConfig.asset";
        private const string SERVICES = "Assets/_Project/Prefabs/Services/Resources/GameServices.prefab";

        [MenuItem("Gas Safari/Setup/Crear BotConfig (M4-01)")]
        public static void Build()
        {
            var config = ProvisionalFartFeedbackBuilder.LoadOrCreate<BotConfig>(CONFIG);
            EditorUtility.SetDirty(config);

            var root = PrefabUtility.LoadPrefabContents(SERVICES);
            try
            {
                var session = root.GetComponentInChildren<GameSession>(true);
                var so = new SerializedObject(session);
                so.FindProperty("botConfig").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SERVICES);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
