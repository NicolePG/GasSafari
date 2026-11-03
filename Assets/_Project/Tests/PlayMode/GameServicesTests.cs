using System.Collections;
using GasSafari.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class GameServicesTests
    {
        [Test]
        public void Bootstrap_CreatesPersistentServices()
        {
            Assert.IsTrue(Services.IsReady);
            Assert.AreEqual("DontDestroyOnLoad", Services.Root.gameObject.scene.name);
        }

        [UnityTest]
        public IEnumerator SecondInstance_DestroysItself()
        {
            var original = Services.Root;
            var duplicate = new GameObject("Duplicate").AddComponent<GameServices>();
            yield return null;

            Assert.IsTrue(duplicate == null);
            Assert.AreSame(original, Services.Root);
            Assert.AreEqual(1, Object.FindObjectsByType<GameServices>(FindObjectsSortMode.None).Length);
        }
    }
}
