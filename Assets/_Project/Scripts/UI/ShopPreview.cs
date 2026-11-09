using GasSafari.Characters;
using GasSafari.Progression;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GasSafari.UI
{
    // Vista previa 3D giratoria de la tienda (docs/06 §2, docs/07 §2): una cámara aparte, lejos de todo, dibuja en una textura
    // el sombrero puesto en el animal, la balsa o el animal bailando su celebración.
    public class ShopPreview : MonoBehaviour
    {
        private const int SIZE = 640;
        private static readonly Vector3 StageOrigin = new Vector3(4000f, 0f, 4000f);

        private Camera cam;
        private Transform camTransform;
        private Transform stage;
        private GameObject current;
        private Animator animator;
        private PlayableGraph graph;
        private AnimationClipPlayable clipPlayable;
        private bool hasClip;
        private float clipLength;
        private bool spin;

        public RenderTexture Texture { get; private set; }
        public GameObject Current => current;
        public bool IsPlayingClip => hasClip;

        public static ShopPreview Create(Transform parent)
        {
            var go = new GameObject("ShopPreview");
            go.transform.SetParent(parent, false);
            var preview = go.AddComponent<ShopPreview>();
            preview.Build();
            return preview;
        }

        private void Build()
        {
            Texture = new RenderTexture(SIZE, SIZE, 24, RenderTextureFormat.ARGB32) { name = "ShopPreview" };
            stage = new GameObject("Stage").transform;
            stage.SetParent(transform, false);
            stage.position = StageOrigin;

            var camObject = new GameObject("PreviewCamera");
            camObject.transform.SetParent(stage, false);
            cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.36f, 1f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.targetTexture = Texture;
            cam.depth = -50f;
            cam.enabled = false;
            camTransform = camObject.transform;
            Frame(1.8f);
            var audio = camObject.GetComponent<AudioListener>();
            if (audio != null)
                Destroy(audio);
        }

        // Encuadra un animal de 'height' m de pie: cabe entero, con aire arriba y abajo.
        private void Frame(float height)
        {
            camTransform.localPosition = new Vector3(0f, height * 0.56f, -height * 2.7f);
            camTransform.localRotation = Quaternion.identity;
        }

        private void FrameRaft()
        {
            camTransform.localPosition = new Vector3(0f, 1.3f, -6.4f);
            camTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        }

        private void OnEnable()
        {
            if (cam != null)
                cam.enabled = true;
        }

        private void OnDisable()
        {
            if (cam != null)
                cam.enabled = false;
            Clear();
        }

        private void OnDestroy()
        {
            Clear();
            if (Texture != null)
            {
                Texture.Release();
                Destroy(Texture);
            }
        }

        private void Clear()
        {
            if (graph.IsValid())
                graph.Destroy();
            hasClip = false;
            if (current != null)
                Destroy(current);
            current = null;
            animator = null;
        }

        // Muestra el artículo. 'wearer' es el animal que lo lleva (sombrero, gas y celebración).
        public void Show(CosmeticData item, CharacterData wearer, HatAnchorSet anchors)
        {
            Clear();
            if (item == null)
                return;

            spin = true;
            switch (item.category)
            {
                case CosmeticCategory.Raft:
                    ShowRaft(item);
                    break;
                case CosmeticCategory.Hat:
                    ShowAnimal(wearer, item, anchors, null);
                    break;
                case CosmeticCategory.Celebration:
                    spin = false;
                    ShowAnimal(wearer, null, anchors, item);
                    break;
                default:
                    ShowAnimal(wearer, null, anchors, null);
                    break;
            }
        }

        private void ShowRaft(CosmeticData item)
        {
            var source = item.prefab != null ? item.prefab : item.previewPrefab;
            if (source == null)
                return;
            FrameRaft();
            current = Instantiate(source, stage);
            current.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            current.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            current.transform.localScale = Vector3.one * 1.1f;
            StripPhysics(current);
            current.SetActive(true);
        }

        private void ShowAnimal(CharacterData wearer, CosmeticData hat, HatAnchorSet anchors, CosmeticData celebration)
        {
            if (wearer == null || wearer.prefab == null)
                return;
            var model = wearer.prefab.transform.Find("Model");
            if (model == null)
                return;
            current = Instantiate(model.gameObject, stage);
            current.name = "PreviewAnimal";
            Frame(Mathf.Max(wearer.height, 1f));
            current.transform.localPosition = Vector3.zero;
            current.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);
            current.transform.localScale = Vector3.one;
            current.SetActive(true);
            animator = current.GetComponent<Animator>();

            if (hat != null && hat.prefab != null)
                AttachHat(wearer, hat, anchors);
            if (celebration != null)
                PlayCelebration(celebration, wearer);
            else
                PlaySelectPose(wearer);
        }

        private void AttachHat(CharacterData wearer, CosmeticData hat, HatAnchorSet anchors)
        {
            var socket = FindDeep(current.transform, "HatSocket");
            if (socket == null || anchors == null || !anchors.TryGet(wearer.id, out var anchor))
                return;
            var instance = Instantiate(hat.prefab);
            StripPhysics(instance);
            HatAnchorSet.Attach(socket, instance.transform, anchor);
        }

        private void PlayCelebration(CosmeticData item, CharacterData wearer)
        {
            if (animator == null)
                return;
            foreach (var entry in item.clips)
            {
                if (!string.Equals(entry.character, wearer.id, System.StringComparison.OrdinalIgnoreCase) || entry.clip == null)
                    continue;
                clipLength = entry.clip.length;
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                AnimationPlayableUtilities.PlayClip(animator, entry.clip, out graph);
                graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
                clipPlayable = (AnimationClipPlayable)graph.GetOutput(0).GetSourcePlayable();
                hasClip = true;
                return;
            }
        }

        private void PlaySelectPose(CharacterData wearer)
        {
            // Sin controlador la pose de la tienda es la postura neutra del modelo; con el controlador del personaje, su reposo.
            if (animator != null && wearer.overrideController != null)
                animator.runtimeAnimatorController = wearer.overrideController;
        }

        private void Update()
        {
            if (current == null)
                return;
            if (spin)
                current.transform.Rotate(0f, 36f * Time.unscaledDeltaTime, 0f, Space.World);
            if (hasClip && graph.IsValid())
            {
                // La celebración no hace bucle sola: se repite con una pausa corta.
                if (clipPlayable.GetTime() >= clipLength + 0.4f)
                    clipPlayable.SetTime(0.0);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        // Quita lo que no se necesita en una vista previa: cuerpos rígidos, colisionadores y scripts.
        private static void StripPhysics(GameObject instance)
        {
            foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true))
                body.isKinematic = true;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }
    }
}
