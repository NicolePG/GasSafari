using Unity.Netcode;
using UnityEngine;

namespace GasSafari.Input
{
    // Comando de un jugador en un frame. Los contadores de pulsaciones son acumulativos
    // (byte con desborde) para que no se pierdan pulsaciones entre ticks de red.
    public struct PlayerCommand : INetworkSerializable
    {
        public float SeatMove;
        public float Aim;
        public bool FartHeld, HoldHeld, AbilityHeld, GrabHeld, SuperHeld;
        public byte FartPresses, AbilityPresses, JumpPresses;
        public PingType Ping;
        public Vector2 Swim;

        // Pulsaciones nuevas entre dos lecturas de un contador, tolerando el desborde.
        public static int NewPresses(byte current, byte previous)
        {
            return (byte)(current - previous);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SeatMove);
            serializer.SerializeValue(ref Aim);
            serializer.SerializeValue(ref FartHeld);
            serializer.SerializeValue(ref HoldHeld);
            serializer.SerializeValue(ref AbilityHeld);
            serializer.SerializeValue(ref GrabHeld);
            serializer.SerializeValue(ref SuperHeld);
            serializer.SerializeValue(ref FartPresses);
            serializer.SerializeValue(ref AbilityPresses);
            serializer.SerializeValue(ref JumpPresses);
            serializer.SerializeValue(ref Ping);
            serializer.SerializeValue(ref Swim);
        }
    }
}
