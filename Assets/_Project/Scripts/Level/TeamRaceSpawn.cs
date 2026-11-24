using UnityEngine;

namespace GasSafari.Level
{
    // Marcador del nivel (docs/10 §5): salida de una balsa en Carrera por Equipos (docs/06 §1). Los niveles compatibles
    // (LevelData.raceCompatible) tienen dos, uno por equipo, junto a la puerta de salida. Los coloca TeamRaceSpawnPass.
    public class TeamRaceSpawn : MonoBehaviour
    {
        public const int TEAMS = 2;

        [Tooltip("0 = equipo Azul, 1 = equipo Naranja")]
        [SerializeField, Range(0, TEAMS - 1)] private int team;

        public int Team => team;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        public void Configure(int teamIndex)
        {
            team = Mathf.Clamp(teamIndex, 0, TEAMS - 1);
        }

        // Las dos salidas del nivel cargado, por equipo (null si falta alguna). Solo al iniciar la carrera.
        public static TeamRaceSpawn[] FindAll()
        {
            var result = new TeamRaceSpawn[TEAMS];
            foreach (var spawn in FindObjectsByType<TeamRaceSpawn>(FindObjectsSortMode.None))
            {
                if (result[spawn.Team] == null)
                    result[spawn.Team] = spawn;
            }
            return result;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = team == 0 ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
