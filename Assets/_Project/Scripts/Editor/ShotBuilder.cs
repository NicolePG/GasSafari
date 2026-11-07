using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GasSafari.Editor
{
    // Toma de cámara con Timeline (momentos estrella): cámara virtual apagada que una pista de activación enciende
    // 'seconds' segundos. Corre en tiempo sin escalar para que la cámara lenta no la estire.
    public static class ShotBuilder
    {
        public static PlayableDirector Create(Transform parent, string name, string timelinePath, float seconds, Vector3 offset,
            out CinemachineVirtualCamera vcam)
        {
            var shotGo = new GameObject(name + "_Camera");
            shotGo.transform.SetParent(parent, false);
            vcam = shotGo.AddComponent<CinemachineVirtualCamera>();
            vcam.Priority = 100;
            var transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
            transposer.m_FollowOffset = offset;
            transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
            vcam.AddCinemachineComponent<CinemachineComposer>();
            shotGo.SetActive(false);

            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timeline, timelinePath);
            }
            foreach (var track in timeline.GetOutputTracks().ToList())
                timeline.DeleteTrack(track);
            var activation = timeline.CreateTrack<ActivationTrack>(null, "Toma");
            var clip = activation.CreateDefaultClip();
            clip.start = 0;
            clip.duration = seconds;
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            var directorGo = new GameObject(name);
            directorGo.transform.SetParent(parent, false);
            var director = directorGo.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.SetGenericBinding(activation, shotGo);
            return director;
        }
    }
}
