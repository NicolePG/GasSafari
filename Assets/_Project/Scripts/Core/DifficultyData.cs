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
        [Tooltip("Velocidad de impacto (m/s, componente contra la superficie) sobre la que un golpe revienta una cámara; " +
                 "pinchos y mordidas revientan siempre. Antes chamberDamageImpulse (N·s), que dependía de la masa de la balsa")]
        public float chamberDamageSpeed;
        [Tooltip("Segundos sumados al cronómetro por reaparecer")] public float respawnPenalty;
        public float hazardSpeed;
        public float autoRespawnTime;
        public CheckpointFilter checkpointFilter;
        [Tooltip("Probabilidad de que un bot responda a un pedo humano (docs/10 §15)")] public float botSyncChance = 0.75f;
    }
}
