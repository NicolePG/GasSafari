using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GasSafari.Tests
{
    // docs/09 §2: SG_Toon y SG_ToonWater compilan y exponen sus propiedades; el URP Asset trae Depth Texture.
    public class ToonShadersTests
    {
        [TestCase("GasSafari/SG_Toon", "_BaseColor", "_ShadowColor", "_RimStrength")]
        [TestCase("GasSafari/SG_ToonWater", "_ShallowColor", "_DeepColor", "_FlowDir")]
        public void Shader_IsSupportedAndHasProperties(string shaderName, params string[] properties)
        {
            var shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, shaderName);
            Assert.IsTrue(shader.isSupported, shaderName);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), shaderName);
            foreach (var property in properties)
                Assert.GreaterOrEqual(shader.FindPropertyIndex(property), 0, property);
        }

        [Test]
        public void UrpAsset_HasDepthAndOpaqueTexture()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("Assets/_Project/Settings/GasSafari_URP.asset");
            var so = new SerializedObject(asset);
            Assert.IsTrue(so.FindProperty("m_RequireDepthTexture").boolValue);
            Assert.IsTrue(so.FindProperty("m_RequireOpaqueTexture").boolValue);
        }
    }
}
