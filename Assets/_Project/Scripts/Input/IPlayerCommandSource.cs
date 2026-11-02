namespace GasSafari.Input
{
    // Toda fuente de control (humano, bot, red) produce un PlayerCommand.
    // La lógica del avatar solo lee de aquí, nunca del Input System.
    public interface IPlayerCommandSource
    {
        PlayerCommand Current { get; }
    }
}
