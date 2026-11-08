using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H26 Murciélagos (docs/05): bandada que avisa con un chillido 1 s y tira al agua a un animal suelto (misma regla que
    // la fosa). Gira sobre su zona; el Rugido y la cebolla la espantan.
    public class BatSwarm : PitAmbush
    {
        [SerializeField] private float spinDegrees = 140f;

        protected override void Update()
        {
            base.Update();
            transform.Rotate(0f, spinDegrees * Time.deltaTime, 0f, Space.Self);
        }
    }
}
