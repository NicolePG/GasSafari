using System.Collections;
using GasSafari.Abilities;
using GasSafari.Characters;
using GasSafari.Level;
using GasSafari.Raft;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;

namespace GasSafari.Tests
{
    public class GloriaTests
    {
        private const float CURRENT = 1f;

        private RaftTestRig rig;
        private FlowSpline flow;
        private ScriptedCommandSource gloria;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var go = new GameObject("TestFlow");
            go.SetActive(false);
            go.AddComponent<SplineContainer>().Spline = new Spline(new[]
            {
                new BezierKnot(new float3(-50f, 0f, 0f)),
                new BezierKnot(new float3(50f, 0f, 0f))
            });
            flow = go.AddComponent<FlowSpline>();
            flow.Configure(10f, AnimationCurve.Constant(0f, 1f, CURRENT));
            go.SetActive(true);

            rig = new RaftTestRig(new Vector3(0f, 0.2f, 0f));
            rig.Get<RaftFlowReceiver>().SetFlow(flow);
            gloria = rig.AddAvatar(180f, "Gloria");
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void TearDown()
        {
            rig.Destroy();
            Object.Destroy(flow.gameObject);
        }

        [UnityTest]
        public IEnumerator HeavyPassive_ReducesLateralWhileOnBoard()
        {
            var receiver = rig.Get<RaftFlowReceiver>();
            Assert.AreEqual(rig.Balance.heavyLateralFactor, receiver.LateralFactor, 1e-5f);
            gloria.GetComponent<AvatarFall>().Fall(new Vector3(0f, 0f, -3f));
            yield return null;
            Assert.AreEqual(1f, receiver.LateralFactor, 1e-5f);
        }

        [UnityTest]
        public IEnumerator BellyFlop_LandsAfterJump_StabilizesAndPushesWithCurrent()
        {
            var flop = (BellyFlopAbility)gloria.GetComponent<AbilityRunner>().Ability;
            var body = rig.Get<Rigidbody>();
            var effects = rig.Get<RaftEffects>();
            body.angularVelocity = new Vector3(0f, 2f, 0f);

            var runner = gloria.GetComponent<AbilityRunner>();
            var states = gloria.GetComponent<AvatarStateMachine>();
            string log = "";
            runner.AbilityPressed += ok => log += $"pulsó(activó={ok}, estado={states.State}) ";
            gloria.Command.AbilityPresses++;
            yield return null;
            Assert.AreEqual(AvatarState.AbilityCast, states.State,
                $"{log}| cmd={gloria.GetComponent<PlayerAvatar>().Command.AbilityPresses} recarga lista={runner.Cooldown.IsReady}");
            float vxBefore = body.velocity.x;

            yield return new WaitForSeconds(flop.jumpTime + 0.05f);
            yield return new WaitForFixedUpdate();

            Assert.Less(Mathf.Abs(body.angularVelocity.y), 0.5f, "anula el giro");
            Assert.AreEqual(rig.Balance.waterAngularDrag * flop.angularDragMultiplier, body.angularDrag, 1e-4f);
            Assert.IsTrue(effects.IsWhirlpoolImmune);
            Assert.Greater(body.velocity.x - vxBefore, flop.currentImpulse / body.mass * 0.6f, "impulso con la corriente");
            Assert.AreEqual(AvatarState.Seated, gloria.GetComponent<AvatarStateMachine>().State);

            yield return new WaitForSeconds(flop.effectDuration);
            Assert.AreEqual(rig.Balance.waterAngularDrag, body.angularDrag, 1e-4f);
            Assert.IsFalse(effects.IsWhirlpoolImmune);
        }
    }
}
