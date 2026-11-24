using Cinemachine;
using GasSafari.Core;
using GasSafari.Raft;
using GasSafari.VFX;
using UnityEngine;

namespace GasSafari.Modes
{
    // Pantalla dividida vertical de Carrera por Equipos local (docs/02 §12, docs/06 §1): la cámara del nivel pasa a la mitad
    // izquierda y sigue a la balsa Azul; una copia de la cámara y de su RaftCameraRig (M1-15, mismas distancias, inclinación
    // y giro) dibuja la mitad derecha con la balsa Naranja. Con Cinemachine 2.x cada CinemachineBrain usa solo las cámaras
    // virtuales cuya capa ve, así que cada cámara virtual va en su capa (TeamCamA / TeamCamB) y la otra cámara no la ve.
    public class TeamSplitScreen : MonoBehaviour
    {
        public Camera LeftCamera { get; private set; }
        public Camera RightCamera { get; private set; }
        public RaftCameraRig LeftRig { get; private set; }
        public RaftCameraRig RightRig { get; private set; }
        public bool IsSplit => RightCamera != null;

        public bool Build(RaftController left, RaftController right)
        {
            var main = Camera.main;
            var rig = FindFirstObjectByType<RaftCameraRig>();
            int layerA = LayerMask.NameToLayer(GameLayers.TEAM_CAMERA_A);
            int layerB = LayerMask.NameToLayer(GameLayers.TEAM_CAMERA_B);
            if (main == null || rig == null || rig.Vcam == null || layerA < 0 || layerB < 0)
            {
                Debug.LogWarning("TeamSplitScreen: falta la cámara, el rig o las capas TeamCamA/TeamCamB (Gas Safari/Setup/Aplicar configuración del proyecto)");
                return false;
            }

            LeftCamera = main;
            LeftRig = rig;
            rig.Bind(left);

            var cameraCopy = Instantiate(main.gameObject, main.transform.position, main.transform.rotation);
            cameraCopy.name = "Camera_Right";
            cameraCopy.tag = "Untagged";
            var listener = cameraCopy.GetComponent<AudioListener>();
            if (listener != null)
                DestroyImmediate(listener);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraCopy, main.gameObject.scene);
            RightCamera = cameraCopy.GetComponent<Camera>();

            var rigCopy = Instantiate(rig.gameObject, rig.transform.position, rig.transform.rotation);
            rigCopy.name = rig.gameObject.name + "_Right";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rigCopy, rig.gameObject.scene);
            RightRig = rigCopy.GetComponent<RaftCameraRig>();
            RightRig.Bind(right);
            RightRig.Vcam.transform.SetPositionAndRotation(rig.Vcam.transform.position, rig.Vcam.transform.rotation);

            SetLayer(rig.Vcam.gameObject, layerA);
            SetLayer(RightRig.Vcam.gameObject, layerB);
            LeftCamera.cullingMask = (LeftCamera.cullingMask | (1 << layerA)) & ~(1 << layerB);
            RightCamera.cullingMask = (RightCamera.cullingMask | (1 << layerB)) & ~(1 << layerA);
            LeftCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
            RightCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);

            // Los iconos de caídos fuera de cámara se calculan sobre la pantalla entera con Camera.main: en media pantalla
            // apuntarían mal, así que no se usan en la carrera (provisional, docs/06).
            DisableEdgeMarkers(rig.gameObject);
            DisableEdgeMarkers(rigCopy);
            return true;
        }

        // Cámara que muestra esa balsa (para pasar de mundo a pantalla).
        public Camera CameraFor(int team) => team == 1 && RightCamera != null ? RightCamera : LeftCamera;

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        private static void DisableEdgeMarkers(GameObject rig)
        {
            foreach (var markers in rig.GetComponentsInChildren<UI.SwimmerEdgeMarkers>(true))
                markers.enabled = false;
        }

        private void OnDestroy()
        {
            if (RightCamera != null)
                Destroy(RightCamera.gameObject);
            if (RightRig != null)
                Destroy(RightRig.gameObject);
            if (LeftCamera != null)
                LeftCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
