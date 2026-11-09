using GasSafari.Core;
using GasSafari.Hazards;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Aviso de cada abismo H22 (decisión 2026-11-08): 30 m antes del hueco, una hilera de boyas rojas que cruza el cauce, boyas
    // guía a los lados, un cartel de peligro a cada orilla y el trigger AbyssWarning (aviso de peligro del HUD).
    public static class AbyssSignage
    {
        private const string PREFAB = "Assets/_Project/Prefabs/Hazards/H22_AbyssWarning.prefab";
        private static readonly Color Red = new Color(0.9f, 0.1f, 0.08f);
        private static readonly Color RedGlow = new Color(0.9f, 0.1f, 0.08f) * 2.5f;
        private static readonly Color PostColor = new Color(0.35f, 0.3f, 0.28f);

        // 'from' = donde empieza el hueco. 'id' distingue los nombres de los materiales (una escena puede tener varios abismos).
        public static void Add(GreyboxLevel level, float from, string id)
        {
            float at = Mathf.Max(from - AbyssWarning.DISTANCE_BEFORE, 3f);
            float width = level.WidthAt(at);
            var props = level.GroupOf("Props");

            var zone = level.Place(PREFAB, at, 0f, 0f, 1f);
            zone.name = "AvisoAbismo_" + id;
            zone.GetComponent<BoxCollider>().size = new Vector3(width + 2f, 6f, 3f);

            // Boyas rojas: una hilera que cruza el cauce y guías a los lados hasta el despegue.
            var red = level.ToonMaterial("BoyaPeligro_" + id, Red, RedGlow);
            for (float x = -width * 0.5f + 1.2f; x <= width * 0.5f - 1.1f; x += 2.8f)
                Buoy(props, level.PointAt(at, x, 0.25f), red);
            for (float d = at + 6f; d < from - 5f; d += 6f)
            {
                Buoy(props, level.PointAt(d, -width * 0.5f + 1f, 0.25f), red);
                Buoy(props, level.PointAt(d, width * 0.5f - 1f, 0.25f), red);
            }

            // Carteles de peligro fuera de las paredes, altos para verse por encima de ellas.
            var board = level.ToonMaterial("CartelPeligro_" + id, Red, RedGlow * 0.4f);
            var post = level.ToonMaterial("PosteCartel_" + id, PostColor);
            foreach (float side in new[] { -1f, 1f })
                Sign(level, props, at + 1f, side * (width * 0.5f + 1.6f), board, post);
        }

        private static void Buoy(Transform parent, Vector3 position, Material material)
        {
            var buoy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buoy.name = "BoyaRoja";
            buoy.layer = GameLayers.Environment;
            buoy.transform.SetParent(parent, false);
            buoy.transform.position = position;
            buoy.transform.localScale = new Vector3(0.8f, 0.45f, 0.8f);
            Object.DestroyImmediate(buoy.GetComponent<Collider>()); // marca visual: no frena a la balsa
            buoy.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void Sign(GreyboxLevel level, Transform parent, float d, float lateral, Material board, Material post)
        {
            var yaw = Quaternion.LookRotation(level.DirectionAt(d));
            var root = new GameObject("CartelAbismo");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(level.PointAt(d, lateral, 0f), yaw);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Poste";
            pole.layer = GameLayers.Environment;
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            pole.transform.localScale = new Vector3(0.5f, 2.7f, 0.5f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = post;

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Tablero";
            panel.layer = GameLayers.Environment;
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            panel.transform.localScale = new Vector3(3.6f, 2f, 0.4f);
            panel.GetComponent<MeshRenderer>().sharedMaterial = board;

            // El texto se lee mirando en el sentido del cauce (la balsa se acerca de frente).
            var text = new GameObject("Texto").AddComponent<TMPro.TextMeshPro>();
            text.transform.SetParent(panel.transform, false);
            text.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            text.transform.localScale = new Vector3(1f / 3.6f, 1f / 2f, 1f / 0.4f);
            text.rectTransform.sizeDelta = new Vector2(3.4f, 1.8f);
            text.text = "¡PELIGRO!\nABISMO";
            text.fontSize = 3.4f;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = false;
        }
    }
}
