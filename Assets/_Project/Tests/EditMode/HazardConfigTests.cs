using System.Reflection;
using GasSafari.Hazards;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    public class HazardConfigTests
    {
        [Test]
        public void Get_ReturnsParameterOrFallback()
        {
            var config = ScriptableObject.CreateInstance<HazardConfig>();
            config.parameters = new[] { new HazardConfig.Parameter { key = "cycle", value = 2.5f } };
            Assert.AreEqual(2.5f, config.Get("cycle"));
            Assert.AreEqual(7f, config.Get("speed", 7f));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void WarningTime_NeverBelowHalfSecond()
        {
            var config = ScriptableObject.CreateInstance<HazardConfig>();
            Assert.AreEqual(HazardConfig.MIN_WARNING_TIME, config.warningTime);
            config.warningTime = 0.1f;
            typeof(HazardConfig).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(config, null);
            Assert.AreEqual(0.5f, config.warningTime);
            Object.DestroyImmediate(config);
        }
    }
}
