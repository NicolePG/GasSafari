using System.Collections;
using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    // Máscara de agua: con el piso por debajo de la superficie, el agua no se dibuja dentro de la balsa.
    public class RaftWaterMaskTests
    {
        private const string RAFT_PREFAB = "Assets/_Project/Prefabs/Raft/Raft.prefab";
        private const string WATER_MATERIAL = "Assets/_Project/Art/Materials/World/M_Water_W1.mat";

        private GameObject water;
        private GameObject raft;
        private GameObject cameraObject;
        private RenderTexture target;

        [TearDown]
        public void TearDown()
        {
            if (water != null) Object.Destroy(water);
            if (raft != null) Object.Destroy(raft);
            if (cameraObject != null) Object.Destroy(cameraObject);
            if (target != null) target.Release();
        }

        private Camera BuildScene(bool requestDepth)
        {
#if UNITY_EDITOR
            water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "MaskTestWater";
            water.transform.localScale = new Vector3(5f, 1f, 5f);
            Object.Destroy(water.GetComponent<Collider>());
            water.GetComponent<Renderer>().sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(WATER_MATERIAL);

            // Piso 0.2 m bajo la superficie: sin máscara, el agua cubre el interior de la balsa.
            raft = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RAFT_PREFAB), new Vector3(0f, -0.1f, 0f), Quaternion.identity);
            foreach (var behaviour in raft.GetComponents<MonoBehaviour>())
            {
                if (!(behaviour is RaftWaterMask))
                    behaviour.enabled = false;
            }
            raft.GetComponent<Rigidbody>().isKinematic = true;
#endif
            cameraObject = new GameObject("MaskTestCamera");
            var cam = cameraObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 40f;
            cam.transform.position = new Vector3(0f, 6f, -5f);
            cam.transform.LookAt(new Vector3(0f, 0f, 0f));
            var data = cam.GetUniversalAdditionalCameraData();
            data.requiresDepthOption = requestDepth ? CameraOverrideOption.On : CameraOverrideOption.Off;
            target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = target;
            return cam;
        }

        private Color SampleCenter(Camera cam)
        {
            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            // Promedio de un cuadro pequeño sobre el piso (centro de la balsa).
            Vector3 screen = cam.WorldToScreenPoint(new Vector3(0f, 0.3f, 0f));
            int cx = Mathf.RoundToInt(screen.x), cy = Mathf.RoundToInt(screen.y);
            Color sum = Color.black;
            int count = 0;
            for (int x = cx - 6; x <= cx + 6; x++)
            {
                for (int y = cy - 6; y <= cy + 6; y++)
                {
                    sum += texture.GetPixel(x, y);
                    count++;
                }
            }
            Object.Destroy(texture);
            return sum / count;
        }

        private static float Distance(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        [UnityTest, Timeout(60000)]
        public IEnumerator Mask_HidesWaterInsideRaft()
        {
            var cam = BuildScene(true);
            yield return null;
            yield return null;

            var mask = raft.GetComponent<RaftWaterMask>().Mask;
            Assert.IsNotNull(mask, "la máscara se arma al empezar");

            var withMask = SampleCenter(cam);
            water.SetActive(false);
            var noWater = SampleCenter(cam);
            water.SetActive(true);
            mask.SetActive(false);
            var withoutMask = SampleCenter(cam);

            Debug.Log($"RAFTMASK conMascara={withMask} sinAgua={noWater} sinMascara={withoutMask}");
            Assert.Less(Distance(withMask, noWater), 0.05f, "con la máscara el interior se ve igual que sin agua");
            Assert.Greater(Distance(withoutMask, noWater), 0.1f, "sin la máscara el agua cubre el interior (la prueba detecta la diferencia)");
        }

        // Las 6 balsas de la tienda y las mini balsas arman su máscara dentro del bote (radio y altura razonables).
        [UnityTest, Timeout(60000)]
        public IEnumerator ShopSkinsAndMiniRafts_BuildMaskInsideBoat()
        {
#if UNITY_EDITOR
            int count = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Raft/Skins", "Assets/_Project/Prefabs/MiniRaft" }))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var instance = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path));
                yield return null;
                var mask = instance.GetComponent<RaftWaterMask>().Mask;
                Assert.IsNotNull(mask, $"{path}: la máscara se arma");
                var bounds = mask.GetComponent<MeshRenderer>().bounds;
                var whole = new Bounds(instance.GetComponentInChildren<Renderer>().bounds.center, Vector3.zero);
                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    if (r.gameObject != mask)
                        whole.Encapsulate(r.bounds);
                }
                Debug.Log($"RAFTSKINS {path} mascara={bounds.size.x:F2}x{bounds.size.y:F2}x{bounds.size.z:F2} balsa={whole.size.x:F2}x{whole.size.y:F2}x{whole.size.z:F2}");
                Assert.Less(bounds.size.x, whole.size.x, $"{path}: la máscara cabe dentro de la balsa");
                Assert.Greater(bounds.size.x, whole.size.x * 0.3f, $"{path}: la máscara cubre el interior");
                Object.Destroy(instance);
                count++;
            }
            Assert.AreEqual(11, count);
#else
            yield break;
#endif
        }

        // Diagnóstico del agua casi blanca: la misma escena con y sin textura de profundidad en la cámara.
        [UnityTest, Timeout(60000)]
        public IEnumerator Telemetry_WaterColor_WithAndWithoutDepthTexture()
        {
            var cam = BuildScene(true);
            raft.SetActive(false);
            cam.transform.position = new Vector3(0f, 6f, -5f);
            yield return null;
            yield return null;
            var withDepth = SampleCenter(cam);

            cam.GetUniversalAdditionalCameraData().requiresDepthOption = CameraOverrideOption.Off;
            yield return null;
            var withoutDepth = SampleCenter(cam);
            Debug.Log($"RAFTMASK aguaConProfundidad={withDepth} aguaSinProfundidad={withoutDepth}");
        }
    }
}
