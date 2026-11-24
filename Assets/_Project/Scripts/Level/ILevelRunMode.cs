namespace GasSafari.Level
{
    // Reglas del modo del nivel en curso para los resultados (docs/06 §1): de dónde sale el récord anterior, cómo se
    // ajustan los resultados y dónde se guardan. Lo implementa GameModeBase; sin modo, el nivel usa las reglas de Historia.
    public interface ILevelRunMode
    {
        int PreviousBestStars(string levelId);
        void BuildResults(LevelResults results);
        void Commit(LevelData level, LevelResults results);
        // false: el modo muestra su propio final (Carrera por Equipos) y no se arman ni guardan los resultados del nivel.
        bool ShowsLevelResults { get; }
    }
}
