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

        private void Awake()
        {
            source = GetComponent<IPlayerCommandSource>();
            Gas = GetComponent<GasTank>();
        }

        public void SetCommandSource(IPlayerCommandSource commandSource)
        {
            source = commandSource;
        }

        private void Update()
        {
            if (source != null)
                Command = source.Current;
        }
    }
}
