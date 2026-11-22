using GasSafari.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.Modes
{
    // Modo Espejo: la cámara del nivel dibuja en una textura del tamaño de la pantalla y una imagen a pantalla completa la
    // muestra invertida en X, debajo del HUD (los lienzos del HUD se dibujan encima y no se invierten).
    public class MirrorScreen : MonoBehaviour
    {
        // Debajo de todos los lienzos del juego.
        private const int SORTING_ORDER = -500;

        private Camera target;
        private RenderTexture texture;
        private RawImage image;
        private Canvas canvas;

        public Camera Target => target;
        public RenderTexture Texture => texture;
        public RawImage Image => image;

        private void Awake()
        {
            canvas = UiFactory.CreateCanvas("MirrorScreen", SORTING_ORDER, transform);
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
            image = new GameObject("View", typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(canvas.transform, false);
            UiFactory.Stretch(image.rectTransform);
            image.raycastTarget = false;
            image.uvRect = new Rect(1f, 0f, -1f, 1f);
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        // También al crearse, para que no haya ni un cuadro sin invertir.
        private void Refresh()
        {
            var main = Camera.main;
            if (main != target)
            {
                Release(target);
                target = main;
            }
            image.enabled = target != null;
            if (target == null)
                return;
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null)
                {
                    target.targetTexture = null;
                    texture.Release();
                    Destroy(texture);
                }
                texture = new RenderTexture(width, height, 24) { name = "MirrorView" };
                image.texture = texture;
            }
            if (target.targetTexture != texture)
                target.targetTexture = texture;
        }

        private void Release(Camera camera)
        {
            if (camera != null && camera.targetTexture == texture)
                camera.targetTexture = null;
        }

        private void OnDestroy()
        {
            Release(target);
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
