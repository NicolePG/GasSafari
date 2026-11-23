using GasSafari.Raft;
using UnityEngine;

namespace GasSafari.Modes
{
    // Mini balsa individual de Fiesta (skill game-modes: la balsa de siempre con radio 1 m y 1 asiento): mismo prefab y
    // misma física, con el radio de asientos propio, el casco a escala y el modelo MiniRaft_J<n>.
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

            // La máscara de agua de la balsa grande dejaría un hueco de 2 m: la mini trae la suya.
            var mask = raft.GetComponent<RaftWaterMask>();
            if (mask != null && mask.Mask != null)
                Object.Destroy(mask.Mask);
            var big = raft.transform.Find("Model");
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
        }
    }
}
