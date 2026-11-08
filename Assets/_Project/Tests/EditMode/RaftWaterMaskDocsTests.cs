using GasSafari.Raft;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // docs/09 §2: toda balsa (la de juego, las 6 de la tienda y las mini balsas) lleva máscara de agua.
    public class RaftWaterMaskDocsTests
    {
        private static readonly string[] Folders =
        {
            "Assets/_Project/Prefabs/Raft",
            "Assets/_Project/Prefabs/MiniRaft",
        };

        [Test]
        public void EveryRaftPrefab_HasWaterMask()
        {
            int checkedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", Folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab.GetComponent<RaftWaterMask>(), $"{path} no tiene RaftWaterMask");
                checkedCount++;
            }
            // Balsa de juego + 6 de la tienda + mini balsa y sus 4 colores.
            Assert.GreaterOrEqual(checkedCount, 12, "faltan prefabs de balsa");
        }

        [Test]
        public void WaterMaskShader_Exists()
        {
            Assert.IsNotNull(Shader.Find("GasSafari/SG_WaterMask"));
        }
    }
}
