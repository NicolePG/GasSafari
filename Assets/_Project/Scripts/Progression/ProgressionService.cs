using System;
using GasSafari.Level;
using GasSafari.Save;
using UnityEngine;

namespace GasSafari.Progression
{
    // Estrellas, mangos, frijolitos y desbloqueos (docs/06 §2). Guarda en los momentos clave, no cada frame.
    public class ProgressionService : MonoBehaviour
    {
        private SaveSystem system;

        public SaveData Data { get; private set; }
        public int Beans => Data.frijolitos;
        public int TotalStars => ProgressionRules.TotalStars(Data);
        public int TotalMangos => ProgressionRules.TotalMangos(Data);

        public event Action Changed;

        private void Awake()
        {
            if (system == null)
                Initialize(new SaveSystem(SaveSystem.DefaultPath));
            ResultsController.PreviousBestStars = BestStars;
            ResultsController.Commit = Commit;
        }

        private void OnDestroy()
        {
            // Solo si seguimos siendo quienes están conectados.
            if (ResultsController.Commit.Target == (object)this)
                ResultsController.ResetHooks();
        }

        // Pruebas: otra ruta de guardado.
        public void Initialize(SaveSystem saveSystem)
        {
            system = saveSystem;
            GhostStore.Folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(saveSystem.FilePath), "ghosts");
            Data = system.Load();
            GameSettings.Apply(Data.settings);
        }

        public void Save() => system.Save(Data);

        public int BestStars(string levelId)
        {
            var record = Data.FindLevel(levelId);
            return record != null && record.stars > 0 ? record.stars : -1;
        }

        public bool IsWorldUnlocked(WorldData world) => ProgressionRules.IsWorldUnlocked(world, Data);

        // Fin de nivel: guarda estrellas y tiempo solo si mejoran (sin modificadores), suma los mangos y los frijolitos.
        public void Commit(LevelData level, LevelResults results, bool modifiersActive)
        {
            var record = Data.GetOrAddLevel(level.id);
            if (!modifiersActive)
            {
                record.stars = Mathf.Max(record.stars, results.Stars);
                if (record.best <= 0f || results.Time < record.best)
                    record.best = results.Time;
            }
            record.mangos |= results.MangoMask;
            AddBeans(results.Beans);
            Save();
            Changed?.Invoke();
        }

        // Espejo (docs/06 §1): estrellas y tiempo en sus propios récords; los mangos son los del nivel.
        public void CommitMirror(LevelData level, LevelResults results)
        {
            var record = Data.GetOrAddMirror(level.id);
            record.stars = Mathf.Max(record.stars, results.Stars);
            if (record.best <= 0f || results.Time < record.best)
                record.best = results.Time;
            Data.GetOrAddLevel(level.id).mangos |= results.MangoMask;
            AddBeans(results.Beans);
            Save();
            Changed?.Invoke();
        }

        public int MirrorBestStars(string levelId)
        {
            var record = Data.FindMirror(levelId);
            return record != null && record.stars > 0 ? record.stars : -1;
        }

        // Contrarreloj: agrega el tiempo a la tabla del nivel si entra entre los mejores. Devuelve el puesto (0 = el mejor)
        // o -1 si no entró.
        public int RecordTrial(string levelId, float time, string team, System.Collections.Generic.IList<string> characters, int keep)
        {
            var table = Data.GetOrAddTrial(levelId);
            var entry = new SaveData.TrialEntry { time = time, team = team };
            if (characters != null)
                entry.characters.AddRange(characters);
            int rank = table.entries.FindIndex(e => time < e.time);
            if (rank < 0)
                rank = table.entries.Count;
            if (rank >= keep)
                return -1;
            table.entries.Insert(rank, entry);
            if (table.entries.Count > keep)
                table.entries.RemoveRange(keep, table.entries.Count - keep);
            Save();
            Changed?.Invoke();
            return rank;
        }

        public void AddBeans(int amount)
        {
            Data.frijolitos = Mathf.Max(0, Data.frijolitos + Mathf.Max(0, amount));
            Changed?.Invoke();
        }

        // Compra: valida el saldo; nunca queda negativo.
        public bool TrySpendBeans(int price)
        {
            if (price < 0 || Data.frijolitos < price)
                return false;
            Data.frijolitos -= price;
            Save();
            Changed?.Invoke();
            return true;
        }
    }
}
