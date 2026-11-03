using GasSafari.Raft;
using NUnit.Framework;
using UnityEngine;

namespace GasSafari.Tests
{
    public class FlowReceiverTests
    {
        [Test]
        public void ReduceLateral_ScalesOnlyPerpendicularComponent()
        {
            var accel = new Vector3(3f, 0f, 4f);
            var result = RaftFlowReceiver.ReduceLateral(accel, Vector3.forward, 0.7f);
            Assert.AreEqual(2.1f, result.x, 1e-4f);
            Assert.AreEqual(4f, result.z, 1e-4f);
        }

        [Test]
        public void ReduceLateral_FactorOneIsIdentity()
        {
            var accel = new Vector3(3f, 1f, 4f);
            Assert.AreEqual(accel, RaftFlowReceiver.ReduceLateral(accel, Vector3.right, 1f));
        }
    }
}
