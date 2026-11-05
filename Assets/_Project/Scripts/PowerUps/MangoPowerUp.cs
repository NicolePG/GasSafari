using GasSafari.Characters;
using UnityEngine;

namespace GasSafari.PowerUps
{
    // P03 Mango: suma una fracción de gas a todo el equipo.
    public class MangoPowerUp : PowerUpBase
    {
        protected override void Apply(PowerUpContext context)
        {
            var crew = context.Seats.Crew;
            for (int i = 0; i < crew.Count; i++)
            {
                var tank = crew[i].Avatar.Gas;
                tank.SetFraction(Mathf.Min(1f, tank.Normalized + Data.magnitude));
            }
        }
    }
}
