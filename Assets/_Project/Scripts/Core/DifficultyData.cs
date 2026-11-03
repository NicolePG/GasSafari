using UnityEngine;

namespace GasSafari.Core
{
    // Una dificultad de docs/06 §3 (Familiar, Normal, Amistades Rotas).
    [CreateAssetMenu(menuName = "Gas Safari/Difficulty", fileName = "Difficulty")]
    public class DifficultyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public float syncWindow;
        [Tooltip("Multiplica la recarga de gas del personaje")] public float regenMultiplier;
        [Tooltip("Si es falso, solo pinchos y mordidas revientan cámaras")] public bool impactsDamageChambers;
        public float chamberDamageImpulse;
        [Tooltip("Segundos sumados al cronómetro por reaparecer")] public float respawnPenalty;
        public float hazardSpeed;
        public float autoRespawnTime;
        public CheckpointFilter checkpointFilter;
    }
}
