using System.Collections;
using GasSafari.Characters;
using GasSafari.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class RaftCameraRigTests
    {
        private const string RIG_PREFAB = "Assets/_Project/Prefabs/Level/RaftCameraRig.prefab";

        private RaftTestRig rig;
        private RaftCameraRig cameraRig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RIG_PREFAB);
            cameraRig = Object.Instantiate(prefab).GetComponent<RaftCameraRig>();
#endif
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(cameraRig.gameObject);
            rig.Destroy();
        }

        [UnityTest]
        public IEnumerator Swimmer_JoinsGroupAndZoomsOut_ThenReturns()
        {
            var input = rig.AddAvatar(180f);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(rig.Balance.cameraDistanceMin, cameraRig.Distance, 0.3f);
            Assert.AreEqual(1, cameraRig.Group.m_Targets.Length);

            var fall = input.GetComponent<AvatarFall>();
            fall.Fall(new Vector3(0f, 0f, -3f));
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(1, cameraRig.TrackedSwimmers);
            Assert.AreEqual(2, cameraRig.Group.m_Targets.Length);
            Assert.AreEqual(rig.Balance.cameraDistanceMax, cameraRig.Distance, 0.5f);

            fall.ReturnToRaft();
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(0, cameraRig.TrackedSwimmers);
            Assert.AreEqual(1, cameraRig.Group.m_Targets.Length);
            Assert.AreEqual(rig.Balance.cameraDistanceMin, cameraRig.Distance, 0.5f);
        }
    }
}
