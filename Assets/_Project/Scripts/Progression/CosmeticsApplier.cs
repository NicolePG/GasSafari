using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Level;
using GasSafari.VFX;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GasSafari.Progression
{
    // Muestra en el nivel lo que cada jugador equipó (docs/06 §2): sombrero en la cabeza, color del gas, piel de la balsa del
    // equipo y celebración al cruzar la meta. RaftSpawner lo llama al crear la balsa y cada animal.
    public static class CosmeticsApplier
    {
        public const string HAT_SOCKET = "HatSocket";
        public const string HAT_NAME = "EquippedHat";
        public const string SKIN_NAME = "EquippedSkin";

        public static void ApplyAvatar(PlayerAvatar avatar, PlayerSlot slot)
        {
            var catalog = GameCatalog.Instance;
            if (avatar == null || slot == null || catalog == null)
                return;
            ApplyHat(avatar, slot, catalog);
            ApplyGas(avatar, slot, catalog);
            ApplyCelebration(avatar, slot, catalog);
        }

        private static void ApplyHat(PlayerAvatar avatar, PlayerSlot slot, GameCatalog catalog)
        {
            var hat = catalog.FindCosmetic(slot.Hat);
            if (hat == null || hat.prefab == null || hat.category != CosmeticCategory.Hat)
                return;
            var socket = FindDeep(avatar.transform, HAT_SOCKET);
            if (socket == null)
                return;
            var id = slot.Character != null ? slot.Character.id : string.Empty;
            if (catalog.hatAnchors == null || !catalog.hatAnchors.TryGet(id, out var anchor))
                return;
            var instance = Object.Instantiate(hat.prefab);
            instance.name = HAT_NAME;
            // Los sombreros son solo visuales: no estorban a la física ni al sondeo de la balsa.
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            HatAnchorSet.Attach(socket, instance.transform, anchor);
        }

        private static void ApplyGas(PlayerAvatar avatar, PlayerSlot slot, GameCatalog catalog)
        {
            var gas = catalog.FindCosmetic(slot.Gas);
            if (gas == null || gas.category != CosmeticCategory.Gas || !avatar.TryGetComponent<FartFeedback>(out var feedback))
                return;
            feedback.GasColor = gas.gasColor;
            feedback.GasColorSource = gas.rainbow && Services.IsReady && Services.Cosmetics != null
                ? (System.Func<Color>)(() => Services.Cosmetics.GasColor(gas.id))
                : null;
        }

        private static void ApplyCelebration(PlayerAvatar avatar, PlayerSlot slot, GameCatalog catalog)
        {
            var celebration = catalog.FindCosmetic(slot.Celebration);
            if (celebration == null || celebration.category != CosmeticCategory.Celebration || slot.Character == null)
                return;
            foreach (var entry in celebration.clips)
            {
                if (!string.Equals(entry.character, slot.Character.id, System.StringComparison.OrdinalIgnoreCase) || entry.clip == null)
                    continue;
                var runner = avatar.GetComponent<CelebrationRunner>();
                if (runner == null)
                    runner = avatar.gameObject.AddComponent<CelebrationRunner>();
                runner.Clip = entry.clip;
                return;
            }
        }

        // La balsa del equipo (la elige el anfitrión): se oculta el modelo naranja y se pone la piel equipada.
        public static void ApplyRaft(GameObject raft)
        {
            var catalog = GameCatalog.Instance;
            if (raft == null || catalog == null || !Services.IsReady || Services.Cosmetics == null)
                return;
            var skin = catalog.FindCosmetic(Services.Cosmetics.Equipped(0, CosmeticCategory.Raft));
            if (skin == null || skin.category != CosmeticCategory.Raft || skin.prefab == null || !Services.Cosmetics.IsOwned(skin))
                return;
            var model = raft.transform.Find("Model");
            if (model == null)
                return;
            model.gameObject.SetActive(false);
            var instance = Object.Instantiate(skin.prefab, raft.transform);
            instance.name = SKIN_NAME;
            instance.transform.localPosition = model.localPosition;
            instance.transform.localRotation = model.localRotation;
            instance.SetActive(true);
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
    }

    // Reproduce la celebración equipada cuando la balsa cruza la meta (reemplaza a la de serie) y devuelve el control al
    // Animator cuando termina.
    public class CelebrationRunner : MonoBehaviour
    {
        public AnimationClip Clip { get; set; }
        public bool IsPlaying { get; private set; }

        private PlayableGraph graph;
        private float endTime;

        private void OnEnable()
        {
            GameEvents.OnLevelFinished += OnFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelFinished -= OnFinished;
            Stop();
        }

        private void OnFinished(float time) => Play();

        public void Play()
        {
            if (Clip == null || IsPlaying)
                return;
            var animator = GetComponentInChildren<Animator>();
            if (animator == null)
                return;
            AnimationPlayableUtilities.PlayClip(animator, Clip, out graph);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            endTime = Time.time + Clip.length;
            IsPlaying = true;
        }

        public void Stop()
        {
            if (graph.IsValid())
                graph.Destroy();
            IsPlaying = false;
        }

        private void Update()
        {
            if (IsPlaying && Time.time >= endTime)
                Stop();
        }
    }
}
