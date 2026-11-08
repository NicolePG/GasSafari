using System.Collections.Generic;
using System.IO;
using System.Linq;
using GasSafari.Characters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // Auditoría de traspasos (docs/11, M9-09): los 8 sombreros en los 5 personajes quedan sobre la cabeza (sobre su ancla de
    // hat_anchors.json), sin hundirse en ella ni flotar, centrados y de un tamaño razonable.
    public class HatClippingTests
    {
        private const string AVATARS = "Assets/_Project/Prefabs/Characters/Avatar_";
        private const string HATS = "Assets/_Project/Prefabs/Cosmetics/Hats/";
        private static readonly string[] Characters = { "Alex", "Marty", "Melman", "Gloria", "Julien" };

        // Tolerancias (m): el socket sobre su ancla, la base del sombrero sobre la cabeza y el centro del sombrero sobre ella.
        private const float SOCKET_TOLERANCE = 0.12f;
        private const float BASE_TOLERANCE = 0.08f;
        private const float CENTER_TOLERANCE = 0.12f;
        private const float MAX_HAT_WIDTH = 1.0f;

        private static Transform FindDeep(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }

        private static IEnumerable<string> HatPaths()
        {
            return Directory.GetFiles(HATS, "*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p);
        }

        [Test]
        public void EveryHat_SitsOnTheHeadOfEveryCharacter()
        {
            var anchors = AssetDatabase.LoadAssetAtPath<HatAnchorSet>("Assets/_Project/Data/Cosmetics/HatAnchors.asset");
            Assert.IsNotNull(anchors, "falta HatAnchors (menú Crear prefabs de fauna y sombreros)");
            var hats = HatPaths().ToList();
            Assert.AreEqual(8, hats.Count, "los 8 sombreros");

            var problems = new List<string>();
            foreach (var character in Characters)
            {
                Assert.IsTrue(anchors.TryGet(character, out var anchor), $"ancla de {character}");
                var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AVATARS + character + ".prefab");
                Assert.IsNotNull(avatarPrefab, character);
                var avatar = Object.Instantiate(avatarPrefab);
                try
                {
                    avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    var socket = FindDeep(avatar.transform, "HatSocket");
                    Assert.IsNotNull(socket, $"{character} necesita HatSocket");
                    float socketError = Vector3.Distance(socket.position, anchor.headTop);
                    if (socketError > SOCKET_TOLERANCE)
                        problems.Add($"{character}: el HatSocket está a {socketError:F2} m de la cabeza ({socket.position} vs {anchor.headTop})");

                    foreach (var hatPath in hats)
                    {
                        var hat = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(hatPath));
                        try
                        {
                            HatAnchorSet.Attach(socket, hat.transform, anchor);
                            var renderers = hat.GetComponentsInChildren<Renderer>();
                            var bounds = renderers[0].bounds;
                            foreach (var r in renderers)
                                bounds.Encapsulate(r.bounds);
                            string tag = $"{character} + {Path.GetFileNameWithoutExtension(hatPath)}";
                            float baseError = bounds.min.y - anchor.headTop.y;
                            if (Mathf.Abs(baseError) > BASE_TOLERANCE)
                                problems.Add($"{tag}: la base del sombrero queda a {baseError:+0.00;-0.00} m de la cabeza ({(baseError < 0 ? "hundido" : "flotando")})");
                            var offset = new Vector2(bounds.center.x - anchor.headTop.x, bounds.center.z - anchor.headTop.z);
                            if (offset.magnitude > CENTER_TOLERANCE)
                                problems.Add($"{tag}: descentrado {offset.magnitude:F2} m");
                            float width = Mathf.Max(bounds.size.x, bounds.size.z);
                            if (width > MAX_HAT_WIDTH)
                                problems.Add($"{tag}: demasiado ancho ({width:F2} m)");
                        }
                        finally
                        {
                            Object.DestroyImmediate(hat);
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(avatar);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
