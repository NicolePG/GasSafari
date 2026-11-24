using UnityEngine;

namespace GasSafari.Modes
{
    // Un tramo de Infinito (docs/10 §9: prefab, mundo, dificultad 1-5, largo). Sale de un nivel de M5 (sourceLevel) y lo
    // arma "Gas Safari/Setup/Crear tramos de Infinito". Los tramos con giro tienen la variante espejada.
    [CreateAssetMenu(menuName = "Gas Safari/Endless Chunk", fileName = "Chunk")]
    public class EndlessChunkData : ScriptableObject
    {
        public string id;
        [Tooltip("Nivel de M5 del que sale la receta (W3-3)")] public string sourceLevel;
        [Range(1, 6)] public int world = 1;
        [Tooltip("1-5; 0 = tramo de salida, no se elige al azar")] [Range(0, 5)] public int difficulty = 1;
        [Tooltip("m a lo largo del cauce")] public float length = 40f;
        [Tooltip("Giro neto en grados de la variante normal (positivo = derecha)")] public float turn;
        public EndlessChunk prefab;
        [Tooltip("Variante con el giro al otro lado (solo si turn != 0)")] public EndlessChunk mirroredPrefab;

        public EndlessChunk PrefabFor(bool mirrored) => mirrored && mirroredPrefab != null ? mirroredPrefab : prefab;

        public ChunkSpec Spec => new ChunkSpec
        {
            Id = id,
            World = world,
            Difficulty = difficulty,
            Length = length,
            Turn = mirroredPrefab != null ? turn : 0f
        };
    }
}
