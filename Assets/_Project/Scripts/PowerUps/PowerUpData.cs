using GasSafari.Audio;
using UnityEngine;

namespace GasSafari.PowerUps
{
    public enum PowerUpTarget
    {
        NearestAnimal,
        Team,
        Raft
    }

    // Un power-up (docs/05, docs/10 §9). magnitude, duration y radius se interpretan según el tipo.
    [CreateAssetMenu(menuName = "Gas Safari/Power-up", fileName = "P00_PowerUp")]
    public class PowerUpData : ScriptableObject
    {
        [Tooltip("P01..P07")] public string id;
        public string displayName;
        public PowerUpTarget target;
        [Tooltip("Segundos; 0 = instantáneo")] public float duration;
        [Tooltip("P01 fracción de gas, P02 multiplicador de fuerza, P03 fracción de gas, P05 altura en m, P07 puntos de medidor")]
        public float magnitude;
        [Tooltip("P02 alcance con que quema Burnable (provisional, se ajusta en el playtest), P06 radio de la nube en m")]
        public float radius;
        [Tooltip("P07: queda recogido al reaparecer desde un checkpoint")] public bool permanent;
        public SoundId pickupSound;
    }
}
