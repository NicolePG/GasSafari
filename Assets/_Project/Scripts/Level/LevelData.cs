using UnityEngine;

namespace GasSafari.Level
{
    // Un nivel (docs/04, docs/10 §9). Los tiempos en segundos.
    [CreateAssetMenu(menuName = "Gas Safari/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("W2-3, T-0")] public string id;
        [Tooltip("Nombre de la escena en Scenes/Levels (L_<ID>_<Nombre>)")] public string sceneName;
        [Tooltip("Vacío en el tutorial T-0, que no pertenece a ninguno de los 6 mundos")] public WorldData world;
        [Tooltip("Posición dentro del mundo (1-5); T-0 es 0")] public int orderInWorld;
        [Tooltip("Tiempo para 3 estrellas, en s")] public float goldTime;
        [Tooltip("Tiempo para 2 estrellas, en s (Oro x 1.4 redondeado hacia arriba a múltiplo de 5)")] public float silverTime;
        public int mangoCount = 3;
        public int checkpointCount;
        public bool isBoss;
        [Tooltip("T-0 no muestra el cronómetro")] public bool showTimer = true;
        [Tooltip("Pista de Carrera por Equipos: tiene 2 puntos de salida (docs/06)")] public bool raceCompatible;
        public string musicId;
        [TextArea] public string mechanicText;

        // Claves de la tabla de textos (strings.csv): level.w1_1.name / level.w1_1.mechanic. Los campos de texto de arriba
        // quedan solo como nota de diseño; la interfaz lee siempre por clave.
        public string KeyBase => "level." + (id != null && id.StartsWith("T") ? "t0" : (id ?? string.Empty).ToLowerInvariant().Replace('-', '_'));
        public string NameKey => KeyBase + ".name";
        public string MechanicKey => KeyBase + ".mechanic";
    }
}
