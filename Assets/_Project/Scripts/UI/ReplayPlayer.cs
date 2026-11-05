using GasSafari.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GasSafari.UI
{
    // Repetición "¿Quién fue?" (docs/06 §4): 3 s del peor choque en bucle, vistos por una cámara propia dentro de
    // la pantalla de resultados, con un marcador sobre la balsa del color del jugador cuyo pedo lo causó.
    public class ReplayPlayer : MonoBehaviour
    {
        private const int TEXTURE_WIDTH = 640;
        private const int TEXTURE_HEIGHT = 360;
        private const float RAFT_THICKNESS = 0.3f;
        private const float MARKER_HEIGHT = 2.2f;
        private static readonly Vector3 CameraOffset = new Vector3(0f, 9f, -9f);

        [SerializeField] private RawImage view;
        [SerializeField] private float raftDiameter = 4f;

        private ReplayClip clip;
        private RenderTexture texture;
        private Camera replayCamera;
        private Transform raft;
        private Transform marker;
        private float time;

        public bool IsPlaying => clip != null;
        public Transform Raft => raft;

        // culpritColor y culpritLabel: el jugador que causó el choque ("J2").
        public void Play(ReplayClip replay, Color culpritColor, string culpritLabel)
        {
            Stop();
            clip = replay;
            time = 0f;
            texture = new RenderTexture(TEXTURE_WIDTH, TEXTURE_HEIGHT, 16);
            view.texture = texture;
            view.gameObject.SetActive(true);

            var cameraObject = new GameObject("ReplayCamera");
            replayCamera = cameraObject.AddComponent<Camera>();
            replayCamera.targetTexture = texture;
            replayCamera.clearFlags = CameraClearFlags.Skybox;

            var raftObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            raftObject.name = "ReplayRaft";
            Destroy(raftObject.GetComponent<Collider>());
            raftObject.transform.localScale = new Vector3(raftDiameter, RAFT_THICKNESS, raftDiameter);
            raft = raftObject.transform;

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObject.name = "ReplayCulprit";
            Destroy(markerObject.GetComponent<Collider>());
            markerObject.transform.localScale = Vector3.one * 0.8f;
            markerObject.GetComponent<Renderer>().material.color = culpritColor;
            var label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(markerObject.transform, false);
            label.transform.localPosition = Vector3.up * 1.2f;
            label.text = culpritLabel;
            label.fontSize = 6f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = culpritColor;
            marker = markerObject.transform;
            Step();
        }

        public void Stop()
        {
            clip = null;
            if (replayCamera != null)
                Destroy(replayCamera.gameObject);
            if (raft != null)
                Destroy(raft.gameObject);
            if (marker != null)
                Destroy(marker.gameObject);
            if (texture != null)
                texture.Release();
            texture = null;
            if (view != null)
                view.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Stop();
        }

        private void Update()
        {
            if (clip == null)
                return;
            time += Time.unscaledDeltaTime;
            if (time > clip.Duration)
                time = 0f;
            Step();
        }

        private void LateUpdate()
        {
            if (clip == null || marker == null)
                return;
            // El texto del marcador mira a la cámara de la repetición.
            marker.GetChild(0).rotation = replayCamera.transform.rotation;
        }

        private void Step()
        {
            clip.Evaluate(time, out var position, out var rotation);
            raft.SetPositionAndRotation(position, rotation);
            marker.position = position + Vector3.up * MARKER_HEIGHT;
            replayCamera.transform.position = position + CameraOffset;
            replayCamera.transform.LookAt(position);
        }
    }
}
