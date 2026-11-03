using UnityEngine;

namespace GasSafari.Hazards
{
    // Etiquetas de un obstáculo, consultadas por habilidades y power-ups.
    public class HazardTags : MonoBehaviour
    {
        [SerializeField] private HazardTag tags;

        public HazardTag Tags => tags;

        public bool Has(HazardTag tag) => (tags & tag) == tag;

        public void Configure(HazardTag value)
        {
            tags = value;
        }
    }
}
