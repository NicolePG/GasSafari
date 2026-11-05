using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Informe de los FBX de personajes y props: rig, clips, altura y triángulos (docs/09 §3).
    public static class ModelReport
    {
        [MenuItem("Gas Safari/Informes/Modelos importados")]
        public static void Run()
        {
            var sb = new StringBuilder("MODEL-REPORT\n");
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/Characters", "Assets/_Project/Art/Props" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var renderers = root.GetComponentsInChildren<Renderer>();
                var bounds = new Bounds();
                bool first = true;
                int tris = 0;
                foreach (var r in renderers)
                {
                    if (first) { bounds = r.bounds; first = false; }
                    else bounds.Encapsulate(r.bounds);
                    if (r is SkinnedMeshRenderer s && s.sharedMesh != null)
                        tris += s.sharedMesh.triangles.Length / 3;
                    else if (r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
                        tris += mf.sharedMesh.triangles.Length / 3;
                }
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                sb.AppendLine($"{path}: anim={importer.animationType} scale={importer.globalScale} avatar={(avatar != null ? (avatar.isValid ? "valid" : "invalid") : "none")} human={(avatar != null && avatar.isHuman)} size={bounds.size.ToString("F2")} min.y={bounds.min.y:F2} tris={tris} mats={string.Join(",", renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct())}");
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
                    sb.AppendLine($"   clip {clip.name} {clip.length:F2}s loop={clip.isLooping} frames={clip.frameRate}");
                var bones = root.GetComponentsInChildren<Transform>().Select(t => t.name);
                sb.AppendLine("   nodes: " + string.Join(" ", bones.Take(80)));
            }
            foreach (var line in sb.ToString().Split('\n'))
                if (line.Length > 0)
                    Debug.Log("MR| " + line); // la consola solo devuelve la primera línea de cada mensaje
        }
    }
}
