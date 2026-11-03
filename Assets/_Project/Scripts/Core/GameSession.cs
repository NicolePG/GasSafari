using System;
using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Core
{
    // Estado de la partida en curso (docs/10 §4). Se irá completando con modo, nivel y modificadores.
    public class GameSession : MonoBehaviour
    {
        // De 1 a 4 animales por balsa (docs/01).
        public const int MAX_PLAYERS = 4;

        [SerializeField] private DifficultyData difficulty;

        private readonly List<PlayerSlot> players = new List<PlayerSlot>();

        public DifficultyData Difficulty
        {
            get => difficulty;
            set => difficulty = value;
        }

        public IReadOnlyList<PlayerSlot> Players => players;

        public event Action<PlayerSlot> PlayerAdded;
        public event Action<PlayerSlot> PlayerRemoved;

        public void AddPlayer(PlayerSlot slot)
        {
            players.Add(slot);
            players.Sort((a, b) => a.Index.CompareTo(b.Index));
            PlayerAdded?.Invoke(slot);
        }

        public void RemovePlayer(PlayerSlot slot)
        {
            if (players.Remove(slot))
                PlayerRemoved?.Invoke(slot);
        }

        public void ClearPlayers()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                RemovePlayer(players[i]);
        }
    }
}
