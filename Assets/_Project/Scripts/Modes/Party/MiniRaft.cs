using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Mini balsa individual de Fiesta (skill game-modes: la balsa de siempre con radio 1 m y 1 asiento): mismo prefab y
    // misma física, con el radio de asientos propio, el casco a escala y el modelo MiniRaft_J<n> ajustado al radio.
    public static class MiniRaft
    {
        public const string MODEL_NAME = "MiniModel";

        public static void Shrink(RaftController raft, float radius, GameObject model)
        {
            float scale = radius / raft.Balance.raftRadius;
            raft.SetRadius(radius);
            var body = raft.transform.Find("Body");
            if (body != null)
                body.localScale = new Vector3(body.localScale.x * scale, body.localScale.y, body.localScale.z * scale);

            var big = raft.transform.Find("Model");
            // Cuánto sobresale el modelo grande de su radio de asientos: la mini guarda la misma proporción.
            float bigExtent = big != null ? HorizontalExtent(big) : 0f;
            // La máscara de agua de la balsa grande dejaría un hueco de 2 m: la mini trae la suya.
            var mask = raft.GetComponent<RaftWaterMask>();
            if (mask != null && mask.Mask != null)
                Object.Destroy(mask.Mask);
            if (big != null)
                big.gameObject.SetActive(false);
            var skin = raft.transform.Find(Progression.CosmeticsApplier.SKIN_NAME);
            if (skin != null)
                Object.Destroy(skin.gameObject);
            if (model == null)
                return;
            var instance = Object.Instantiate(model, raft.transform);
            instance.name = MODEL_NAME;
            instance.transform.localPosition = big != null ? big.localPosition : Vector3.zero;
            instance.transform.localRotation = big != null ? big.localRotation : Quaternion.identity;
            float miniExtent = HorizontalExtent(instance.transform);
            if (bigExtent > 0f && miniExtent > 0f)
                instance.transform.localScale *= bigExtent * scale / miniExtent;
        }

        // Los tubos (Chamber_*) del color del jugador: el modelo J3 es amarillo y la paleta dice verde (y existe la paleta
        // para daltónicos), así la balsa siempre coincide con el marcador.
        public static void Tint(RaftController raft, Color color)
        {
            var model = raft.transform.Find(MODEL_NAME);
            if (model == null)
                return;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.name.StartsWith("Chamber"))
                    continue;
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColor, color);
                renderer.SetPropertyBlock(block);
            }
        }

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // Mitad del ancho horizontal de los renderers (sin la máscara de agua), en metros del mundo.
        private static float HorizontalExtent(Transform root)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == RaftWaterMask.MASK_NAME)
                    continue;
                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }
            return any ? Mathf.Max(bounds.extents.x, bounds.extents.z) : 0f;
        }
    }
}
