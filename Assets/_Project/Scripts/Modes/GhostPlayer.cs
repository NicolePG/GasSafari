using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Save;
using UnityEngine;

namespace GasSafari.Modes
{
    // Reproduce un fantasma (docs/06 §1): copia semitransparente de las mallas de la balsa, sin colisiones, en la capa Ghost.
    public class GhostPlayer : MonoBehaviour
    {
        // Después de su llegada el fantasma se queda en la meta un momento y se esconde.
        private const float LINGER_SECONDS = 2f;

        private GhostData data;
        private LevelTimer timer;
        private GameObject visual;
        private Material material;

        public GhostData Data => data;
        public bool IsVisible => visual != null && visual.activeSelf;
        public int MeshCount { get; private set; }

        public void Setup(GhostData ghost, LevelTimer levelTimer, Transform raft, Material template, Color color)
        {
            data = ghost;
            timer = levelTimer;
            material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            visual = new GameObject("GhostVisual");
            visual.transform.SetParent(transform, false);
            if (raft != null)
                CopyMeshes(raft);
            SetLayer(gameObject, GameLayers.Ghost);
            Refresh();
        }

        // Solo las mallas de la balsa: los animales sentados son hijos de la balsa y se dejan fuera.
        private void CopyMeshes(Transform raft)
        {
            var raftScale = raft.lossyScale;
            foreach (var filter in raft.GetComponentsInChildren<MeshFilter>())
            {
                var source = filter.GetComponent<MeshRenderer>();
                if (source == null || !source.enabled || filter.sharedMesh == null || filter.GetComponentInParent<PlayerAvatar>() != null)
                    continue;
                var copy = new GameObject(filter.name);
                copy.transform.SetParent(visual.transform, false);
                copy.transform.localPosition = raft.InverseTransformPoint(filter.transform.position);
                copy.transform.localRotation = Quaternion.Inverse(raft.rotation) * filter.transform.rotation;
                var scale = filter.transform.lossyScale;
                copy.transform.localScale = new Vector3(scale.x / raftScale.x, scale.y / raftScale.y, scale.z / raftScale.z);
                copy.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var ghostRenderer = copy.AddComponent<MeshRenderer>();
                var materials = new Material[Mathf.Max(1, source.sharedMaterials.Length)];
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;
                ghostRenderer.sharedMaterials = materials;
                ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ghostRenderer.receiveShadows = false;
                MeshCount++;
            }
        }

        private static void SetLayer(GameObject root, int layer)
        {
            if (layer < 0)
                return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (data == null || timer == null)
                return;
            float t = timer.RunTime;
            data.Sample(t, out var position, out var rotation);
            transform.SetPositionAndRotation(position, rotation);
            bool show = data.Count > 0 && t <= data.Duration + LINGER_SECONDS;
            if (visual.activeSelf != show)
                visual.SetActive(show);
        }

        private void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }
    }
}
