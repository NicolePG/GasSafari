using System.Collections;
using GasSafari.Level;
using GasSafari.Raft;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class FlowTests
    {
        private const float ACCEL = 2f;

        private RaftTestRig rig;
        private FlowSpline flow;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var go = new GameObject("TestFlow");
            go.SetActive(false);
            var container = go.AddComponent<SplineContainer>();
            container.Spline = new Spline(new[]
            {
                new BezierKnot(new float3(-50f, 0f, 0f)),
                new BezierKnot(new float3(50f, 0f, 0f))
            });
            flow = go.AddComponent<FlowSpline>();
            flow.Configure(10f, AnimationCurve.Constant(0f, 1f, ACCEL));
            go.SetActive(true);

            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            rig.Get<RaftFlowReceiver>().SetFlow(flow);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
            Object.Destroy(flow.gameObject);
        }

        [Test]
        public void GetFlow_ReturnsTangentAndIntensity()
        {
            int hint = 0;
            flow.GetFlow(new Vector3(20f, 0f, 3f), ref hint, out var direction, out float accel);
            Assert.AreEqual(1f, direction.x, 1e-3f);
            Assert.AreEqual(ACCEL, accel, 1e-4f);

            flow.GetFlow(new Vector3(-49f, 0f, 0f), ref hint, out direction, out _);
            Assert.AreEqual(1f, direction.x, 1e-3f, "también en el extremo");
        }

        [UnityTest]
        public IEnumerator Raft_DriftsWithCurrent()
        {
            yield return new WaitForSeconds(2f);
            var velocity = rig.Get<Rigidbody>().velocity;
            Assert.Greater(velocity.x, 0.8f);
            Assert.Less(Mathf.Abs(velocity.z), 0.1f);
        }

        [UnityTest]
        public IEnumerator HeavyPassive_ReducesLateralDrift()
        {
            // La balsa mira a +Z y la corriente va a +X: toda la corriente es lateral.
            rig.Get<RaftFlowReceiver>().LateralFactor = 0.7f;
            yield return new WaitForSeconds(2f);
            float reduced = rig.Get<Rigidbody>().velocity.x;

            rig.Get<RaftFlowReceiver>().LateralFactor = 1f;
            rig.Get<Rigidbody>().velocity = Vector3.zero;
            yield return new WaitForSeconds(2f);
            float full = rig.Get<Rigidbody>().velocity.x;

            Assert.AreEqual(0.7f, reduced / full, 0.05f);
        }
    }
}
