using GasSafari.Input;
using UnityEngine;

namespace GasSafari.Characters
{
    // Raíz de un animal. Lee un comando por frame de su fuente (humano, bot o red).
    [DefaultExecutionOrder(-50)]
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] private CharacterData character;

        private IPlayerCommandSource source;

        public CharacterData Character => character;
        public PlayerCommand Command { get; private set; }
        public GasTank Gas { get; private set; }
        // Intro, cuenta 3-2-1 y resultados: el avatar recibe un comando vacío (LevelManager).
        public bool InputLocked { get; set; }
        // Color del jugador (aro, destellos de power-ups).
        public Color PlayerColor { get; set; } = Color.white;

        private void Awake()
        {
            source = GetComponent<IPlayerCommandSource>();
            (source as IAvatarBoundSource)?.Bind(this);
            Gas = GetComponent<GasTank>();
        }

        public void SetCommandSource(IPlayerCommandSource commandSource)
        {
            source = commandSource;
            (source as IAvatarBoundSource)?.Bind(this);
        }

        private void Update()
        {
            if (source != null)
                Command = InputLocked ? default : source.Current;
        }
    }
}
