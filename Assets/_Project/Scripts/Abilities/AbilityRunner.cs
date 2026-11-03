using System;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Input;
using UnityEngine;

namespace GasSafari.Abilities
{
    // Activa la habilidad del personaje al pulsar y lleva su recarga.
    [RequireComponent(typeof(PlayerAvatar), typeof(AvatarStateMachine))]
    public class AbilityRunner : MonoBehaviour
    {
        private readonly AbilityCooldown cooldown = new AbilityCooldown();
        private PlayerAvatar avatar;
        private AvatarStateMachine states;
        private AbilityContext context;
        private byte lastPresses;
        private float castTimer;

        // Por defecto la del personaje; se puede reemplazar (pruebas, modificadores).
        public AbilityBase Ability { get; set; }
        public AbilityCooldown Cooldown => cooldown;
        public AbilityContext Context => context;

        public event Action<AbilityBase> Activated;

        private void Awake()
        {
            avatar = GetComponent<PlayerAvatar>();
            states = GetComponent<AvatarStateMachine>();
            context = new AbilityContext(this);
            Ability = avatar.Character.ability;
        }

        private void Update()
        {
            cooldown.Tick(Time.deltaTime);
            UpdateCast();

            var cmd = avatar.Command;
            bool pressed = PlayerCommand.NewPresses(cmd.AbilityPresses, lastPresses) > 0;
            lastPresses = cmd.AbilityPresses;
            if (pressed)
                TryActivate();
        }

        public bool TryActivate()
        {
            if (Ability == null || !cooldown.IsReady || states.State != AvatarState.Seated)
                return false;

            context.Refresh();
            if (!Ability.CanActivate(context))
                return false;

            Ability.Activate(context);
            cooldown.Start(Ability.cooldown);
            Services.Audio.Play(Ability.sound, transform.position);
            GameEvents.RaiseAbilityUsed(avatar, Ability.id);
            Activated?.Invoke(Ability);
            return true;
        }

        // La habilidad ocupa al animal 'duration' segundos: no se mueve ni dispara (estado AbilityCast).
        public void Cast(float duration)
        {
            castTimer = duration;
            states.Enter(AvatarState.AbilityCast);
        }

        private void UpdateCast()
        {
            if (castTimer <= 0f)
                return;
            castTimer -= Time.deltaTime;
            if (castTimer <= 0f && states.State == AvatarState.AbilityCast)
                states.Enter(AvatarState.Seated);
        }
    }
}
