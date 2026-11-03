using GasSafari.Characters;
using GasSafari.Raft;
using GasSafari.Team;

namespace GasSafari.Abilities
{
    // Lo que una habilidad puede tocar: el animal, su balsa y su equipo. Uno por AbilityRunner.
    public class AbilityContext
    {
        private RaftSeats cachedSeats;

        public AbilityRunner Runner { get; }
        public PlayerAvatar Avatar { get; }
        public PlayerSeat Seat { get; }
        public FartThruster Thruster { get; }
        public GasTank Gas { get; }
        public AvatarFall Fall { get; }
        public AvatarStateMachine States { get; }

        public RaftSeats Seats { get; private set; }
        public RaftController Raft { get; private set; }
        public RaftChambers Chambers { get; private set; }
        public RaftFlowReceiver Flow { get; private set; }
        public RaftEffects Effects { get; private set; }
        public SyncDetector Sync { get; private set; }
        public TeamMeter Meter { get; private set; }

        public AbilityContext(AbilityRunner runner)
        {
            Runner = runner;
            Avatar = runner.GetComponent<PlayerAvatar>();
            Seat = runner.GetComponent<PlayerSeat>();
            Thruster = runner.GetComponent<FartThruster>();
            Gas = runner.GetComponent<GasTank>();
            Fall = runner.GetComponent<AvatarFall>();
            States = runner.GetComponent<AvatarStateMachine>();
        }

        // Se llama antes de activar: la balsa puede cambiar (reaparición, Carrera por Equipos).
        public void Refresh()
        {
            var seats = Seat.IsSeated ? Seat.Raft : Fall.HomeRaft;
            if (seats == cachedSeats)
                return;

            cachedSeats = seats;
            Seats = seats;
            Raft = seats != null ? seats.Controller : null;
            Chambers = seats != null ? seats.GetComponent<RaftChambers>() : null;
            Flow = seats != null ? seats.GetComponent<RaftFlowReceiver>() : null;
            Effects = seats != null ? seats.GetComponent<RaftEffects>() : null;
            Sync = seats != null ? seats.GetComponent<SyncDetector>() : null;
            Meter = Sync != null ? Sync.Meter : null;
        }
    }
}
