using System.Collections.Generic;
using System.IO;
using System.Text;
using GasSafari.UI;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // M10-01: importa Tools/Export/Localization/strings.csv (columnas key, es, en) a la tabla única Resources/Strings.asset.
    // Reemplaza a Strings_es.asset conservando su GUID, y las viñetas pasan a leer sus pies de esta misma tabla.
    public static class StringsImporter
    {
        public const string TABLE_PATH = "Assets/_Project/Resources/Strings.asset";
        public const string CSV_PATH = "Tools/Export/Localization/strings.csv";
        private const string OLD_TABLE_PATH = "Assets/_Project/Data/UI/Strings_es.asset";
        private const string OLD_STORY_PATH = "Assets/_Project/Data/Story/Strings_story_es.asset";

        [MenuItem("Gas Safari/Setup/Importar textos (CSV)")]
        public static void Import()
        {
            string csvFull = Path.GetFullPath(CSV_PATH);
            if (!File.Exists(csvFull))
            {
                Debug.LogError("StringsImporter: no encuentro " + CSV_PATH);
                return;
            }

            // El asset viejo se mueve (no se copia): los prefabs y escenas que lo usan siguen apuntando a él.
            if (AssetDatabase.LoadAssetAtPath<UIStrings>(TABLE_PATH) == null && AssetDatabase.LoadAssetAtPath<UIStrings>(OLD_TABLE_PATH) != null)
            {
                string error = AssetDatabase.MoveAsset(OLD_TABLE_PATH, TABLE_PATH);
                if (!string.IsNullOrEmpty(error))
                    Debug.LogError("StringsImporter: " + error);
            }

            var table = AssetDatabase.LoadAssetAtPath<UIStrings>(TABLE_PATH);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<UIStrings>();
                AssetDatabase.CreateAsset(table, TABLE_PATH);
            }

            table.entries = Parse(File.ReadAllText(csvFull, Encoding.UTF8)).ToArray();
            EditorUtility.SetDirty(table);

            RepointStory(table);
            AssetDatabase.SaveAssets();
            Debug.Log("StringsImporter: " + table.entries.Length + " textos importados a " + TABLE_PATH);
        }

        public static List<UIStrings.Entry> Parse(string csv)
        {
            var entries = new List<UIStrings.Entry>();
            var seen = new HashSet<string>();
            var rows = StoryBuilder.ParseCsv(csv);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 3 || string.IsNullOrWhiteSpace(row[0]) || !seen.Add(row[0].Trim()))
                    continue;
                entries.Add(new UIStrings.Entry { key = row[0].Trim(), text = row[1], en = row[2] });
            }
            return entries;
        }

        // Las viñetas ya no tienen su propia tabla: leen la unificada, con las mismas claves story.<momento>.<n>.
        private static void RepointStory(UIStrings table)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:StoryVignetteData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<StoryVignetteData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null || data.strings == table)
                    continue;
                data.strings = table;
                EditorUtility.SetDirty(data);
            }
            if (AssetDatabase.LoadAssetAtPath<UIStrings>(OLD_STORY_PATH) != null)
                AssetDatabase.DeleteAsset(OLD_STORY_PATH);
        }
    }
}
