using UnityEngine;

namespace GasSafari.Progression
{
    // Una tarjeta de "Elegir modo" (docs/06 §1).
    [CreateAssetMenu(menuName = "Gas Safari/Game Mode", fileName = "Mode")]
    public class GameModeData : ScriptableObject
    {
        [Tooltip("story, time_trial, mirror, modifiers, party, endless, daily, team_race (ProgressionRules.MODE_*)")] public string id;
        public int order;
        [Tooltip("mode.story.name / mode.story.desc")] public string keyBase;
        public Sprite icon;
        [Tooltip("La escena del modo ya existe (los modos de M7 se activan al construirlos)")] public bool implemented;
        public int minPlayers = 1;
        public int maxPlayers = 4;

        public string NameKey => "mode." + keyBase + ".name";
        public string DescKey => "mode." + keyBase + ".desc";
    }
}
