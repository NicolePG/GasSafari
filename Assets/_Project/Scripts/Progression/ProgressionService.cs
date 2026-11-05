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
            Data = system.Load();
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
