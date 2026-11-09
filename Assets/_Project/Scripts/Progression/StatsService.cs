using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Save;
using UnityEngine;

namespace GasSafari.Progression
{
    // Estadísticas acumuladas (docs/06 §6): pedos, grupos de sync por tamaño, Súper Pedos, caídas, rescates, hundimientos,
    // distancia, tiempo de juego y personaje más usado. Se cuentan con los eventos del juego y se guardan al terminar un nivel
    // y al cerrar la aplicación, no en cada evento.
    public class StatsService : MonoBehaviour
    {
        private SaveData.StatsRecord Stats => Services.Progression != null ? Services.Progression.Data.stats : null;

        private float seconds;
        private float meters;
        private Vector3 lastRaftPosition;
        private RaftTracker tracker;
        private bool dirty;

        private struct RaftTracker
        {
            public Object Raft;
            public bool Valid;
        }

        private void OnEnable()
        {
            GameEvents.OnFart += OnFart;
            GameEvents.OnSync += OnSync;
            GameEvents.OnSuperFart += OnSuperFart;
            GameEvents.OnPlayerFell += OnFell;
            GameEvents.OnPlayerRescued += OnRescued;
            GameEvents.OnRaftSunk += OnSunk;
            GameEvents.OnLevelFinished += OnLevelFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnFart -= OnFart;
            GameEvents.OnSync -= OnSync;
            GameEvents.OnSuperFart -= OnSuperFart;
            GameEvents.OnPlayerFell -= OnFell;
            GameEvents.OnPlayerRescued -= OnRescued;
            GameEvents.OnRaftSunk -= OnSunk;
            GameEvents.OnLevelFinished -= OnLevelFinished;
            Flush();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Flush();
        }

        private void OnApplicationQuit() => Flush();

        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            var stats = Stats;
            if (stats == null)
                return;
            stats.farts++;
            dirty = true;
        }

        private void OnSync(int players, float multiplier)
        {
            var stats = Stats;
            if (stats == null)
                return;
            if (players >= 4) stats.sync4++;
            else if (players == 3) stats.sync3++;
            else if (players == 2) stats.sync2++;
            dirty = true;
        }

        private void OnSuperFart(float power)
        {
            if (Stats == null) return;
            Stats.superFarts++;
            dirty = true;
        }

        private void OnFell(PlayerAvatar avatar)
        {
            if (Stats == null) return;
            Stats.falls++;
            dirty = true;
        }

        private void OnRescued(PlayerAvatar rescued, PlayerAvatar rescuer)
        {
            if (Stats == null) return;
            Stats.rescues++;
            dirty = true;
        }

        private void OnSunk()
        {
            if (Stats == null) return;
            Stats.sinks++;
            dirty = true;
        }

        // Al terminar un nivel: suma un uso a cada personaje de la balsa y guarda.
        private void OnLevelFinished(float time)
        {
            var stats = Stats;
            if (stats == null)
                return;
            if (Services.IsReady)
            {
                var players = Services.Session.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].Character != null)
                        AddCharacterUse(stats, players[i].Character.id);
                }
            }
            dirty = true;
            Flush();
        }

        public static void AddCharacterUse(SaveData.StatsRecord stats, string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;
            for (int i = 0; i < stats.characters.Count; i++)
            {
                if (stats.characters[i].id == characterId)
                {
                    stats.characters[i].count++;
                    return;
                }
            }
            stats.characters.Add(new SaveData.CharacterUse { id = characterId, count = 1 });
        }

        // El personaje con más niveles terminados, o null si todavía no hay datos.
        public static string FavoriteCharacter(SaveData.StatsRecord stats)
        {
            string best = null;
            int bestCount = 0;
            for (int i = 0; i < stats.characters.Count; i++)
            {
                if (stats.characters[i].count > bestCount)
                {
                    bestCount = stats.characters[i].count;
                    best = stats.characters[i].id;
                }
            }
            return best;
        }

        // Tiempo de juego y distancia mientras un nivel está en marcha.
        private void Update()
        {
            var stats = Stats;
            var manager = LevelManager.Current;
            if (stats == null || manager == null || manager.State != LevelState.Running || manager.IsPaused)
            {
                tracker.Valid = false;
                return;
            }

            seconds += Time.unscaledDeltaTime;
            if (seconds >= 1f)
            {
                int whole = Mathf.FloorToInt(seconds);
                stats.playSeconds += whole;
                seconds -= whole;
                dirty = true;
            }

            var raft = manager.Raft;
            if (raft == null)
                return;
            var position = raft.transform.position;
            if (tracker.Valid && tracker.Raft == raft)
            {
                var delta = position - lastRaftPosition;
                delta.y = 0f;
                // Un salto brusco (reaparición) no es distancia recorrida.
                if (delta.magnitude < 5f)
                    meters += delta.magnitude;
            }
            tracker.Raft = raft;
            tracker.Valid = true;
            lastRaftPosition = position;
            if (meters >= 1f)
            {
                int whole = Mathf.FloorToInt(meters);
                stats.meters += whole;
                meters -= whole;
                dirty = true;
            }
        }

        public void Flush()
        {
            if (!dirty || Services.Progression == null)
                return;
            dirty = false;
            Services.Progression.Save();
        }
    }
}
