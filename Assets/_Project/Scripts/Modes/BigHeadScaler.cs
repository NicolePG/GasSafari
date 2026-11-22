using UnityEngine;

namespace GasSafari.Modes
{
    // Modificador Cabezones (docs/06 §1, cosmético): agranda la cabeza del animal después de la animación de cada cuadro.
    public class BigHeadScaler : MonoBehaviour
    {
        private Transform head;
        private Vector3 baseScale;
        private float factor = 2f;

        public Transform Head => head;

        public void Setup(float scale)
        {
            factor = scale;
            head = FindHead(transform);
            if (head != null)
                baseScale = head.localScale;
        }

        // Hueso Head del avatar Humanoid; si no es Humanoid, el primer hueso llamado "head" más cerca de la raíz.
        public static Transform FindHead(Transform root)
        {
            var animator = root.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                var bone = animator.GetBoneTransform(HumanBodyBones.Head);
                if (bone != null)
                    return bone;
            }
            Transform best = null;
            int bestDepth = int.MaxValue;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name.ToLowerInvariant();
                if (!name.Contains("head") || name.Contains("end") || name.Contains("top"))
                    continue;
                int depth = 0;
                for (var p = t; p != root && p != null; p = p.parent)
                    depth++;
                if (depth < bestDepth)
                {
                    best = t;
                    bestDepth = depth;
                }
            }
            return best;
        }

        private void LateUpdate()
        {
            if (head != null)
                head.localScale = baseScale * factor;
        }

        private void OnDestroy()
        {
            if (head != null)
                head.localScale = baseScale;
        }
    }
}
