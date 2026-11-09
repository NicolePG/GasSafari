using GasSafari.Raft;
using GasSafari.UI;
using UnityEngine;

namespace GasSafari.Hazards
{
    // Aviso del abismo H22 (docs/05): una zona 30 m antes del hueco. Al cruzarla la balsa se emite el aviso de peligro del HUD
    // (evento + borde rojo de pantalla + texto "¡Abismo adelante!"); las boyas rojas y el cartel de peligro los pone el nivel.
    [RequireComponent(typeof(BoxCollider))]
    public class AbyssWarning : HazardBase
    {
        public const float DISTANCE_BEFORE = 30f;
        private const float EDGE_SECONDS = 3f;
        private const float COOLDOWN = 8f;

        private static readonly Color DangerRed = new Color(0.95f, 0.08f, 0.05f);

        private float lastWarn = -100f;

        public int Warnings { get; private set; }

        protected override void OnRaftEnter(RaftController raft)
        {
            if (Time.time - lastWarn < COOLDOWN)
                return;
            lastWarn = Time.time;
            Warnings++;
            Warn();
            ScreenEdgeWarning.Show(DangerRed, EDGE_SECONDS);
        }
    }
}
