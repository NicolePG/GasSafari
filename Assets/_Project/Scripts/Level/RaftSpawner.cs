using System.Collections.Generic;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Level
{
    // Crea la balsa en la puerta de salida y un PlayerAvatar por PlayerSlot en las posiciones iniciales (docs/02 §2).
    public class RaftSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject raftPrefab;
        [Tooltip("Para slots sin personaje o sin prefab propio")]
        [SerializeField] private GameObject fallbackAvatarPrefab;
        [SerializeField] private StartGate startGate;

        private readonly List<PlayerAvatar> avatars = new List<PlayerAvatar>();

        public RaftController Raft { get; private set; }
        public IReadOnlyList<PlayerAvatar> Avatars => avatars;

        public void Configure(GameObject raft, GameObject fallbackAvatar, StartGate gate)
        {
            raftPrefab = raft;
            fallbackAvatarPrefab = fallbackAvatar;
            startGate = gate;
        }

        public RaftController Spawn(IReadOnlyList<PlayerSlot> slots)
        {
            var raftObject = Instantiate(raftPrefab, startGate.Position, startGate.Rotation);
            Raft = raftObject.GetComponent<RaftController>();
            var seats = raftObject.GetComponent<RaftSeats>();

            avatars.Clear();
            int count = Mathf.Clamp(slots.Count, 0, GameSession.MAX_PLAYERS);
            if (count == 0)
                return Raft;

            var angles = seats.GetStartAngles(count);
            for (int i = 0; i < count; i++)
                avatars.Add(SpawnAvatar(slots[i], seats, angles[i]));
            return Raft;
        }

        public void SetInputLocked(bool locked)
        {
            for (int i = 0; i < avatars.Count; i++)
                avatars[i].InputLocked = locked;
        }

        private PlayerAvatar SpawnAvatar(PlayerSlot slot, RaftSeats seats, float angle)
        {
            var prefab = slot.Character != null && slot.Character.prefab != null ? slot.Character.prefab : fallbackAvatarPrefab;
            var avatar = Instantiate(prefab).GetComponent<PlayerAvatar>();
            if (slot.CommandSource != null)
                avatar.SetCommandSource(slot.CommandSource);
            avatar.GetComponent<PlayerSeat>().Sit(seats, angle);
            return avatar;
        }
    }
}
