using System.IO;
using UnityEngine;

namespace GasSafari.Save
{
    // Un archivo binario por nivel junto al guardado (persistentDataPath/ghosts/<id>.ghost), con escritura atómica como
    // SaveSystem. Binario porque 10 Hz durante 5 min son 3000 poses (unos 84 KB; en JSON serían 5 veces más).
    public static class GhostStore
    {
        private const int MAGIC = 0x47534754; // "GSGT"
        private const int VERSION = 1;

        private static string folder;

        // ProgressionService lo apunta junto al guardado (las pruebas usan una carpeta temporal).
        public static string Folder
        {
            get => folder ?? (folder = Path.Combine(Application.persistentDataPath, "ghosts"));
            set => folder = value;
        }

        public static string PathFor(string levelId) => Path.Combine(Folder, Safe(levelId) + ".ghost");

        private static string Safe(string id)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                id = id.Replace(c, '_');
            return id;
        }

        public static void Save(GhostData ghost)
        {
            if (ghost == null || string.IsNullOrEmpty(ghost.LevelId))
                return;
            string path = PathFor(ghost.LevelId);
            string temp = path + ".tmp";
            Directory.CreateDirectory(Folder);
            using (var writer = new BinaryWriter(File.Create(temp)))
            {
                writer.Write(MAGIC);
                writer.Write(VERSION);
                writer.Write(ghost.LevelId);
                writer.Write(ghost.Time);
                writer.Write(ghost.SampleRate);
                writer.Write(ghost.Characters.Count);
                foreach (var character in ghost.Characters)
                    writer.Write(character ?? string.Empty);
                writer.Write(ghost.Splits.Length);
                foreach (float split in ghost.Splits)
                    writer.Write(split);
                writer.Write(ghost.Count);
                for (int i = 0; i < ghost.Count; i++)
                {
                    var p = ghost.Positions[i];
                    var r = ghost.Rotations[i];
                    writer.Write(p.x); writer.Write(p.y); writer.Write(p.z);
                    writer.Write(r.x); writer.Write(r.y); writer.Write(r.z); writer.Write(r.w);
                }
            }
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        // null si no hay fantasma o el archivo está dañado.
        public static GhostData Load(string levelId)
        {
            string path = PathFor(levelId);
            if (!File.Exists(path))
                return null;
            try
            {
                using (var reader = new BinaryReader(File.OpenRead(path)))
                {
                    if (reader.ReadInt32() != MAGIC || reader.ReadInt32() != VERSION)
                        return null;
                    var ghost = new GhostData { LevelId = reader.ReadString(), Time = reader.ReadSingle(), SampleRate = reader.ReadSingle() };
                    int characters = reader.ReadInt32();
                    for (int i = 0; i < characters; i++)
                        ghost.Characters.Add(reader.ReadString());
                    ghost.Splits = new float[reader.ReadInt32()];
                    for (int i = 0; i < ghost.Splits.Length; i++)
                        ghost.Splits[i] = reader.ReadSingle();
                    ghost.Count = reader.ReadInt32();
                    ghost.Positions = new Vector3[ghost.Count];
                    ghost.Rotations = new Quaternion[ghost.Count];
                    for (int i = 0; i < ghost.Count; i++)
                    {
                        ghost.Positions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        ghost.Rotations[i] = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    }
                    return ghost.LevelId == levelId && ghost.SampleRate > 0f ? ghost : null;
                }
            }
            catch (IOException)
            {
                Debug.LogWarning("Fantasma ilegible: " + path);
                return null;
            }
        }

        public static void Delete(string levelId)
        {
            string path = PathFor(levelId);
            if (File.Exists(path))
                File.Delete(path);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => folder = null;
    }
}
