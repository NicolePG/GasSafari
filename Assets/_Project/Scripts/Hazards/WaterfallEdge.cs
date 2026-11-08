using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Hazards
{
    // H17 Cascada / caída (docs/05): el borde de una caída de 3-20 m. La caída en sí es el cauce (agua inclinada); este
    // trigger avisa antes del borde (flecha en pantalla) para alinear la balsa. Aterrizar torcido hace rebotar (docs/02 §15).
    [RequireComponent(typeof(BoxCollider))]
    public class WaterfallEdge : HazardBase
    {
        private float lastWarn = -10f;

        public float Drop => Config.Get("drop", 3f);

        protected override void OnRaftEnter(RaftController raft)
        {
            if (Time.time - lastWarn < 4f)
                return;
            lastWarn = Time.time;
            Warn();
        }
    }
}
