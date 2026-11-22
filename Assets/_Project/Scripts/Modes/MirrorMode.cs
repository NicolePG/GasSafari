using GasSafari.Core;
using GasSafari.Level;
using GasSafari.Progression;
using TMPro;
using UnityEngine;

namespace GasSafari.Modes
{
    // Espejo (docs/06 §1): el nivel se ve y se maneja invertido en X (MirrorView), con récords propios y frijolitos x1.5.
    // Decisión provisional: se invierte la imagen y la entrada, no la geometría (ver docs/06, Espejo).
    public class MirrorMode : GameModeBase
    {
        public override string ModeId => ProgressionRules.MODE_MIRROR;
        public MirrorScreen Screen { get; private set; }
        // Textos del mundo que se voltearon para que se lean al derecho.
        public int FlippedTexts { get; private set; }

        protected override void OnLevelLoaded()
        {
            MirrorView.Active = true;
            Screen = gameObject.AddComponent<MirrorScreen>();
        }

        // Al crear la balsa ya existen los textos de los animales (pings): los textos del mundo se voltean en su eje X
        // para que, al invertir la imagen, se lean al derecho.
        protected override void OnRaftSpawned()
        {
            foreach (var text in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Flip(text.transform);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.isRootCanvas)
                    Flip(canvas.transform);
            }
        }

        private void Flip(Transform target)
        {
            var scale = target.localScale;
            scale.x = -scale.x;
            target.localScale = scale;
            FlippedTexts++;
        }

        public override int PreviousBestStars(string levelId)
        {
            return Services.IsReady && Services.Progression != null ? Services.Progression.MirrorBestStars(levelId) : -1;
        }

        public override void BuildResults(LevelResults results)
        {
            base.BuildResults(results);
            float multiplier = ModesConfig.Instance != null ? ModesConfig.Instance.mirrorBeanMultiplier : 1.5f;
            results.Beans = Mathf.RoundToInt(results.Beans * multiplier);
        }

        public override void Commit(LevelData level, LevelResults results)
        {
            if (Services.IsReady && Services.Progression != null)
                Services.Progression.CommitMirror(level, results);
        }

        protected override void OnExit()
        {
            MirrorView.Active = false;
            if (Screen != null)
                Destroy(Screen);
        }
    }
}
