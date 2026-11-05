using GasSafari.Characters;
using GasSafari.Raft;
using GasSafari.Team;

namespace GasSafari.PowerUps
{
    // Quién tomó el power-up y las piezas de su balsa que los efectos necesitan.
    public readonly struct PowerUpContext
    {
        public readonly PlayerAvatar Taker;
        public readonly RaftSeats Seats;
        public readonly RaftController Raft;
        public readonly RaftChambers Chambers;
        public readonly RaftEffects Effects;
        public readonly TeamMeter Meter;

        public PowerUpContext(PlayerAvatar taker, RaftSeats seats)
        {
            Taker = taker;
            Seats = seats;
            Raft = seats.Controller;
            Chambers = seats.GetComponent<RaftChambers>();
            Effects = seats.GetComponent<RaftEffects>();
            Meter = seats.GetComponent<TeamMeter>();
        }
    }
}
