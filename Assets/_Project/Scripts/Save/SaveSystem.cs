using System;
using System.IO;
using UnityEngine;

namespace GasSafari.Save
{
    // JSON en persistentDataPath/save.json con escritura atómica y copia .bak (docs/10 §13).
    public sealed class SaveSystem
    {
        private const string FILE_NAME = "save.json";

        private readonly string path;

        public SaveSystem(string filePath)
        {
            path = filePath;
        }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FILE_NAME);

        public string FilePath => path;
        public string BackupPath => path + ".bak";
        private string TempPath => Path.ChangeExtension(path, ".tmp");

        // Si el JSON falla, el .bak; si tampoco, datos nuevos.
        public SaveData Load()
        {
            if (TryRead(path, out var data) || TryRead(BackupPath, out data))
                return SaveMigrator.Migrate(data);

            if (File.Exists(path) || File.Exists(BackupPath))
                Debug.LogWarning("Guardado ilegible: se empieza con datos nuevos (" + path + ")");
            return new SaveData();
        }

        // Archivo temporal → reemplazo atómico → copia .bak del guardado anterior.
        public void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(TempPath, json);
            if (File.Exists(path))
                File.Replace(TempPath, path, BackupPath);
            else
                File.Move(TempPath, path);
        }

        private static bool TryRead(string file, out SaveData data)
        {
            data = null;
            if (!File.Exists(file))
                return false;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(file));
                return data != null;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
