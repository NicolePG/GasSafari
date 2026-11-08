using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Editor
{
    // Informe de la estructura de los modelos importados (tamaño, partes y su pivote): una línea por FBX en la consola.
    public static class ModelStructureReport
    {
        [MenuItem("Gas Safari/Informes/Estructura de modelos de obstáculos y tutorial")]
        public static void Run()
        {
            foreach (var folder in new[] { "Hazards", "Tutorial", "Cosmetics/Hats" })
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/" + folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var sb = new StringBuilder();
                    var all = model.GetComponentsInChildren<Renderer>(true);
                    if (all.Length == 0)
                        continue;
                    var bounds = all[0].bounds;
                    foreach (var r in all)
                        bounds.Encapsulate(r.bounds);
                    sb.Append($"MODELO {System.IO.Path.GetFileNameWithoutExtension(path)} size=({bounds.size.x:F2},{bounds.size.y:F2},{bounds.size.z:F2}) center=({bounds.center.x:F2},{bounds.center.y:F2},{bounds.center.z:F2}) ");
                    foreach (var t in model.GetComponentsInChildren<Transform>(true).Where(t => t != model.transform && t.parent == model.transform || t.parent != null && t.parent.parent == model.transform))
                    {
                        var rs = t.GetComponentsInChildren<Renderer>(true);
                        string size = "";
                        if (rs.Length > 0)
                        {
                            var b = rs[0].bounds;
                            foreach (var r in rs)
                                b.Encapsulate(r.bounds);
                            size = $"[{b.size.x:F1},{b.size.y:F1},{b.size.z:F1}@{b.center.x:F1},{b.center.y:F1},{b.center.z:F1}]";
                        }
                        sb.Append($"{t.name}{size}(p{t.localPosition.x:F1},{t.localPosition.y:F1},{t.localPosition.z:F1}) ");
                    }
                    Debug.Log(sb.ToString());
                }
            }
        }
    }
}
