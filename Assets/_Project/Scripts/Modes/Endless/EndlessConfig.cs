using System.Collections.Generic;
using UnityEngine;

namespace GasSafari.Modes
{
    // Valores de Infinito y del Desafío Diario (docs/06 §1). Vive en Resources/EndlessConfig.asset y lo llena
    // "Gas Safari/Setup/Crear tramos de Infinito". Los marcados como provisionales están en docs/06.
    [CreateAssetMenu(menuName = "Gas Safari/Endless Config", fileName = "EndlessConfig")]
    public class EndlessConfig : ScriptableObject
    {
        public const string RESOURCE_PATH = "EndlessConfig";

        [Tooltip("En este orden los lee el planificador: cambiarlo cambia los recorridos de cada semilla")]
        public EndlessChunkData[] chunks = new EndlessChunkData[0];

        [Tooltip("Datos de la escena Infinito (la cargan Infinito y el Desafío Diario)")] public Level.LevelData level;

        [Header("Infinito (docs/06)")]
        [Tooltip("m entre subidas de dificultad (docs/06: 200)")] public float difficultyStep = 200f;
        [Tooltip("Un checkpoint cada N tramos (docs/06: 3)")] public int checkpointEvery = 3;
        [Tooltip("Hundimientos que terminan la partida (docs/06: 3)")] public int lives = 3;

        [Header("Desafío Diario (docs/06)")]
        [Tooltip("m del recorrido (docs/06: 1500)")] public float dailyLength = 1500f;

        [Header("Generador (provisional)")]
        [Tooltip("Ancho del cauce de todos los tramos, en m")] public float channelWidth = 12f;
        [Tooltip("Rumbo máximo respecto de la salida, en grados")] public float maxHeading = 45f;
        [Tooltip("m de río ya armado por delante de la balsa")] public float aheadDistance = 160f;
        [Tooltip("Tramos que se conservan antes del checkpoint actual")] public int keepBehind = 1;

        [Header("Frijolitos (docs/06 §2: 5 a 25; reparto provisional)")]
        public int beansMin = 5;
        public int beansMax = 25;
        [Tooltip("Infinito: m por cada frijolito por encima del mínimo")] public float metersPerBean = 100f;
        [Tooltip("Diario: primera vez del día")] public int dailyFirstBeans = 15;
        [Tooltip("Diario: extra por cada día de racha después del primero")] public int dailyStreakBeans = 2;

        private static EndlessConfig instance;
        private List<ChunkSpec> specs;

        public static EndlessConfig Instance
        {
            get => instance != null ? instance : instance = Resources.Load<EndlessConfig>(RESOURCE_PATH);
            set => instance = value;
        }

        public IReadOnlyList<ChunkSpec> Specs
        {
            get
            {
                if (specs == null || specs.Count != chunks.Length)
                {
                    specs = new List<ChunkSpec>(chunks.Length);
                    for (int i = 0; i < chunks.Length; i++)
                        specs.Add(chunks[i].Spec);
                }
                return specs;
            }
        }

        public EndlessPlanner CreatePlanner(int seed) => new EndlessPlanner(Specs, seed, difficultyStep, maxHeading);

        public List<ChunkPick> PlanDaily(int seed) => EndlessPlanner.Plan(Specs, seed, dailyLength, difficultyStep, maxHeading);

        private void OnValidate() => specs = null;
    }
}
