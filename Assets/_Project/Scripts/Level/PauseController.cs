using System;
using GasSafari.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GasSafari.Level
{
    // Pausa del nivel (docs/07 §4): congela el tiempo, salvo en online, donde el menú solo se abre para ese jugador.
    // Reiniciar desde el checkpoint pasa por una votación entre los jugadores humanos (docs/02 §10).
    public class PauseController : MonoBehaviour
    {
        private const string PAUSE_ACTION = "Pause";

        [SerializeField] private LevelManager level;
        [SerializeField] private CheckpointSystem checkpoints;

        private readonly RestartVote vote = new RestartVote();
        private InputAction[] pauseActions = Array.Empty<InputAction>();
        private Action<InputAction.CallbackContext>[] handlers = Array.Empty<Action<InputAction.CallbackContext>>();

        public bool IsOpen { get; private set; }
        public PlayerSlot Owner { get; private set; }
        public RestartVote Vote => vote;
        // Falso en online: el juego sigue corriendo.
        public bool StopsTime => !Services.Session.IsOnline;

        public event Action<PlayerSlot> Opened;
        public event Action Closed;
        public event Action VoteChanged;
        // Los destinos los conecta el cargador de escenas y las pantallas de Opciones (M6).
        public event Action RestartLevelRequested;
        public event Action QuitToMapRequested;
        public event Action OptionsRequested;
        public event Action ControlsRequested;

        public void Configure(LevelManager levelManager, CheckpointSystem system)
        {
            level = levelManager;
            checkpoints = system;
        }

        private void Start()
        {
            if (checkpoints == null)
                checkpoints = FindFirstObjectByType<CheckpointSystem>();
            BindPauseButtons();
        }

        private void OnDisable()
        {
            for (int i = 0; i < pauseActions.Length; i++)
            {
                if (pauseActions[i] != null)
                    pauseActions[i].performed -= handlers[i];
            }
            if (IsOpen)
                Close();
        }

        // Start / Options de cada jugador local.
        private void BindPauseButtons()
        {
            var players = Services.Session.Players;
            pauseActions = new InputAction[players.Count];
            handlers = new Action<InputAction.CallbackContext>[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Input == null)
                    continue;
                var slot = players[i];
                pauseActions[i] = slot.Input.actions.FindAction(PAUSE_ACTION);
                if (pauseActions[i] == null)
                    continue;
                handlers[i] = context => Toggle(slot);
                pauseActions[i].performed += handlers[i];
            }
        }

        public void Toggle(PlayerSlot who)
        {
            // Con Opciones abiertas encima, Esc/Start pertenecen a ese menú.
            if (UI.OptionsOverlay.IsOpen)
                return;
            if (IsOpen)
                Close();
            else
                Open(who);
        }

        public void Open(PlayerSlot who)
        {
            if (IsOpen || level.State == LevelState.Results)
                return;
            IsOpen = true;
            Owner = who;
            if (StopsTime)
            {
                Services.TimeController.SetPaused(true);
                level.SetPaused(true);
            }
            Opened?.Invoke(who);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            vote.Cancel();
            if (StopsTime)
            {
                level.SetPaused(false);
                Services.TimeController.SetPaused(false);
            }
            Closed?.Invoke();
        }

        // Un jugador solo reinicia al instante; con varios hay votación.
        public void RequestCheckpointRestart(PlayerSlot who)
        {
            int voters = HumanCount();
            if (voters <= 1)
            {
                ApplyCheckpointRestart();
                return;
            }
            Resolve(vote.Start(voters, IndexOf(who)));
        }

        public void CastVote(PlayerSlot who, bool accept)
        {
            Resolve(vote.Cast(IndexOf(who), accept));
        }

        private void Resolve(VoteResult result)
        {
            VoteChanged?.Invoke();
            if (result == VoteResult.Accepted)
                ApplyCheckpointRestart();
        }

        private void ApplyCheckpointRestart()
        {
            Close();
            checkpoints.RequestRespawn();
        }

        public void RestartLevel()
        {
            Close();
            RestartLevelRequested?.Invoke();
        }

        public void QuitToMap()
        {
            Close();
            QuitToMapRequested?.Invoke();
        }

        // Siguiente jugador humano que aún no votó; en una sola consola votan por turnos.
        public PlayerSlot NextVoter()
        {
            int index = 0;
            var players = Services.Session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsBot)
                    continue;
                if (!vote.HasVoted(index))
                    return players[i];
                index++;
            }
            return null;
        }

        public void OpenOptions() => OptionsRequested?.Invoke();
        public void OpenControls() => ControlsRequested?.Invoke();

        private static int HumanCount()
        {
            int count = 0;
            var players = Services.Session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].IsBot)
                    count++;
            }
            return count;
        }

        // Índice del jugador entre los humanos, para la votación.
        private static int IndexOf(PlayerSlot who)
        {
            int index = 0;
            var players = Services.Session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == who)
                    return index;
                if (!players[i].IsBot)
                    index++;
            }
            return -1;
        }
    }
}
