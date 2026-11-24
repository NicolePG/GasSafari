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
        private bool hasStartPose;
        private Vector3 startPosition;
        private Quaternion startRotation;

        public RaftController Raft { get; private set; }
        public IReadOnlyList<PlayerAvatar> Avatars => avatars;

        public void Configure(GameObject raft, GameObject fallbackAvatar, StartGate gate)
        {
            raftPrefab = raft;
            fallbackAvatarPrefab = fallbackAvatar;
            startGate = gate;
        }

        // Otra salida para la balsa del nivel en vez de la puerta (Carrera por Equipos: su TeamRaceSpawn).
        public void SetStartPose(Vector3 position, Quaternion rotation)
        {
            hasStartPose = true;
            startPosition = position;
            startRotation = rotation;
        }

        public RaftController Spawn(IReadOnlyList<PlayerSlot> slots)
        {
            var raftObject = hasStartPose
                ? Instantiate(raftPrefab, startPosition, startRotation)
                : Instantiate(raftPrefab, startGate.Position, startGate.Rotation);
            Progression.CosmeticsApplier.ApplyRaft(raftObject);
            Raft = raftObject.GetComponent<RaftController>();
            if (raftObject.GetComponent<RaftWake>() == null)
                raftObject.AddComponent<RaftWake>();
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

        // Balsa adicional con su tripulación (minijuegos de Fiesta, Carrera por Equipos): no cambia Raft ni Avatars.
        // 'radius' > 0 la convierte en mini balsa con ese radio. Los animales creados se agregan a 'crew'.
        public RaftController SpawnExtra(Vector3 position, Quaternion rotation, IReadOnlyList<PlayerSlot> slots,
            List<PlayerAvatar> crew, bool applySkin = true)
        {
            var raftObject = Instantiate(raftPrefab, position, rotation);
            if (applySkin)
                Progression.CosmeticsApplier.ApplyRaft(raftObject);
            var raft = raftObject.GetComponent<RaftController>();
            if (raftObject.GetComponent<RaftWake>() == null)
                raftObject.AddComponent<RaftWake>();
            var seats = raftObject.GetComponent<RaftSeats>();
            int count = Mathf.Clamp(slots.Count, 0, GameSession.MAX_PLAYERS);
            if (count == 0)
                return raft;
            var angles = seats.GetStartAngles(count);
            for (int i = 0; i < count; i++)
                crew.Add(SpawnAvatar(slots[i], seats, angles[i]));
            return raft;
        }

        // Un slot de bot: su BotBrain escribe los comandos en un BotCommandSource.
        private static void AttachBot(PlayerAvatar avatar, PlayerSlot slot)
        {
            var source = slot.CommandSource as Bots.BotCommandSource ?? new Bots.BotCommandSource();
            slot.CommandSource = source;
            var brain = avatar.GetComponent<Bots.BotBrain>();
            if (brain == null)
                brain = avatar.gameObject.AddComponent<Bots.BotBrain>();
            brain.Attach(source, Services.IsReady ? Services.Session.BotConfig : null);
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
            avatar.PlayerColor = slot.Color;
            if (slot.CommandSource != null)
                avatar.SetCommandSource(slot.CommandSource);
            avatar.GetComponent<PlayerSeat>().Sit(seats, angle);
            if (slot.IsBot)
                AttachBot(avatar, slot);
            Progression.CosmeticsApplier.ApplyAvatar(avatar, slot);
            return avatar;
        }
    }
}
