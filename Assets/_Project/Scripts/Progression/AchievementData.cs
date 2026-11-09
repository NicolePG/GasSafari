using UnityEngine;

namespace GasSafari.Progression
{
    public enum AchievementKind : byte
    {
        FirstFart,
        SyncGroup,
        Synergy,
        SuperFart,
        Sinks,
        NoChambersLost,
        WorldFinished,
        PerfectWorld,
        TotalStars,
        TotalMangos,
        ThreeStarLevels,
        StoryOnHardest,
        PartyPlayed,
        EndlessMeters,
        DailyStreak,
        FinishLevel
    }

    // Un logro (docs/06 §5, 26 en total). 'value' y 'text' dependen del tipo.
    [CreateAssetMenu(menuName = "Gas Safari/Achievement", fileName = "Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Tooltip("first_fart, duet, raft_king... los mismos de ach.<id>.name en la tabla")] public string id;
        [Tooltip("Orden en la rejilla (1-26)")] public int order;
        public AchievementKind kind;
        public int value;
        [Tooltip("Sinergia (honda_selva...), mundo (W1...) o nivel (W6-5) según el tipo")] public string target;
        [Tooltip("Se muestra como ??? hasta desbloquearlo")] public bool secret;
        public Sprite icon;

        public string NameKey => "ach." + id + ".name";
        public string DescKey => "ach." + id + ".desc";
    }
}
