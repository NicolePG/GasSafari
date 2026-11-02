namespace GasSafari.Input
{
    // docs/02 §11. NoGas lo emite el sistema, no el jugador.
    public enum PingType : byte
    {
        None,
        Now,
        Wait,
        TurnLeft,
        TurnRight,
        NoGas
    }
}
