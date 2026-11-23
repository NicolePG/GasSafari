using GasSafari.Modes;
using UnityEngine;

namespace GasSafari.Editor
{
    // Coco Caliente (M7-08): la piscina redonda de Rey de la Balsa (radio 10 m, baldosas naranjas) con la balsa compartida
    // en el centro y el coco-bomba (cáscara, mecha y chispa, sin colisión) que sigue a quien lo tiene.
    public static partial class PartyArenaBuilder
    {
        private static void BuildCoconut(PartyGameData data)
        {
            var scene = Begin();
            try
            {
                const float radius = 10f;
                var root = Common(data, scene, out var spawner, new Vector3(0f, 14.5f, -12.5f), new Vector3(0f, 0f, -0.8f));
                var game = root.AddComponent<CocoCalienteGame>();
                RoundPool(root.transform, radius, Mat("M_Party_DeckStripeOrange", new Color(1f, 0.6f, 0.2f)));
                var center = Marker(root.transform, "Center", new Vector3(0f, 0.3f, 0f), Quaternion.identity);
                game.Configure(data, spawner, Balance);
                game.ConfigureArena(center, Coconut(root.transform));
                Save(scene, data.sceneName);
            }
            finally
            {
                End(scene);
            }
        }

        private static HotCoconut Coconut(Transform parent)
        {
            var go = new GameObject("HotCoconut");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            Visual(go.transform, PrimitiveType.Sphere, "Shell", Vector3.zero, new Vector3(0.75f, 0.7f, 0.75f), Mat("M_Party_Coconut", new Color(0.45f, 0.28f, 0.14f)));
            Visual(go.transform, PrimitiveType.Cylinder, "Fuse", new Vector3(0f, 0.45f, 0f), new Vector3(0.07f, 0.12f, 0.07f), Mat("M_Party_Black", new Color(0.12f, 0.12f, 0.15f)));
            Visual(go.transform, PrimitiveType.Sphere, "Spark", new Vector3(0f, 0.6f, 0f), new Vector3(0.18f, 0.18f, 0.18f), Mat("M_Party_Spark", new Color(1f, 0.85f, 0.2f)));
            var coconut = go.AddComponent<HotCoconut>();
            coconut.Configure(go.transform.Find("Shell").GetComponent<Renderer>(), go.transform.Find("Spark"));
            return coconut;
        }
    }
}
