using System;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Team;
using UnityEngine;

namespace GasSafari.Level
{
    // Cuenta lo que hace cada jugador durante el nivel para los premios de resultados (docs/06 §4).
    public class LevelStats : MonoBehaviour
    {
        private const string GOLDEN_MANGO_ID = "P07";
        // Un pedo es inoportuno si salió en el último segundo antes del choque.
        private const float UNTIMELY_WINDOW = 1f;
        private const int SEVERITY_CHAMBER = 1;
        private const int SEVERITY_CAPSIZE = 2;
        private const int SEVERITY_SINK = 3;

        private readonly StatsData data = new StatsData();
        private LevelManager level;
        private SyncDetector sync;
        private int lastFartPlayer = -1;
        private float lastFartTime = float.NegativeInfinity;
        // Un mismo pedo cuenta una sola vez aunque cause varios eventos (cámara y hundimiento).
        private bool lastFartCounted;

        public StatsData Data => data;

        // severity, jugador culpable (-1 ninguno).
        public event Action<int, int> Crashed;

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Bind(LevelManager manager)
        {
            level = manager;
            sync = manager.Raft.GetComponent<SyncDetector>();
            data.Players = manager.Avatars.Count;
            GameEvents.OnSync += OnSync;
            GameEvents.OnPlayerRescued += OnRescued;
            GameEvents.OnAbilityUsed += OnAbility;
            GameEvents.OnPowerUp += OnPowerUp;
            GameEvents.OnPlayerFell += OnFell;
            GameEvents.OnGasEmpty += OnGasEmpty;
            GameEvents.OnFart += OnFart;
            GameEvents.OnChamberPopped += OnChamberPopped;
            GameEvents.OnRaftSunk += OnSunk;
            GameEvents.OnRaftCapsized += OnCapsized;
        }

        private void Unsubscribe()
        {
            GameEvents.OnSync -= OnSync;
            GameEvents.OnPlayerRescued -= OnRescued;
            GameEvents.OnAbilityUsed -= OnAbility;
            GameEvents.OnPowerUp -= OnPowerUp;
            GameEvents.OnPlayerFell -= OnFell;
            GameEvents.OnGasEmpty -= OnGasEmpty;
            GameEvents.OnFart -= OnFart;
            GameEvents.OnChamberPopped -= OnChamberPopped;
            GameEvents.OnRaftSunk -= OnSunk;
            GameEvents.OnRaftCapsized -= OnCapsized;
        }

        private int IndexOf(PlayerAvatar avatar)
        {
            var avatars = level.Avatars;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (avatars[i] == avatar)
                    return i;
            }
            return -1;
        }

        private void Count(int[] counters, PlayerAvatar avatar)
        {
            int index = avatar != null ? IndexOf(avatar) : -1;
            if (index >= 0)
                counters[index]++;
        }

        // IsInGroup solo vale justo al recibir OnSync.
        private void OnSync(int players, float multiplier)
        {
            if (players >= GameSession.MAX_PLAYERS)
                data.FullSyncGroups++;
            var avatars = level.Avatars;
            for (int i = 0; i < avatars.Count; i++)
            {
                if (sync.IsInGroup(avatars[i].GetComponent<FartThruster>()))
                    data.SyncGroups[i]++;
            }
        }

        private void OnRescued(PlayerAvatar rescued, PlayerAvatar rescuer) => Count(data.Rescues, rescuer);
        private void OnAbility(PlayerAvatar avatar, string id) => Count(data.Abilities, avatar);
        private void OnFell(PlayerAvatar avatar) => Count(data.Falls, avatar);
        private void OnGasEmpty(PlayerAvatar avatar) => Count(data.NoGas, avatar);

        private void OnPowerUp(PowerUps.PowerUpData powerUp, PlayerAvatar taker)
        {
            Count(data.PowerUps, taker);
            if (powerUp.id == GOLDEN_MANGO_ID)
                data.GoldenMangos++;
        }

        private void OnFart(PlayerAvatar avatar, FartType type, float impulse)
        {
            int index = IndexOf(avatar);
            if (index < 0)
                return;
            data.Farts[index]++;
            lastFartPlayer = index;
            lastFartTime = Time.time;
            lastFartCounted = false;
        }

        private void OnChamberPopped(int chamber) => RecordCrash(SEVERITY_CHAMBER);
        private void OnCapsized() => RecordCrash(SEVERITY_CAPSIZE);

        private void OnSunk()
        {
            data.SunkCount++;
            RecordCrash(SEVERITY_SINK);
        }

        private void RecordCrash(int severity)
        {
            int culprit = Time.time - lastFartTime <= UNTIMELY_WINDOW ? lastFartPlayer : -1;
            if (culprit >= 0 && !lastFartCounted)
            {
                data.UntimelyFarts[culprit]++;
                lastFartCounted = true;
            }
            Crashed?.Invoke(severity, culprit);
        }
    }
}
