using System;
using UnityEngine;

namespace GasSafari.Characters
{
    // Dónde va el sombrero en cada personaje (Tools/Export/Cosmetics/Hats/hat_anchors.json): la parte superior de la cabeza en
    // reposo, en el espacio del modelo (m), y la escala del sombrero (los sombreros se hicieron para una cabeza de 0.42 m).
    [CreateAssetMenu(menuName = "Gas Safari/Hat Anchors", fileName = "HatAnchors")]
    public class HatAnchorSet : ScriptableObject
    {
        [Serializable]
        public struct Anchor
        {
            public string character;
            public Vector3 headTop;
            public float scale;
        }

        [SerializeField] private Anchor[] anchors = Array.Empty<Anchor>();

        public Anchor[] Anchors => anchors;

        public void Set(Anchor[] value) => anchors = value;

        public bool TryGet(string character, out Anchor anchor)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                if (string.Equals(anchors[i].character, character, StringComparison.OrdinalIgnoreCase))
                {
                    anchor = anchors[i];
                    return true;
                }
            }
            anchor = default;
            return false;
        }

        // Cuelga el sombrero del HatSocket del personaje con la escala de su ancla (en el mundo, aunque el hueso escale).
        public static void Attach(Transform socket, Transform hat, Anchor anchor)
        {
            hat.SetParent(socket, false);
            hat.localPosition = Vector3.zero;
            hat.localRotation = Quaternion.identity;
            hat.localScale = Vector3.one * (anchor.scale / Mathf.Max(socket.lossyScale.x, 1e-4f));
        }
    }
}
