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
        public class CharacterUse
        {
            public string id;
            public int count;
        }

        [Serializable]
        public class StatsRecord
        {
            public int farts, sync2, sync3, sync4, superFarts, falls, rescues, sinks, meters, playSeconds;
            // Niveles terminados con cada personaje (para "personaje más usado").
            public List<CharacterUse> characters = new List<CharacterUse>();
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
            public float volMusic = 0.8f;
            public float volSfx = 0.8f;
            public float volVoices = 0.8f;
            public float volUi = 0.8f;
            // Video: -1 = la resolución actual del escritorio.
            public int resolutionIndex = -1;
            public bool fullscreen = true;
            public bool vsync = true;
            // 0 Baja, 1 Media, 2 Alta.
            public int quality = 2;
            // 0 = sin límite.
            public int fpsLimit = 60;
            public bool vibration = true;
            public float aimSensitivity = 1f;
            public bool invertAim;
            // Remapeo (Input System SaveBindingOverridesAsJson).
            public string bindingOverrides = string.Empty;
            public bool shake = true;
            public bool slowmo = true;
            public float textScale = 1f;
            public bool colorblind;
            public bool highContrastPings;
            // Aguantar y Agarrarse: falso = mantener pulsado, verdadero = pulsar para alternar.
            public bool toggleHold;
            public string difficulty = "Normal";
            public bool tips = true;
            public string language = "es";
        }

        public int version = CURRENT_VERSION;
        public int frijolitos;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public CosmeticsRecord cosmetics = new CosmeticsRecord();
        public List<string> achievements = new List<string>();
        // Niveles terminados en la dificultad Amistades Rotas (logro 22), minijuegos de Fiesta jugados (logro 23)
        // y mejor distancia en Infinito en metros (logro 24).
        public List<string> brokenLevels = new List<string>();
        public List<string> partyPlayed = new List<string>();
        public int endlessMeters;
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
