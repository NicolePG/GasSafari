using GasSafari.Bots;
using GasSafari.Core;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Crea Data/Balance/BotConfig.asset, lo asigna a la GameSession del prefab GameServices y fija la probabilidad
    // de sincronía de los bots por dificultad (M4-01, M4-02). Repetible.
    public static class BotSetupBuilder
    {
        private const string CONFIG = "Assets/_Project/Data/Balance/BotConfig.asset";
        private const string SERVICES = "Assets/_Project/Prefabs/Services/Resources/GameServices.prefab";

        [MenuItem("Gas Safari/Setup/Crear BotConfig y dificultad de bots (M4)")]
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

            // Probabilidad de que un bot responda a un pedo humano (docs/10 §15).
            SetSyncChance("Familiar", 0.9f);
            SetSyncChance("Normal", 0.75f);
            SetSyncChance("AmistadesRotas", 0.6f);
            AssetDatabase.SaveAssets();
        }

        private static void SetSyncChance(string name, float chance)
        {
            var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyData>("Assets/_Project/Data/Difficulty/Difficulty_" + name + ".asset");
            difficulty.botSyncChance = chance;
            EditorUtility.SetDirty(difficulty);
        }
    }
}
