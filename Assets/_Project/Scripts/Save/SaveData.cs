using System;
using System.Collections.Generic;

namespace GasSafari.Save
{
    // Formato del guardado (docs/10 §13). JsonUtility no serializa diccionarios: los pares van en listas.
    [Serializable]
    public class SaveData
    {
        public const int CURRENT_VERSION = 1;

        [Serializable]
        public class LevelRecord
        {
            public string id;
            // Mejor tiempo en s; 0 = nunca terminado sin modificadores.
            public float best;
            public int stars;
            // 3 bits por nivel, uno por mango dorado.
            public int mangos;
        }

        [Serializable]
        public class EquippedRecord
        {
            public string player;
            public string hat;
            public string gas;
            public string celebration;
        }

        [Serializable]
        public class CosmeticsRecord
        {
            public List<string> owned = new List<string>();
            public List<EquippedRecord> equipped = new List<EquippedRecord>();
            public string raft = "raft_orange";
        }

        [Serializable]
        public class StatsRecord
        {
            public int farts, sync2, sync3, sync4, superFarts, falls, rescues, sinks, meters, playSeconds;
        }

        [Serializable]
        public class DailyRecord
        {
            public string lastDate;
            public int streak;
            public float best;
        }

        [Serializable]
        public class SettingsRecord
        {
            public float volMaster = 0.8f;
            public bool shake = true;
            public float textScale = 1f;
            public bool colorblind;
            public string difficulty = "Normal";
        }

        public int version = CURRENT_VERSION;
        public int frijolitos;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public CosmeticsRecord cosmetics = new CosmeticsRecord();
        public List<string> achievements = new List<string>();
        public StatsRecord stats = new StatsRecord();
        public DailyRecord daily = new DailyRecord();
        public SettingsRecord settings = new SettingsRecord();

        public LevelRecord FindLevel(string id)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].id == id)
                    return levels[i];
            }
            return null;
        }

        public LevelRecord GetOrAddLevel(string id)
        {
            var record = FindLevel(id);
            if (record != null)
                return record;
            record = new LevelRecord { id = id };
            levels.Add(record);
            return record;
        }
    }
}
