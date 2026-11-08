using GasSafari.Core;
using GasSafari.Hazards;
using UnityEngine;

namespace GasSafari.Editor
{
    // Collider simple (una caja ajustada a la malla) para los props con los que puede chocar la balsa.
    // Lo usan los constructores de nivel y la corrección de CollisionAudit, para que un nivel regenerado quede igual.
    public static class PropColliders
    {
        // Caja del tamaño de las mallas del prop, en el espacio local de 'root'. Capa Environment (sólido fijo).
        public static BoxCollider AddSolidBox(GameObject root)
        {
            if (!TryLocalBounds(root, out var bounds))
                return null;
            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            box.isTrigger = false;
            root.layer = GameLayers.Environment;
            return box;
        }

        // Objeto suelto que flota y se aparta cuando la balsa lo empuja (decorado en el cauce): masa baja para que un
        // empujón no dañe cámaras (RaftChambers cuenta el impulso del choque).
        public static void MakeLightFloater(GameObject root, float mass)
        {
            var box = AddSolidBox(root);
            if (box == null)
                return;
            root.layer = GameLayers.Hazard;
            var body = root.GetComponent<Rigidbody>();
            if (body == null)
                body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.None;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (root.GetComponent<FloatingBody>() == null)
                root.AddComponent<FloatingBody>();
        }

        private static bool TryLocalBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;
                var b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var local = root.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!any)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }
            return any;
        }
    }
}
