using System;
using System.Collections.Generic;
using GasSafari.Core;
using GasSafari.Save;
using UnityEngine;

namespace GasSafari.Progression
{
    // Compras y equipamiento de cosméticos (docs/06 §2, docs/10 §4). Sombreros, colores de gas y celebraciones son por jugador
    // (p1..p4 en el guardado); la balsa es del equipo. Los premios de mangos y de W6-5 se entregan solos.
    public class CosmeticsService : MonoBehaviour
    {
        public const string DEFAULT_HAT = "hat_zoo_cap";
        public const string DEFAULT_GAS = "gas_classic";
        public const string DEFAULT_RAFT = "raft_orange";
        public const string FINISH_LEVEL_ID = "W6-5";

        private static readonly Color[] RainbowStops =
        {
            new Color(1f, 0.25f, 0.25f), new Color(1f, 0.65f, 0.15f), new Color(1f, 0.95f, 0.25f),
            new Color(0.3f, 0.9f, 0.35f), new Color(0.3f, 0.65f, 1f), new Color(0.7f, 0.4f, 1f)
        };

        private ProgressionService subscribed;
        private int rainbowStep;

        public event Action Changed;
        // Un premio acaba de entregarse (mangos o W6-5).
        public event Action<CosmeticData> RewardGranted;

        private SaveData Data => Services.Progression != null ? Services.Progression.Data : null;
        private GameCatalog Catalog => GameCatalog.Instance;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
            GrantRewards();
        }

        private void OnDisable()
        {
            if (subscribed != null)
                subscribed.Changed -= OnProgressChanged;
            subscribed = null;
        }

        private void TrySubscribe()
        {
            if (subscribed != null || Services.Progression == null)
                return;
            subscribed = Services.Progression;
            subscribed.Changed += OnProgressChanged;
        }

        private void OnProgressChanged() => GrantRewards();

        // ---------------------------------------------------------------- tenencia
        public bool IsOwned(CosmeticData item)
        {
            if (item == null)
                return false;
            if (item.source == CosmeticSource.Starter)
                return true;
            var data = Data;
            return data != null && data.cosmetics.owned.Contains(item.id);
        }

        public bool IsOwned(string id) => IsOwned(Catalog != null ? Catalog.FindCosmetic(id) : null);

        public bool CanAfford(CosmeticData item) => Services.Progression != null && Services.Progression.Beans >= item.price;

        // Compra con frijolitos. Solo los artículos de precio; nunca deja el saldo negativo.
        public bool TryBuy(CosmeticData item)
        {
            if (item == null || item.source != CosmeticSource.Price || IsOwned(item) || Services.Progression == null)
                return false;
            if (!Services.Progression.TrySpendBeans(item.price))
                return false;
            Data.cosmetics.owned.Add(item.id);
            Services.Progression.Save();
            Changed?.Invoke();
            return true;
        }

        public List<CosmeticData> OwnedIn(CosmeticCategory category)
        {
            var owned = new List<CosmeticData>();
            if (Catalog == null)
                return owned;
            foreach (var item in Catalog.InCategory(category))
            {
                if (IsOwned(item))
                    owned.Add(item);
            }
            return owned;
        }

        // ---------------------------------------------------------------- equipamiento
        private static string PlayerKey(int playerIndex) => "p" + (playerIndex + 1);

        private SaveData.EquippedRecord Record(int playerIndex, bool create)
        {
            var data = Data;
            if (data == null)
                return null;
            string key = PlayerKey(playerIndex);
            var list = data.cosmetics.equipped;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].player == key)
                    return list[i];
            }
            if (!create)
                return null;
            var record = new SaveData.EquippedRecord { player = key, hat = DEFAULT_HAT, gas = DEFAULT_GAS, celebration = string.Empty };
            list.Add(record);
            return record;
        }

        public string Equipped(int playerIndex, CosmeticCategory category)
        {
            if (category == CosmeticCategory.Raft)
                return Data != null && !string.IsNullOrEmpty(Data.cosmetics.raft) ? Data.cosmetics.raft : DEFAULT_RAFT;
            var record = Record(playerIndex, false);
            switch (category)
            {
                case CosmeticCategory.Hat: return record != null ? record.hat : DEFAULT_HAT;
                case CosmeticCategory.Gas: return record != null ? record.gas : DEFAULT_GAS;
                default: return record != null ? record.celebration : string.Empty;
            }
        }

        // Equipa un artículo ya comprado; null quita sombrero o celebración.
        public bool Equip(int playerIndex, CosmeticCategory category, CosmeticData item)
        {
            if (Data == null)
                return false;
            if (item != null && (!IsOwned(item) || item.category != category))
                return false;
            string id = item != null ? item.id : string.Empty;
            if (category == CosmeticCategory.Raft)
            {
                Data.cosmetics.raft = string.IsNullOrEmpty(id) ? DEFAULT_RAFT : id;
            }
            else
            {
                var record = Record(playerIndex, true);
                switch (category)
                {
                    case CosmeticCategory.Hat: record.hat = id; break;
                    case CosmeticCategory.Gas: record.gas = string.IsNullOrEmpty(id) ? DEFAULT_GAS : id; break;
                    default: record.celebration = id; break;
                }
            }
            Services.Progression.Save();
            Changed?.Invoke();
            return true;
        }

        // Copia lo equipado a un lugar de la partida para que el nivel lo muestre.
        public void Apply(PlayerSlot slot)
        {
            slot.Hat = Equipped(slot.Index, CosmeticCategory.Hat);
            slot.Gas = Equipped(slot.Index, CosmeticCategory.Gas);
            slot.Celebration = Equipped(slot.Index, CosmeticCategory.Celebration);
        }

        // ---------------------------------------------------------------- premios
        // Hitos de mangos dorados (10 gas, 25 balsa, 40 sombrero) y la corona dorada al terminar W6-5.
        public void GrantRewards()
        {
            var data = Data;
            var catalog = Catalog;
            if (data == null || catalog == null)
                return;
            int mangos = ProgressionRules.TotalMangos(data);
            bool finished = ProgressionRules.HasFinished(data, FINISH_LEVEL_ID);
            bool changed = false;
            foreach (var item in catalog.cosmetics)
            {
                bool earned = item.source == CosmeticSource.MangoMilestone && mangos >= item.mangosRequired
                    || item.source == CosmeticSource.FinishW65 && finished;
                if (!earned || data.cosmetics.owned.Contains(item.id))
                    continue;
                data.cosmetics.owned.Add(item.id);
                changed = true;
                RewardGranted?.Invoke(item);
            }
            if (!changed)
                return;
            Services.Progression.Save();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- colores de gas
        // El Arcoíris recorre sus colores en cada pedo; los demás tienen un color fijo.
        public Color GasColor(string gasId)
        {
            var item = Catalog != null ? Catalog.FindCosmetic(gasId) : null;
            if (item == null)
                return Color.white;
            if (item.rainbow)
                return RainbowStops[rainbowStep++ % RainbowStops.Length];
            return item.gasColor;
        }
    }
}
